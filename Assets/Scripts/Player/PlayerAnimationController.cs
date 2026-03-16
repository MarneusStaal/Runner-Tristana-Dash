using System.Collections;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private float _damageAnimationDuration = 1f;
    public float DamageAnimationDuration { get => _damageAnimationDuration; }

    [SerializeField] private float _damageJumpHeight = 1f;
    [SerializeField] private AnimationCurve _damageJumpCurve;
    [SerializeField] private float _animationSpeed = 0.05f;
    [SerializeField] private Animator _playerAnimator;

    [SerializeField]private SkinnedMeshRenderer[] _meshs;

    private void Awake()
    {
        RunnerEventSystem.OnPlayerJump += Jump;
        RunnerEventSystem.OnPlayerOutOfFuel += HandleOutOfFuel;
        RunnerEventSystem.OnFlyingDamage += HandleFlyingDamage;
        RunnerEventSystem.OnFlyDown += FlyDown;
        RunnerEventSystem.OnFlyUp += FlyUp;
        RunnerEventSystem.OnStartRunning += Run;
        RunnerEventSystem.OnStartWalking += Walk;
    }

    private void OnDestroy()
    {
        RunnerEventSystem.OnPlayerJump -= Jump;
        RunnerEventSystem.OnFlyingDamage -= HandleOutOfFuel;
        RunnerEventSystem.OnFlyingDamage += HandleFlyingDamage;
        RunnerEventSystem.OnFlyDown -= FlyDown;
        RunnerEventSystem.OnFlyUp -= FlyUp;
        RunnerEventSystem.OnStartRunning -= Run;
        RunnerEventSystem.OnStartWalking -= Walk;
    }

    private void HandleOutOfFuel()
    {
        StartCoroutine(TakeDamageOutOfFuelCoroutine());
    }

    private IEnumerator TakeDamageOutOfFuelCoroutine()
    {
        float timer = 0f;
        bool isBlinking = false;

        float currentHeight = transform.position.y;

        _playerAnimator.SetBool("IsFlying", false);
        _playerAnimator.SetBool("IsFalling", true);

        DamageFlip();

        while (timer <= _damageAnimationDuration)
        {
            timer += _animationSpeed;

            if (!isBlinking)
            {
                HandleDamageMesh(false);
                isBlinking = true;
            }
            else
            {
                HandleDamageMesh(true);
                isBlinking = false;
            }

            float normalizedTime = timer / _damageAnimationDuration;
            float targetHeight = (_damageJumpCurve.Evaluate(normalizedTime) * _damageJumpHeight) + currentHeight;
            Vector3 targetPosition = new Vector3(transform.position.x, targetHeight, transform.position.z);

            transform.position = targetPosition;

            yield return new WaitForSeconds(_animationSpeed);
        }

        DamageFlip();

        HandleDamageMesh(true);
    }

    private void HandleFlyingDamage()
    {
        StartCoroutine(TakeFlyingDamageCoroutine());
    }

    private IEnumerator TakeFlyingDamageCoroutine()
    {
        float timer = 0f;
        bool isBlinking = false;

        _playerAnimator.SetBool("IsFlying", false);
        _playerAnimator.SetBool("IsFalling", true);

        while (timer <= _damageAnimationDuration)
        {
            timer += _animationSpeed;

            if (!isBlinking)
            {
                HandleDamageMesh(false);
                isBlinking = true;
            }
            else
            {
                HandleDamageMesh(true);
                isBlinking = false;
            }

            yield return new WaitForSeconds(_animationSpeed);
        }

        HandleDamageMesh(true);
    }

    private void HandleDamageMesh(bool enabled)
    {
        for (int i = 0; i < _meshs.Length; i++)
        {
            _meshs[i].enabled = enabled;
        }
    }

    private void DamageFlip()
    {
        if (transform.rotation.eulerAngles.y >= 180f) transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        else transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));
    }

    private void Jump(float jumpDuration)
    {
        StartCoroutine(JumpCoroutine(jumpDuration));
    }

    private IEnumerator JumpCoroutine(float jumpDuration)
    {
        //_playerAnimator.SetTrigger("Jumping");

        _playerAnimator.SetBool("IsJumping", true);
        _playerAnimator.SetBool("IsWalking", false);
        _playerAnimator.SetBool("IsRunning", false);

        yield return new WaitForSeconds(jumpDuration / 2);

        //_playerAnimator.SetTrigger("Falling");
        _playerAnimator.SetBool("IsJumping", false);
        _playerAnimator.SetBool("IsFalling", true);

        yield return new WaitForSeconds(jumpDuration / 2);

        _playerAnimator.SetBool("IsRunning", true);
        _playerAnimator.SetBool("IsFalling", false);
    }

    private void FlyUp()
    {
        //_playerAnimator.SetTrigger("Flying");
        _playerAnimator.SetBool("IsFlying", true);
        _playerAnimator.SetBool("IsRunning", false);
        _playerAnimator.SetBool("IsWalking", false);
    }

    private void FlyDown()
    {
        //_playerAnimator.SetTrigger("Falling");
        _playerAnimator.SetBool("IsFlying", false);
        _playerAnimator.SetBool("IsFalling", true);
    }

    private void Run()
    {
        Debug.Log("Run was called !");
        //_playerAnimator.SetTrigger("Running");
        _playerAnimator.SetBool("IsRunning", true);
        _playerAnimator.SetBool("IsWalking", false);
        _playerAnimator.SetBool("IsFalling", false);
    }

    private void Walk()
    {
        Debug.Log("Walk was called !");
        //_playerAnimator.SetTrigger("Walking");
        _playerAnimator.SetBool("IsRunning", false);
        _playerAnimator.SetBool("IsWalking", true);
    }
}
