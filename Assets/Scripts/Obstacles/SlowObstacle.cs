using UnityEngine;

public class SlowObstacle : BaseObstacle
{
    public override void ObstacleEffect()
    {
        RunnerEventSystem.OnSpeedChange?.Invoke(SpeedState.Walk);
    }
}
