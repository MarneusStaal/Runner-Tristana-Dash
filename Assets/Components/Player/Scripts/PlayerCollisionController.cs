using UnityEngine;

// Handles the player's trigger collisions, delegating effects to the collided object.
// Tracks the player's flying state so obstacles can apply the appropriate effect
public class PlayerCollisionController : MonoBehaviour
{
    // True while the player is in flying mode; passed to obstacles so they can react accordingly
    private bool _isFlying;

    private void Awake()
    {
        RunnerEventSystem.OnFlyUp += OnFlyUp;
        RunnerEventSystem.OnFlyDown += OnFlyDown;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent ghost callbacks after the object is destroyed
        RunnerEventSystem.OnFlyUp -= OnFlyUp;
        RunnerEventSystem.OnFlyDown -= OnFlyDown;
    }

    // Set when the player lifts off; ensures subsequent collisions are treated as airborne
    private void OnFlyUp() => _isFlying = true;

    // Cleared when the player lands; subsequent collisions are treated as ground-level
    private void OnFlyDown() => _isFlying = false;

    // Called every frame the player remains inside a trigger collider.
    // Uses interface checks to keep this controller decoupled from specific obstacle/collectable types.
    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<IObstacle>() is IObstacle obstacle)
        {
            // Delegate the effect to the obstacle, passing the flying state so it can
            // decide the appropriate response (e.g. flying damage vs. ground damage)
            obstacle.ObstacleEffect(_isFlying);
        }
        else if (other.GetComponent<ICollectable>() is ICollectable collectable)
        {
            // Delegate the pickup effect to the collectable (e.g. collect fuel, coins, etc.)
            collectable.CollectableEffect();
        }
    }
}