using UnityEngine;

public class BlockObstacle : BaseObstacle
{
    public override void ObstacleEffect(bool isFlying)
    {
        RunnerEventSystem.OnSpeedChange?.Invoke(SpeedState.Stop);

        if (isFlying) RunnerEventSystem.OnFlyingDamage?.Invoke();
    }
}
