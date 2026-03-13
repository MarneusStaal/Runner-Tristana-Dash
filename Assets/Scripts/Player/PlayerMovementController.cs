using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerCommand
{
    Idle,
    MoveLeft,
    MoveRight,
    MoveUp,
    MoveDown,
    Jump
}

public class PlayerMovementController : MonoBehaviour
{
    [Header("Controls")]
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _jumpAction;

    [Header("Movement")]
    [SerializeField] private Transform[] _lanePositions;
    [SerializeField] private Transform[] _flyingPositions;
    [SerializeField] private float _strafeSpeed = 10f;

    //===============================
    private Vector3 _targetPosition;
    private bool _isMoving = false;
    private bool _isJumping = false;
    private bool _isFlying = false;

    private int _currentLaneIndex = 1;
    private int _currentFlyingLaneIndex = 0;
    //===============================

    [Header("Jump parameters")]
    [SerializeField] private float _jumpDuration = 1f;
    [SerializeField] private float _jumpHeight = 2f;
    [SerializeField] private AnimationCurve _jumpCurve;

    private PlayerCommand _inputBuffer = PlayerCommand.Idle;

    private PlayerInventoryController _inventoryController;
    private PlayerAnimationController _animationController;

    private Coroutine _flyingCoroutine = null;

    void Start()
    {
        if (_moveAction != null)
        {
            _moveAction.action.performed += HandleMoveCommand;
        }

        if (_jumpAction != null)
        {
            _jumpAction.action.performed += HandleJumpCommand;
        }

        _targetPosition = new Vector3(_lanePositions[_currentLaneIndex].position.x, _flyingPositions[_currentFlyingLaneIndex].position.y, transform.position.z);

        _inventoryController = GetComponent<PlayerInventoryController>();
        _animationController = GetComponent<PlayerAnimationController>();
    }

    private void Update()
    {
        if (_isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _strafeSpeed * Time.deltaTime);

            if (transform.position == _targetPosition) _isMoving = false;

            // Handle the case where we are coming from the flying position
            if (!_isMoving && transform.position.y == _flyingPositions[0].position.y && _isFlying) HandleDescent();
        }
        else if (!_isJumping) HandleInputBufferCommands();
    }

    private void HandleDescent()
    {
        _isFlying = false;
        RunnerEventSystem.OnSpeedTargetChange?.Invoke(SpeedState.Run);
        RunnerEventSystem.OnStartRunning?.Invoke();
        RunnerEventSystem.OnFlyEnd?.Invoke();
    }

    private void HandleInputBufferCommands()
    {
        switch (_inputBuffer)
        {
            case PlayerCommand.Idle : break;
            case PlayerCommand.Jump : HandleJump(); break;
            default: HandleMove(_inputBuffer); break;
        }

        _inputBuffer = PlayerCommand.Idle;
    }

    private void HandleJumpCommand(InputAction.CallbackContext obj)
    {
        _inputBuffer = PlayerCommand.Jump;
    }

    private void HandleJump()
    {
        if (!_isJumping && !_isMoving && !_isFlying)
        {
            StartCoroutine(JumpCoroutine());
        }
    }

    private IEnumerator JumpCoroutine()
    {
        RunnerEventSystem.OnPlayerJump?.Invoke(_jumpDuration);

        _isJumping = true;

        float jumpTimer = 0f;

        while (jumpTimer < _jumpDuration)
        {
            jumpTimer += Time.deltaTime;
            float normalizedTime = jumpTimer / _jumpDuration;

            float targetHeight = (_jumpCurve.Evaluate(normalizedTime) * _jumpHeight);
            Vector3 targetPosition = new Vector3(transform.position.x, targetHeight, transform.position.z);

            transform.position = targetPosition;

            yield return new WaitForEndOfFrame();
        }

        _isJumping = false;
    }

    private void HandleMoveCommand(InputAction.CallbackContext obj)
    {
        Vector2 moveDirection = obj.ReadValue<Vector2>();

        if (moveDirection.x != 0)
        {
            if (moveDirection.x > 0) _inputBuffer = PlayerCommand.MoveRight;
            else _inputBuffer = PlayerCommand.MoveLeft;
        }

        if (moveDirection.y != 0)
        {
            if (moveDirection.y > 0) _inputBuffer = PlayerCommand.MoveUp;
            else _inputBuffer = PlayerCommand.MoveDown;
        }
    }

    private void HandleMove(PlayerCommand command)
    {
        if (!_isMoving && !_isJumping)
        {
            switch (command)
            {
                case PlayerCommand.MoveRight : HandleLateralMovement(Vector2.right); break;
                case PlayerCommand.MoveLeft : HandleLateralMovement(Vector2.left); break;
                case PlayerCommand.MoveUp : HandleFlyMovement(Vector2.up); break;
                case PlayerCommand.MoveDown : HandleFlyMovement(Vector2.down); break;
                default: break;
            }
        }
    }

    private void HandleLateralMovement (Vector2 moveDirection)
    {
        if (moveDirection.x > 0)
        {
            if (_currentLaneIndex < _lanePositions.Length - 1)
            {
                _isMoving = true;

                _currentLaneIndex++;

                _targetPosition = new Vector3(_lanePositions[_currentLaneIndex].position.x, transform.position.y, transform.position.z);
            }
        }
        else
        {
            if (_currentLaneIndex > 0)
            {
                _isMoving = true;

                _currentLaneIndex--;

                _targetPosition = new Vector3(_lanePositions[_currentLaneIndex].position.x, transform.position.y, transform.position.z);
            }
        }
    }

    private void HandleFlyMovement(Vector2 moveDirection)
    {
        if (moveDirection.y > 0)
        {
            if (_currentFlyingLaneIndex == 0)
            {
                FlyUp();
            }
        }
        else if (_currentFlyingLaneIndex > 0)
        {
            FlyDown();
        }
    }

    private void FlyUp()
    {
        if (_inventoryController.Fuel > 0)
        {
            _isMoving = true;
            _isFlying = true;

            RunnerEventSystem.OnSpeedTargetChange?.Invoke(SpeedState.Fly);

            _currentFlyingLaneIndex++;

            _targetPosition.y = _flyingPositions[_currentFlyingLaneIndex].position.y;

            _flyingCoroutine = StartCoroutine(FlyingCoroutine());
        }
    }

    private IEnumerator FlyingCoroutine()
    {
        RunnerEventSystem.OnFlyUp?.Invoke();

        while (_inventoryController.Fuel > 0)
        {
            _inventoryController.Fuel -= 1;

            yield return new WaitForSeconds(1);
        }

        RunnerEventSystem.OnPlayerOutOfFuel?.Invoke(transform.position.y);
        RunnerEventSystem.OnSpeedChange?.Invoke(SpeedState.Stop);
        RunnerEventSystem.OnSpeedTargetChange?.Invoke(SpeedState.Stop);

        yield return new WaitForSeconds(_animationController.DamageAnimationDuration);

        FlyDown();
    }

    private void FlyDown()
    {
        RunnerEventSystem.OnFlyDown?.Invoke();

        _isMoving = true;

        _currentFlyingLaneIndex--;

        _targetPosition.y = _flyingPositions[_currentFlyingLaneIndex].position.y;

        StopCoroutine(_flyingCoroutine);
    }
}
