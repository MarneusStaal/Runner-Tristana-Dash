using UnityEngine;

public class BlockObstacle : BaseObstacle
{
    public override void ObstacleEffect()
    {
        RunnerEventSystem.OnSpeedChange?.Invoke(SpeedState.Stop);
    }
}
