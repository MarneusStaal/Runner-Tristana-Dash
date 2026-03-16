using UnityEngine;

public class ShortSlowObstacle : BaseObstacle
{
    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider>();
    }

    public override void ObstacleEffect(bool isFlying)
    {
        RunnerEventSystem.OnSpeedChange?.Invoke(SpeedState.Walk);
        
        if (_collider != null) _collider.enabled = false;
    }
}
