using UnityEngine;

public abstract class BaseObstacle : MonoBehaviour, IObstacle
{
    public virtual void ObstacleEffect(bool isFlying)
    {
        throw new System.NotImplementedException();
    }
}
