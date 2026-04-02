using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls all player movement in the runner:
/// lateral lane-switching, jumping, and vertical flying.
///
/// Movement is driven by Unity's Input System through buffered commands,
/// meaning input is stored each frame and consumed only when the player
/// is in a valid state to act on it (not mid-strafe, not mid-jump).
///
/// Flying drains fuel over time via <see cref="FlyingCoroutine"/>; the player
/// is forced back to the ground when fuel runs out.
/// </summary>
public class PlayerMovementController : MonoBehaviour
{
    // ── Input ────────────────────────────────────────────────────────────────

    [Header("Controls")]
    /// <summary>Input action that drives horizontal/vertical movement (assigned in Inspector).</summary>
    [SerializeField] private InputActionReference _moveAction;

    /// <summary>Input action that triggers a jump (assigned in Inspector).</summary>
    [SerializeField] private InputActionReference _jumpAction;

    // ── Movement Config ──────────────────────────────────────────────────────

    [Header("Movement")]
    /// <summary>
    /// World-space transforms marking each horizontal lane position
    /// (e.g. index 0 = left, 1 = center, 2 = right).
    /// </summary>
    [SerializeField] private Transform[] _lanePositions;

    /// <summary>
    /// World-space transforms marking each vertical flying level
    /// (index 0 = ground, higher indices = airborne heights).
    /// </summary>
    [SerializeField] private Transform[] _flyingPositions;

    /// <summary>Units per second used by MoveTowards when strafing between lanes or flying heights.</summary>
    [SerializeField] private float _strafeSpeed = 10f;

    /// <summary>
    /// How long the out-of-fuel falling animation plays before <see cref="FlyDown"/> is called.
    /// Gives animators time to show the drop before snapping the player to the ground.
    /// </summary>
    [SerializeField] private float _flyingOutOfFuelAnimationDuration;

    /// <summary>
    /// Multiplier applied to <see cref="_strafeSpeed"/> when the red/green speed potion is active.
    /// Loaded from save data in <see cref="HandleSaveLoaded"/>.
    /// </summary>
    [SerializeField] private float _speedPotionMultiplier = 2.0f;

    // ── Runtime State ────────────────────────────────────────────────────────

    /// <summary>The world position the player is currently interpolating towards via MoveTowards.</summary>
    private Vector3 _targetPosition;

    /// <summary>True while the player is sliding between lanes or flying heights.</summary>
    private bool _isMoving = false;

    /// <summary>True while <see cref="JumpCoroutine"/> is running.</summary>
    private bool _isJumping = false;

    /// <summary>True while the player is airborne in flying mode (not a jump — fuel-based flight).</summary>
    private bool _isFlying = false;

    /// <summary>
    /// When true, all input processing and movement is suspended.
    /// Set by <see cref="HandleStateChanged"/> whenever the game leaves a <see cref="GameState"/>
    /// (e.g. entering a menu, death screen, or cutscene).
    /// </summary>
    private bool _locked = false;

    /// <summary>
    /// Index into <see cref="_lanePositions"/> for the player's current horizontal lane.
    /// Defaults to 1 (center lane).
    /// </summary>
    private int _currentLaneIndex = 1;

    /// <summary>
    /// Index into <see cref="_flyingPositions"/> for the player's current vertical level.
    /// 0 means the player is on the ground; any higher value means airborne.
    /// </summary>
    private int _currentFlyingLaneIndex = 0;

    // ── Jump Config ──────────────────────────────────────────────────────────

    [Header("Jump parameters")]
    /// <summary>Total time in seconds from jump takeoff to landing.</summary>
    [SerializeField] private float _jumpDuration = 1f;

    /// <summary>Maximum height (world units) reached at the apex of the jump arc.</summary>
    [SerializeField] private float _jumpHeight = 2f;

    /// <summary>
    /// Animation curve shaping the jump arc, evaluated from 0 to 1 over <see cref="_jumpDuration"/>.
    /// A value of 1 on the curve equals <see cref="_jumpHeight"/> world units above the ground.
    /// </summary>
    [SerializeField] private AnimationCurve _jumpCurve;

    // ── Misc References ──────────────────────────────────────────────────────

