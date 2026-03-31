using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementController : MonoBehaviour
{
    [Header("Controls")]
    // Input action references bound via the Unity Input System (assigned in Inspector)
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _jumpAction;

    [Header("Movement")]
    // World-space transforms defining the horizontal lane positions (e.g. left, center, right)
    [SerializeField] private Transform[] _lanePositions;
    // World-space transforms defining the vertical flying positions (ground level, flying level, etc.)
    [SerializeField] private Transform[] _flyingPositions;
    // How fast the player slides between lanes or flying heights
    [SerializeField] private float _strafeSpeed = 10f;
    // Duration of the "falling" animation played when the player runs out of fuel mid-flight
    [SerializeField] private float _flyingOutOfFuelAnimationDuration;

    //===============================

    // The world position the player is currently interpolating towards
    private Vector3 _targetPosition;
    // True while the player is sliding between lanes or flying heights
    private bool _isMoving = false;
    // True while the jump coroutine is running
    private bool _isJumping = false;
    // True while the player is in the air (flying mode active)
    private bool _isFlying = false;

    // When true, all input and movement is suspended (e.g. during menus or cutscenes)
    private bool _locked = false;

    // Index into _lanePositions for the player's current horizontal lane (default: center)
    private int _currentLaneIndex = 1;
    // Index into _flyingPositions for the player's current vertical level (0 = ground)
    private int _currentFlyingLaneIndex = 0;

    //===============================

    [Header("Jump parameters")]
    // Total time the jump arc takes from takeoff to landing
    [SerializeField] private float _jumpDuration = 1f;
    // Maximum height reached at the apex of the jump
    [SerializeField] private float _jumpHeight = 2f;
    // Animation curve that shapes the jump arc (evaluated 0→1 over _jumpDuration)
    [SerializeField] private AnimationCurve _jumpCurve;

    // Stores the last player command received so it can be processed on the next valid frame
    private PlayerCommand _inputBuffer = PlayerCommand.Idle;

    // Reference to the inventory controller used to check/consume fuel during flight
    private PlayerInventoryController _inventoryController;

    // Reference to the active flying coroutine, kept so it can be stopped early (e.g. on FlyDown)
    private Coroutine _flyingCoroutine = null;

    private void Awake()
    {
        // Subscribe to game events; FlyDown is called when the player takes damage while flying
        RunnerEventSystem.OnFlyingDamage += FlyDown;
        // HandleStateChanged is called whenever the game transitions between states (menu, gameplay, etc.)
        RunnerEventSystem.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        // Always unsubscribe from events on destroy to avoid memory leaks and ghost callbacks
        RunnerEventSystem.OnFlyingDamage -= FlyDown;
        RunnerEventSystem.OnStateChanged -= HandleStateChanged;
    }

    void Start()
    {
        // Register input callbacks for move and jump actions
        if (_moveAction != null)
        {
            _moveAction.action.performed += HandleMoveCommand;
        }

        if (_jumpAction != null)
        {
            _jumpAction.action.performed += HandleJumpCommand;
        }

        // Set the initial target position: center lane, ground-level flying index
        _targetPosition = new Vector3(
            _lanePositions[_currentLaneIndex].position.x,
            _flyingPositions[_currentFlyingLaneIndex].position.y,
            transform.position.z
        );

        _inventoryController = GetComponent<PlayerInventoryController>();
    }

    private void Update()
    {
        // Skip all movement logic while the controller is locked (non-gameplay state)
        if (_locked)
        {
            return;
        }

        if (_isMoving)
        {
            // Smoothly move towards the target position at the configured strafe speed
            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _strafeSpeed * Time.deltaTime);

            // Once the player reaches the target, clear the moving flag
            if (transform.position == _targetPosition) _isMoving = false;

            // If we just finished a lateral move while flying and have reached the ground height,
            // trigger the landing sequence
            if (!_isMoving && transform.position.y == _flyingPositions[0].position.y && _isFlying) HandleDescent();
        }
        else if (!_isJumping)
        {
            // Process any buffered input command when the player is free (not moving or jumping)
            HandleInputBufferCommands();
        }
    }

    // Called when the player lands after flying (either manually or after running out of fuel)
    private void HandleDescent()
    {
        _isFlying = false;
        // Restore running speed and notify the rest of the game that flight has ended
        RunnerEventSystem.OnSpeedTargetChange?.Invoke(SpeedState.Run);
        RunnerEventSystem.OnStartRunning?.Invoke();
        RunnerEventSystem.OnFlyEnd?.Invoke();
    }

    // Reads the input buffer and dispatches the appropriate action, then clears the buffer
    private void HandleInputBufferCommands()
    {
        switch (_inputBuffer)
        {
            case PlayerCommand.Idle: break;                            // Nothing to process
            case PlayerCommand.Jump: HandleJump(); break;              // Initiate a jump
            default: HandleMove(_inputBuffer); break;  // Initiate a lane/fly move
        }

        _inputBuffer = PlayerCommand.Idle;
    }

    // Called by the Input System when the jump action is performed; buffers the command
    private void HandleJumpCommand(InputAction.CallbackContext obj)
    {
        _inputBuffer = PlayerCommand.Jump;
    }

    // Starts the jump coroutine if the player is in a valid state to jump
    private void HandleJump()
    {
        // Cannot jump while already jumping, mid-strafe, or airborne in flying mode
        if (!_isJumping && !_isMoving && !_isFlying)
        {
            StartCoroutine(JumpCoroutine());
        }
    }

    // Animates the jump arc using the configured curve and height over _jumpDuration seconds
    private IEnumerator JumpCoroutine()
    {
        // Notify other systems (e.g. animator, obstacle spawner) that a jump has started
        RunnerEventSystem.OnPlayerJump?.Invoke(_jumpDuration);

        _isJumping = true;

        float jumpTimer = 0f;

        while (jumpTimer < _jumpDuration)
        {
            if (!_locked)
            {
                jumpTimer += Time.deltaTime;
                float normalizedTime = jumpTimer / _jumpDuration; // 0 at start, 1 at end

                // Sample the curve to get the current height offset, scaled by _jumpHeight
                float targetHeight = (_jumpCurve.Evaluate(normalizedTime) * _jumpHeight);
                Vector3 targetPosition = new Vector3(transform.position.x, targetHeight, transform.position.z);

                transform.position = targetPosition;
            }
            
            yield return new WaitForEndOfFrame();
        }

        _isJumping = false;
    }

    // Called by the Input System when the move action is performed; buffers the direction as a command
    private void HandleMoveCommand(InputAction.CallbackContext obj)
    {
        Vector2 moveDirection = obj.ReadValue<Vector2>();

        // Horizontal input maps to left/right lane changes
        if (moveDirection.x != 0)
        {
            if (moveDirection.x > 0) _inputBuffer = PlayerCommand.MoveRight;
            else _inputBuffer = PlayerCommand.MoveLeft;
        }

        // Vertical input maps to flying up/down (only relevant in flying mode)
        if (moveDirection.y != 0)
        {
            if (moveDirection.y > 0) _inputBuffer = PlayerCommand.MoveUp;
            else _inputBuffer = PlayerCommand.MoveDown;
        }
    }

    // Dispatches a move command to either lateral or vertical (fly) movement handlers
    private void HandleMove(PlayerCommand command)
    {
        // Ignore move commands while already transitioning or mid-jump
        if (!_isMoving && !_isJumping)
        {
            switch (command)
            {
                case PlayerCommand.MoveRight: HandleLateralMovement(Vector2.right); break;
                case PlayerCommand.MoveLeft: HandleLateralMovement(Vector2.left); break;
                case PlayerCommand.MoveUp: HandleFlyMovement(Vector2.up); break;
                case PlayerCommand.MoveDown: HandleFlyMovement(Vector2.down); break;
                default: break;
            }
        }
    }

    // Moves the player one lane to the left or right, clamped to the available lanes
    private void HandleLateralMovement(Vector2 moveDirection)
    {
        if (moveDirection.x > 0)
        {
            // Move right: only if not already on the rightmost lane
            if (_currentLaneIndex < _lanePositions.Length - 1)
            {
                _isMoving = true;
                _currentLaneIndex++;
                _targetPosition = new Vector3(_lanePositions[_currentLaneIndex].position.x, transform.position.y, transform.position.z);
            }
        }
        else
        {
            // Move left: only if not already on the leftmost lane
            if (_currentLaneIndex > 0)
            {
                _isMoving = true;
                _currentLaneIndex--;
                _targetPosition = new Vector3(_lanePositions[_currentLaneIndex].position.x, transform.position.y, transform.position.z);
            }
        }
    }

    // Handles vertical movement: initiates FlyUp when pressing up from ground, FlyDown when pressing down in the air
    private void HandleFlyMovement(Vector2 moveDirection)
    {
        if (moveDirection.y > 0)
        {
            // Can only fly up from the ground level (index 0)
            if (_currentFlyingLaneIndex == 0)
            {
                FlyUp();
            }
        }
        else if (_currentFlyingLaneIndex > 0)
        {
            // Can only fly down if currently airborne
            FlyDown();
        }
    }

    // Launches the player into the air if they have fuel; starts the fuel-drain coroutine
    private void FlyUp()
    {
        if (_inventoryController.Fuel > 0)
        {
            _isMoving = true;
            _isFlying = true;

            // Switch to flying speed
            RunnerEventSystem.OnSpeedTargetChange?.Invoke(SpeedState.Fly);

            _currentFlyingLaneIndex++;
            _targetPosition.y = _flyingPositions[_currentFlyingLaneIndex].position.y;

            // Start draining fuel over time; store the reference so it can be cancelled
            _flyingCoroutine = StartCoroutine(FlyingCoroutine());
        }
    }

    // Drains fuel every second while the player is flying; triggers landing sequence when empty
    private IEnumerator FlyingCoroutine()
    {
        RunnerEventSystem.OnFlyUp?.Invoke();

        while (_inventoryController.Fuel > 0)
        {
             if (!_locked) _inventoryController.Fuel -= 1;
            yield return new WaitForSeconds(1);
        }

        // Fuel depleted: notify the game and pause the runner before the fall animation plays
        RunnerEventSystem.OnPlayerOutOfFuel?.Invoke();
        RunnerEventSystem.OnSpeedChange?.Invoke(SpeedState.Stop);
        RunnerEventSystem.OnSpeedTargetChange?.Invoke(SpeedState.Stop);

        // Wait for the out-of-fuel animation before actually descending
        yield return new WaitForSeconds(_flyingOutOfFuelAnimationDuration);

        FlyDown();
    }

    // Brings the player back to the ground level and stops the fuel-drain coroutine
    private void FlyDown()
    {
        RunnerEventSystem.OnFlyDown?.Invoke();

        _isMoving = true;
        _currentFlyingLaneIndex--;
        _targetPosition.y = _flyingPositions[_currentFlyingLaneIndex].position.y;

        // Cancel the flying coroutine (prevents double FlyDown calls or continued fuel drain)
        StopCoroutine(_flyingCoroutine);
    }

    // Responds to game state changes: locks movement during non-gameplay states (menus, death, etc.)
    private void HandleStateChanged(State newState)
    {       
        if (newState is not GameState)
        {
            _locked = true;
            return;
        }

        _locked = false;
    }
}