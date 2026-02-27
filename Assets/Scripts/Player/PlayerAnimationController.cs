using System.Collections;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private float _damageAnimationDuration = 1f;
    public float DamageAnimationDuration { get => _damageAnimationDuration; }

    [SerializeField] private float _damageJumpHeight = 1f;
    [SerializeField] private AnimationCurve _damageJumpCurve;
    [SerializeField] private float _animationSpeed = 0.05f;


    private MeshRenderer _mesh;

    private void Start()
    {
        _mesh = GetComponent<MeshRenderer>();
    }

    public void TakeDamage(float currentHeight)
    {
        StartCoroutine(TakeDamageCoroutine(currentHeight));
    }

    private IEnumerator TakeDamageCoroutine(float currentHeight)
    {
        float timer = 0f;
        bool isBlinking = false;

        while (timer <= _damageAnimationDuration)
        {
            timer += _animationSpeed;

            if (!isBlinking)
            {
                _mesh.enabled = false;
                isBlinking = true;
            }
            else
            {
                _mesh.enabled = true;
                isBlinking = false;
            }

            float normalizedTime = timer / _damageAnimationDuration;
            float targetHeight = (_damageJumpCurve.Evaluate(normalizedTime) * _damageJumpHeight) + currentHeight;
            Vector3 targetPosition = new Vector3(transform.position.x, targetHeight, transform.position.z);

            transform.position = targetPosition;

            yield return new WaitForSeconds(_animationSpeed);
        }

        _mesh.enabled = true;
    }
}
