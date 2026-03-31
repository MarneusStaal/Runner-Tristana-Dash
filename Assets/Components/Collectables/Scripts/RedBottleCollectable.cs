using UnityEngine;

public class RedBottleCollectable : BaseCollectable
{
    public override void CollectableEffect()
    {
        RunnerEventSystem.OnCollectablePickUp?.Invoke(CollectableType.RedBottle);

        Destroy(gameObject);
    }
}
