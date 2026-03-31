using UnityEngine;

public class BlueBottleCollectable : BaseCollectable
{
    public override void CollectableEffect()
    {
        RunnerEventSystem.OnCollectablePickUp?.Invoke(CollectableType.BlueBottle);

        Destroy(gameObject);
    }
}
