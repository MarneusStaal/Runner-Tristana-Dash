using UnityEngine;

public class PlayerCollisionController : MonoBehaviour
{
    private bool _isFlying;

    private void Awake()
    {
        RunnerEventSystem.OnFlyUp += OnFlyUp;
        RunnerEventSystem.OnFlyDown += OnFlyDown;
    }

    private void OnDestroy()
    {
        RunnerEventSystem.OnFlyUp -= OnFlyUp;
        RunnerEventSystem.OnFlyDown -= OnFlyDown;
    }

    private void OnFlyUp() => _isFlying = true;

    private void OnFlyDown() => _isFlying = false;

    private void OnTriggerStay(Collider other)
    {
        IObstacle script = other.GetComponent<IObstacle>();

        if (script != null) script.ObstacleEffect(_isFlying);
    }
}
