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

    private void Start()
    {
    }

    public void TakeDamage(float currentHeight)
    {
        StartCoroutine(TakeFlyingDamageCoroutine(currentHeight));
    }

    private IEnumerator TakeFlyingDamageCoroutine(float currentHeight)
    {
        float timer = 0f;
        bool isBlinking = false;

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

        //_playerAnimator.SetBool("IsFalling", false);

        DamageFlip();

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

    public void Jump(float jumpDuration)
    {
        StartCoroutine(JumpCoroutine(jumpDuration));
    }

    private IEnumerator JumpCoroutine(float jumpDuration)
    {
        _playerAnimator.SetBool("IsJumping", true);

        yield return new WaitForSeconds(jumpDuration / 2);

        _playerAnimator.SetBool("IsJumping", false);
        _playerAnimator.SetBool("IsFalling", true);

        yield return new WaitForSeconds(jumpDuration / 2);

        _playerAnimator.SetBool("IsFalling", false);
    }

    public void FlyUp()
    {
        _playerAnimator.SetBool("IsFlying", true);
    }

    public void FlyDown()
    {
        _playerAnimator.SetBool("IsFlying", false);
        _playerAnimator.SetBool("IsFalling", true);
    }

    public void Run()
    {
        _playerAnimator.SetBool("IsFlying", false);
        _playerAnimator.SetBool("IsFalling", false);
        _playerAnimator.SetBool("IsJumping", false);
        _playerAnimator.SetBool("IsWalking", false);
    }
}
