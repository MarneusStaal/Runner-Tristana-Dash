using UnityEngine;

public class GreenBottleCollectable : BaseCollectable
{
    public override void CollectableEffect()
    {
        RunnerEventSystem.OnCollectablePickUp?.Invoke(CollectableType.GreenBottle);

        Destroy(gameObject);
    }
}
