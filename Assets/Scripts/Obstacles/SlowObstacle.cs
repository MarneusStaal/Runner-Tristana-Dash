using UnityEngine;

public class SlowObstacle : BaseObstacle
{
    public override void ObstacleEffect(bool isFlying)
    {
        RunnerEventSystem.OnSpeedChange?.Invoke(SpeedState.Walk);
    }
}
