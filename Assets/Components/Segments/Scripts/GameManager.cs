using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Central manager for the gameplay scene. Responsible for:
// - Spawning and recycling level segments to create an endless runner
// - Smoothly accelerating/decelerating world scroll speed towards a target speed
// - Mapping the current speed to a SpeedState and broadcasting it each frame
// - Gating all of the above behind the active game state
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Segments")]
    // The fixed first segment, always spawned at the origin to open the level
    [SerializeField] private SegmentController _initialSegment;
    // Pool of segment prefabs to randomly draw from when extending the level
    [SerializeField] private SegmentController[] _segmentPool;
    // Total number of segments kept alive at any one time
    [SerializeField] private int _segmentNumbers;

    [Header("Movement Speeds")]
    // Current world scroll speed, interpolated towards _targetSpeed each frame
    [SerializeField] private float _speed = 0f;
    // Discrete speed values for each movement state, configured in the Inspector
    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _runSpeed = 10f;
    [SerializeField] private float _flyingSpeed = 15f;
    // Rate (units/second) at which _speed approaches _targetSpeed
    [SerializeField] private float _accelerationSpeed = 5f;
    // The speed value currently being interpolated towards; set via SetTargetSpeed
    private float _targetSpeed = 0f;

    [Header("Destory Segment")]
    // Z position threshold below which a segment is considered off-screen and recycled
    [SerializeField] float _destroyPositionZ = -40f;

    [Header("Debug")]
    // Live list of all currently active segments; visible in the Inspector for debugging
    [SerializeField] private List<SegmentController> _instanciatedSegments = new List<SegmentController>();
    // Holds the segment flagged for removal this frame; processed after the scroll loop
    private SegmentController _toRemoveSegment;

    [Header("Score")]
    [SerializeField] private TMP_Text _scoreText;
    private float _score = 0f;
    public int Score => (int)_score;

    // Animation state flags used to avoid redundant event invocations in HandleMoveAnimation
    private bool _isJumping;
    private bool _isFlying;
    private bool _isWalking;
    private bool _isRunning;

    // Gates Update logic — only runs when the game is in an active GameState
    private bool _inGameState;
    private GameState _gameState;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        RunnerEventSystem.OnSpeedTargetChange += SetTargetSpeed;  // Set the speed to ease towards
        RunnerEventSystem.OnSpeedChange += SetSpeed;         // Snap speed immediately (no easing)
        RunnerEventSystem.OnPlayerJump += HandlePlayerJump; // Track jump state for animation gating
        RunnerEventSystem.OnFlyUp += StartFlying;      // Track fly state for animation gating
        RunnerEventSystem.OnFlyEnd += StopFlying;       // Clear fly state on landing

        RunnerEventSystem.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent ghost callbacks after the object is destroyed
        RunnerEventSystem.OnSpeedTargetChange -= SetTargetSpeed;
        RunnerEventSystem.OnSpeedChange -= SetSpeed;
        RunnerEventSystem.OnPlayerJump -= HandlePlayerJump;
        RunnerEventSystem.OnFlyUp -= StartFlying;
        RunnerEventSystem.OnFlyEnd -= StopFlying;

        RunnerEventSystem.OnStateChanged -= HandleStateChanged;
    }

    private void StartFlying() => _isFlying = true;
    private void StopFlying() => _isFlying = false;

    // ==========================================

    private void Start()
    {
        Init();
    }

    // Sets up the level and kicks off at run speed
    private void Init()
    {
        GenerateBaseSegments();
        SetTargetSpeed(SpeedState.Run);
    }

    private void Update()
    {
        // All gameplay logic is gated: skip entirely outside of a GameState
        if (!_inGameState)
        {
            return;
        }

        SegmentController segment = null;

        // Ease _speed towards _targetSpeed using a simple proportional approach:
        // accelerate when below target, decelerate when above (with a small tolerance to avoid jitter)
        if (_speed <= _targetSpeed) _speed += _accelerationSpeed * Time.deltaTime;
        if (_speed > _targetSpeed + 0.1) _speed -= _accelerationSpeed * Time.deltaTime;

        // Broadcast the appropriate locomotion animation event based on current speed
        HandleMoveAnimation();
        // Broadcast the appropriate SpeedState based on current speed thresholds
        HandleSpeedState();

        // Scroll all active segments towards the camera and recycle any that pass the threshold
        for (int i = 0; i < _instanciatedSegments.Count; i++)
        {
            segment = _instanciatedSegments[i];

            segment.transform.position += Vector3.back * Time.deltaTime * _speed;

            // Flag the segment for removal and immediately spawn a replacement at the chain's end
            if (segment.transform.position.z < _destroyPositionZ)
            {
                _toRemoveSegment = segment;
                AddNewSegment();
            }
        }

        _score += Time.deltaTime * _speed;
        UpdateScore();

        // Remove and destroy the flagged segment outside the loop to avoid modifying
        // the list while it is being iterated
        if (_toRemoveSegment != null)
        {
            _instanciatedSegments.Remove(_toRemoveSegment);
            Destroy(_toRemoveSegment.gameObject);

            _toRemoveSegment = null;
        }
    }

    // Compares the current speed against threshold bands and broadcasts the matching SpeedState.
    // Small tolerances (e.g. - 0.5) prevent flickering at the boundary between states.
    // Mainly used by the Horde Controller to update the horde meter
    private void HandleSpeedState()
    {
        if (_speed >= _flyingSpeed - 2.5)
        {
            RunnerEventSystem.OnSpeedStateChange?.Invoke(SpeedState.Fly);
            return;
        }

        if (_speed >= _runSpeed - 0.5)
        {
            RunnerEventSystem.OnSpeedStateChange?.Invoke(SpeedState.Run);
            return;
        }

        if (_speed >= _walkSpeed - 0.5)
        {
            RunnerEventSystem.OnSpeedStateChange?.Invoke(SpeedState.Walk);
            return;
        }

        RunnerEventSystem.OnSpeedStateChange?.Invoke(SpeedState.Stop);
    }

    // Fires walk/run animation events when the speed crosses the relevant threshold.
    // Skipped entirely during jumps and flight since those have their own animation states.
    // State flags (_isWalking, _isRunning) prevent the same event from firing every frame.
    private void HandleMoveAnimation()
    {
        if (_isJumping || _isFlying)
        {
            return;
        }

        if (_speed < _runSpeed - 0.1f && !_isWalking)
        {
            RunnerEventSystem.OnStartWalking?.Invoke();

            _isWalking = true;
            _isRunning = false;
        }
        else if (_speed >= _runSpeed && !_isRunning)
        {
            RunnerEventSystem.OnStartRunning?.Invoke();

            _isWalking = false;
            _isRunning = true;
        }
    }

    // Kicks off a coroutine that keeps _isJumping true for the full jump duration,
    // preventing walk/run animation events from firing during the arc
    private void HandlePlayerJump(float duration)
    {
        StartCoroutine(HandlePlayerJumpCoroutine(duration));
    }

    private IEnumerator HandlePlayerJumpCoroutine(float duration)
    {
        _isJumping = true;
        yield return new WaitForSeconds(duration);
        _isJumping = false;
    }

    // Spawns the initial segment at the origin then fills the rest of the chain
    // with randomly selected segments placed end-to-start
    private void GenerateBaseSegments()
    {
        // Always start with the designated initial segment at world origin
        SegmentController segment = Instantiate(_initialSegment, Vector3.zero, Quaternion.identity);
        _instanciatedSegments.Add(segment);

        // Fill the remaining slots starting at index 1 (the initial segment occupies index 0)
        for (int i = 1; i < _segmentNumbers; i++)
        {
            AddNewSegment();
        }
    }

    // Picks a random segment from the pool and places it flush against the end of the last segment
    private void AddNewSegment()
    {
        int random = Random.Range(0, _segmentPool.Length);

        // Spawn at the EndOfSegment anchor of the current last segment to keep the chain seamless
        SegmentController segment = Instantiate(_segmentPool[random], LastSegment().EndOfSegment.position, Quaternion.identity);
        _instanciatedSegments.Add(segment);
    }

    // Returns the most recently added segment, used as the attachment point for the next one
    private SegmentController LastSegment()
    {
        return _instanciatedSegments[_instanciatedSegments.Count - 1];
    }

    // Maps a SpeedState to a target speed value; _speed will ease towards this over time
    private void SetTargetSpeed(SpeedState speed)
    {
        switch (speed)
        {
            case SpeedState.Stop: _targetSpeed = 0f; break;
            case SpeedState.Walk: _targetSpeed = _walkSpeed; break;
            case SpeedState.Run: _targetSpeed = _runSpeed; break;
            case SpeedState.Fly: _targetSpeed = _flyingSpeed; break;
            default: goto case SpeedState.Stop;
        }
    }

    // Maps a SpeedState to an immediate speed value, bypassing the acceleration easing.
    // Used for hard stops (e.g. out-of-fuel landing) where snapping is preferable to easing.
    private void SetSpeed(SpeedState speed)
    {
        switch (speed)
        {
            case SpeedState.Stop: _speed = 0f; break;
            case SpeedState.Walk: _speed = _walkSpeed; break;
            case SpeedState.Run: _speed = _runSpeed; break;
            case SpeedState.Fly: _speed = _flyingSpeed; break;
            default: goto case SpeedState.Stop;
        }
    }

    // Locks the manager out of Update when the game leaves a GameState (menu, game over, etc.)
    private void HandleStateChanged(State newState)
    {
        if (newState is not GameState gameState)
        {
            _inGameState = false;
            return;
        }

        _inGameState = true;
    }

    private void UpdateScore()
    {
        _scoreText.text = $"{(int)_score}m";
    }
}