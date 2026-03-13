using UnityEngine;

public class PlayerCollisionController : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        IObstacle script = other.GetComponent<IObstacle>();

        if (script != null) script.ObstacleEffect();
    }
}