    /// <summary>
    /// Stores the most recent unprocessed player command so it can be
    /// consumed on the next frame when the player is in a valid state.
    /// Prevents input from being silently dropped during a strafe or jump.
    /// </summary>
    private PlayerCommand _inputBuffer = PlayerCommand.Idle;

    /// <summary>
    /// Cached reference to the sibling inventory controller, used to
    /// read and consume fuel during flight.
    /// </summary>
    private PlayerInventoryController _inventoryController;

    /// <summary>
    /// Handle to the active <see cref="FlyingCoroutine"/> so it can be
    /// stopped early (e.g. when <see cref="FlyDown"/> is called manually
    /// or via damage before fuel runs out).
    /// </summary>
    private Coroutine _flyingCoroutine = null;

    // ── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        // FlyDown is called when the player takes damage while airborne
        RunnerEventSystem.OnFlyingDamage += FlyDown;
        // HandleStateChanged locks/unlocks movement on game state transitions
        RunnerEventSystem.OnStateChanged += HandleStateChanged;
        // HandleSaveLoaded restores movement modifiers from a loaded save
        RunnerEventSystem.OnSaveLoaded += HandleSaveLoaded;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent ghost callbacks if this object is destroyed
        // (e.g. between scene loads)
        RunnerEventSystem.OnFlyingDamage -= FlyDown;
        RunnerEventSystem.OnStateChanged -= HandleStateChanged;
        RunnerEventSystem.OnSaveLoaded -= HandleSaveLoaded;
    }

    void Start()
    {
        // Register input callbacks; null-checks guard against missing Inspector assignments
        if (_moveAction != null)
            _moveAction.action.performed += HandleMoveCommand;

        if (_jumpAction != null)
            _jumpAction.action.performed += HandleJumpCommand;

        // Snap to the center lane at ground level as the default starting position
        _targetPosition = new Vector3(
            _lanePositions[_currentLaneIndex].position.x,
            _flyingPositions[_currentFlyingLaneIndex].position.y,
            transform.position.z
        );

        _inventoryController = GetComponent<PlayerInventoryController>();
    }

    private void Update()
    {
        // All movement and input processing is suppressed outside gameplay states
        if (_locked) return;

        if (_isMoving)
        {
            // Interpolate toward the target position at a fixed units-per-second rate
            transform.position = Vector3.MoveTowards(
                transform.position, _targetPosition, _strafeSpeed * Time.deltaTime
            );

            // Clear the moving flag once the player has fully arrived at the target
            if (transform.position == _targetPosition)
                _isMoving = false;

            // If we just finished moving while flying AND we've reached ground height,
            // the player has descended — trigger the landing sequence
            if (!_isMoving && transform.position.y == _flyingPositions[0].position.y && _isFlying)
                HandleDescent();
        }
        else if (!_isJumping)
        {
            // The player is stationary and not jumping: safe to consume buffered input
            HandleInputBufferCommands();
        }
    }

    // ── State Handlers ───────────────────────────────────────────────────────

    /// <summary>
    /// Called once the player has fully descended to ground level after flying.
    /// Resets flight state and restores the normal run speed.
    /// </summary>
    private void HandleDescent()
    {
        _isFlying = false;
        RunnerEventSystem.OnSpeedTargetChange?.Invoke(SpeedState.Run);
        RunnerEventSystem.OnStartRunning?.Invoke();
        RunnerEventSystem.OnFlyEnd?.Invoke();
    }

    /// <summary>
    /// Reads and dispatches the buffered input command, then clears the buffer.
    /// Only called when the player is neither moving nor jumping.
    /// </summary>
    private void HandleInputBufferCommands()
    {
        switch (_inputBuffer)
        {
            case PlayerCommand.Idle: break;                  // Nothing queued
            case PlayerCommand.Jump: HandleJump(); break;    // Attempt a jump
            default: HandleMove(_inputBuffer); break;         // Attempt a lane or fly move
        }

        // Always clear the buffer after processing so stale commands don't repeat
        _inputBuffer = PlayerCommand.Idle;
    }

    // ── Input Callbacks ──────────────────────────────────────────────────────

    /// <summary>
    /// Registered with the jump <see cref="InputActionReference"/>.
    /// Buffers a jump command for processing on the next eligible Update frame.
    /// </summary>
    private void HandleJumpCommand(InputAction.CallbackContext obj)
    {
        _inputBuffer = PlayerCommand.Jump;
    }

    /// <summary>
    /// Registered with the move <see cref="InputActionReference"/>.
    /// Maps the raw 2D input vector to a directional <see cref="PlayerCommand"/>
    /// and stores it in the buffer.
    /// Horizontal axis → lane change; vertical axis → fly up/down.
    /// </summary>
    private void HandleMoveCommand(InputAction.CallbackContext obj)
    {
        Vector2 moveDirection = obj.ReadValue<Vector2>();

        if (moveDirection.x != 0)
        {
            _inputBuffer = moveDirection.x > 0
                ? PlayerCommand.MoveRight
                : PlayerCommand.MoveLeft;
        }

        // Vertical input overrides horizontal if both are non-zero
        // (the last assignment wins, so vertical takes priority)
        if (moveDirection.y != 0)
        {
            _inputBuffer = moveDirection.y > 0
                ? PlayerCommand.MoveUp
                : PlayerCommand.MoveDown;
        }
    }

    // ── Movement Dispatch ────────────────────────────────────────────────────

    /// <summary>
    /// Initiates a jump if the player is on the ground and not already jumping or strafing.
    /// </summary>
    private void HandleJump()
    {
        // Jumping while mid-strafe or airborne is intentionally blocked
        if (!_isJumping && !_isMoving && !_isFlying)
            StartCoroutine(JumpCoroutine());
    }

    /// <summary>
    /// Routes a directional command to the appropriate lateral or vertical handler.
    /// Ignored if the player is already transitioning.
    /// </summary>
    private void HandleMove(PlayerCommand command)
    {
        if (_isMoving || _isJumping) return;

        switch (command)
        {
            case PlayerCommand.MoveRight: HandleLateralMovement(Vector2.right); break;
            case PlayerCommand.MoveLeft: HandleLateralMovement(Vector2.left); break;
            case PlayerCommand.MoveUp: HandleFlyMovement(Vector2.up); break;
            case PlayerCommand.MoveDown: HandleFlyMovement(Vector2.down); break;
            default: break;
        }
    }

    // ── Movement Handlers ────────────────────────────────────────────────────

    /// <summary>
    /// Moves the player one lane left or right, clamped to the available lane array.
    /// Sets <see cref="_isMoving"/> and updates <see cref="_targetPosition"/> for
    /// <see cref="Update"/> to interpolate toward.
    /// </summary>
    private void HandleLateralMovement(Vector2 moveDirection)
    {
        if (moveDirection.x > 0)
        {
            // Move right — guard against stepping past the last lane
            if (_currentLaneIndex < _lanePositions.Length - 1)
            {
                _isMoving = true;
                _currentLaneIndex++;
                _targetPosition = new Vector3(
                    _lanePositions[_currentLaneIndex].position.x,
                    transform.position.y,
                    transform.position.z
                );
            }
        }
        else
        {
            // Move left — guard against stepping before the first lane
            if (_currentLaneIndex > 0)
            {
                _isMoving = true;
                _currentLaneIndex--;
                _targetPosition = new Vector3(
                    _lanePositions[_currentLaneIndex].position.x,
                    transform.position.y,
                    transform.position.z
                );
            }
        }
    }

    /// <summary>
    /// Handles vertical input: initiates <see cref="FlyUp"/> when pressing up from the
    /// ground, or <see cref="FlyDown"/> when pressing down while airborne.
    /// </summary>
    private void HandleFlyMovement(Vector2 moveDirection)
    {
        if (moveDirection.y > 0)
        {
            // FlyUp is only available from ground level (index 0)
            if (_currentFlyingLaneIndex == 0)
                FlyUp();
        }
        else if (_currentFlyingLaneIndex > 0)
        {
            // FlyDown is only valid while already airborne
            FlyDown();
        }
    }

    // ── Flight ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Launches the player into the air if they have fuel remaining.
    /// Switches to flying speed and starts the fuel-drain coroutine.
    /// </summary>
    private void FlyUp()
    {
        if (_inventoryController.Fuel <= 0) return;

        _isMoving = true;
        _isFlying = true;

        RunnerEventSystem.OnSpeedTargetChange?.Invoke(SpeedState.Fly);

        _currentFlyingLaneIndex++;
        _targetPosition.y = _flyingPositions[_currentFlyingLaneIndex].position.y;

        // Store the coroutine reference so it can be cancelled early by FlyDown
        _flyingCoroutine = StartCoroutine(FlyingCoroutine());
    }

    /// <summary>
    /// Drains one unit of fuel per second while the player is airborne.
    /// When fuel reaches zero, stops the runner and waits for the out-of-fuel
    /// animation before calling <see cref="FlyDown"/>.
    /// </summary>
    private IEnumerator FlyingCoroutine()
    {
        RunnerEventSystem.OnFlyUp?.Invoke();

        while (_inventoryController.Fuel > 0)
        {
            // Only drain fuel when the game is unpaused/unlocked
            if (!_locked) _inventoryController.Fuel -= 1;
            yield return new WaitForSeconds(1);
        }

        // Fuel exhausted: halt the runner and broadcast the event
        // so other systems (UI, audio) can react before the fall plays
        RunnerEventSystem.OnPlayerOutOfFuel?.Invoke();
        RunnerEventSystem.OnSpeedChange?.Invoke(SpeedState.Stop);
        RunnerEventSystem.OnSpeedTargetChange?.Invoke(SpeedState.Stop);

        // Give the out-of-fuel animation time to finish before descending
        yield return new WaitForSeconds(_flyingOutOfFuelAnimationDuration);

        FlyDown();
    }

    /// <summary>
    /// Returns the player to the ground level and cancels the fuel-drain coroutine.
    /// Can be triggered manually (player input), by damage (<see cref="RunnerEventSystem.OnFlyingDamage"/>),
    /// or automatically when fuel runs out.
    /// </summary>
    private void FlyDown()
    {
        RunnerEventSystem.OnFlyDown?.Invoke();

        _isMoving = true;
        _currentFlyingLaneIndex--;
        _targetPosition.y = _flyingPositions[_currentFlyingLaneIndex].position.y;

        // Stop the coroutine to prevent a second FlyDown call or continued fuel drain
        // after the player has already started descending
        if (_flyingCoroutine != null)
            StopCoroutine(_flyingCoroutine);
    }

    // ── Jump ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Animates a parabolic jump using the configured <see cref="_jumpCurve"/>,
    /// <see cref="_jumpHeight"/>, and <see cref="_jumpDuration"/>.
    /// The jump is paused when the controller is locked (e.g. game paused)
    /// and resumes automatically when unlocked.
    /// </summary>
    private IEnumerator JumpCoroutine()
    {
        RunnerEventSystem.OnPlayerJump?.Invoke(_jumpDuration);

        _isJumping = true;
        float jumpTimer = 0f;

        while (jumpTimer < _jumpDuration)
        {
            if (!_locked)
            {
                jumpTimer += Time.deltaTime;

                // Normalise 0→1 so the curve can be authored independently of duration
                float normalizedTime = jumpTimer / _jumpDuration;

                // Curve output (0–1) scaled by _jumpHeight gives the current Y offset
                float targetHeight = _jumpCurve.Evaluate(normalizedTime) * _jumpHeight;

                transform.position = new Vector3(
                    transform.position.x,
                    targetHeight,
                    transform.position.z
                );
            }

            yield return new WaitForEndOfFrame();
        }

        _isJumping = false;
    }

    // ── Event Handlers ───────────────────────────────────────────────────────

    /// <summary>
    /// Locks movement whenever the game leaves a <see cref="GameState"/>
    /// (e.g. transitions to a menu, pause, or death screen),
    /// and unlocks it when a <see cref="GameState"/> is entered.
    /// </summary>
    private void HandleStateChanged(State newState)
    {
        _locked = newState is not GameState;
    }

    /// <summary>
    /// Called once when a save file is loaded.
    /// If the red/green speed potion was active at save time, applies the
    /// speed multiplier to <see cref="_strafeSpeed"/>.
    /// Unsubscribes immediately after — this only needs to run once per load.
    /// </summary>
    private void HandleSaveLoaded(SaveData save)
    {
        if (save.RedGreenPotionActive)
            _strafeSpeed *= _speedPotionMultiplier;

        // One-shot: unsubscribe so subsequent save-loads don't stack the multiplier
        RunnerEventSystem.OnSaveLoaded -= HandleSaveLoaded;
    }
}