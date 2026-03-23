using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.Rendering.HableCurve;

public class GameManager : MonoBehaviour
{
   // public static GameManager Instance {  get; private set; }

    // ==========================================

    [Header("Segments")]
    [SerializeField] private SegmentController _initialSegment;
    [SerializeField] private SegmentController[] _segmentPool;
    [SerializeField] private int _segmentNumbers;

    [Header("Movement Speeds")]
    [SerializeField] private float _speed = 0f;
    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _runSpeed = 10f;
    [SerializeField] private float _flyingSpeed = 15f;
    [SerializeField] private float _accelerationSpeed = 5f;
    private float _targetSpeed = 0f;

    [Header("Destory Segment")]
    [SerializeField] float _destroyPositionZ = -40f;

    [Header("Debug")]
    [SerializeField] private List<SegmentController> _instanciatedSegments = new List<SegmentController>();
    private SegmentController _toRemoveSegment;

    private bool _isJumping;
    private bool _isWalking;
    private bool _isFlying;
    private bool _isRunning;

    private bool _inGameState;
    private GameState _gameState;

    private void Awake()
    {
        /*if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }*/

        RunnerEventSystem.OnSpeedTargetChange += SetTargetSpeed;
        RunnerEventSystem.OnSpeedChange += SetSpeed;
        RunnerEventSystem.OnPlayerJump += HandlePlayerJump;
        RunnerEventSystem.OnFlyUp += StartFlying;
        RunnerEventSystem.OnFlyEnd += StopFlying;

        RunnerEventSystem.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
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

    private void Init()
    {
        GenerateBaseSegments();
        SetTargetSpeed(SpeedState.Run);
    }

    private void Update()
    {
        if (!_inGameState)
        {
            return;
        }

        SegmentController segment = null;

        if (_speed <= _targetSpeed) _speed += _accelerationSpeed * Time.deltaTime;
        if (_speed > _targetSpeed + 0.1) _speed -= _accelerationSpeed * Time.deltaTime;

        HandleMoveAnimation();
        HandleSpeedState();

        for (int i = 0; i < _instanciatedSegments.Count; i++)
        {
            segment = _instanciatedSegments[i];

            segment.transform.position += Vector3.back * Time.deltaTime * _speed;

            if (segment.transform.position.z < _destroyPositionZ)
            {
                _toRemoveSegment = segment;
                AddNewSegment();
            }
        }

        if (_toRemoveSegment != null)
        {
            _instanciatedSegments.Remove(_toRemoveSegment);
            Destroy(_toRemoveSegment.gameObject);

            _toRemoveSegment = null;
        }
    }

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

    private void HandleMoveAnimation()
    {
        if (_isJumping || _isFlying) return;

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

    private void GenerateBaseSegments()
    {
        SegmentController segment = Instantiate(_initialSegment, Vector3.zero, Quaternion.identity);
        _instanciatedSegments.Add(segment);

        for (int i = 1; i < _segmentNumbers; i++)
        {
            AddNewSegment();
        }
    }

    private void AddNewSegment()
    {
        int random = 0;

        random = Random.Range(0, _segmentPool.Length);

        SegmentController segment = Instantiate(_segmentPool[random], LastSegment().EndOfSegment.position, Quaternion.identity);
        _instanciatedSegments.Add(segment);
    }

    private SegmentController LastSegment()
    {
        return _instanciatedSegments[_instanciatedSegments.Count - 1];
    }

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

    private void HandleStateChanged(State newState)
    {
        if (newState is not GameState gameState)
        {
            _inGameState = false;
            return;
        }

        _inGameState = true;
    }

}
