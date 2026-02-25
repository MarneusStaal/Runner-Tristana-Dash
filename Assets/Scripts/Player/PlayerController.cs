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

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _jumpAction;

    private Rigidbody _rb;

    [SerializeField] private float _leftPositionX;
    [SerializeField] private float _centerPositionX;
    [SerializeField] private float _rightPositionX;
    [SerializeField] private float _flyingPositionY;

    private Vector3 _targetPosition;
    private bool _isMoving = false;
    private bool _isJumping = false;
    private bool _isFlying = false;

    [SerializeField] private float _jumpForce = 10f;
    [SerializeField] private float _strafeSpeed = 10f;

    private PlayerCommand _inputBuffer = PlayerCommand.Idle;

#if UNITY_EDITOR
    [SerializeField] private InputActionReference _addFlyResourceAction;
#endif

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

#if UNITY_EDITOR
        if (_addFlyResourceAction != null)
        {
            _addFlyResourceAction.action.performed += AddFlyResource;
        }
#endif

        _targetPosition = new Vector3(0, 1, 0);

        _rb = GetComponent<Rigidbody>();
    }

#if UNITY_EDITOR
    private void AddFlyResource(InputAction.CallbackContext obj)
    {
        GameManager.Instance.UpdateFlyBar(1);
    }
#endif

    void FixedUpdate()
    {
        if (_isMoving)
        {
            Vector3 tempTarget = Vector3.MoveTowards(transform.position, _targetPosition, _strafeSpeed * Time.fixedDeltaTime);

            _rb.MovePosition(tempTarget);

            if (transform.position == _targetPosition) _isMoving = false;
        }
        else if (!_isJumping) HandleInputBufferCommands();
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
            _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            _isJumping = true;
        }
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
            if (transform.position.x != _rightPositionX)
            {
                _isMoving = true;

                if (transform.position.x - 0.1f <= _leftPositionX && transform.position.x + 0.1f > _leftPositionX) _targetPosition.x = _centerPositionX;
                else if (transform.position.x - 0.1f <= _centerPositionX && transform.position.x + 0.1f > _centerPositionX) _targetPosition.x = _rightPositionX;
            }
        }
        else
        {
            if (transform.position.x != _leftPositionX)
            {
                _isMoving = true;

                if (transform.position.x - 0.1f <= _rightPositionX && transform.position.x + 0.1f > _rightPositionX) _targetPosition.x = _centerPositionX;
                else if (transform.position.x - 0.1f <= _centerPositionX && transform.position.x + 0.1f > _centerPositionX) _targetPosition.x = _leftPositionX;
            }
        }
    }

    private void HandleFlyMovement(Vector2 moveDirection)
    {
        if (moveDirection.y > 0)
        {
            if (transform.position.y != _flyingPositionY)
            {
                _isMoving = true;
                _isFlying = true;

                _targetPosition.y = _flyingPositionY;
                _rb.useGravity = false;
            }
        }
        else
        {
            _isMoving = true;
            _isFlying = false;

            _targetPosition.y = 1;
            _rb.useGravity = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Ground") _isJumping = false;
    }
}
