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
        if (other.GetComponent<IObstacle>() is IObstacle obstacle)
        {
            obstacle.ObstacleEffect(_isFlying);
        }
        else if (other.GetComponent<ICollectable>() is ICollectable collectable)
        {
            collectable.CollectableEffect();
        }
    }
}
