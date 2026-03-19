using UnityEngine;

public class FuelCollectable : BaseCollectable
{
    public override void CollectableEffect()
    {
        RunnerEventSystem.OnCollectablePickUp?.Invoke(CollectableType.Fuel);

        Destroy(gameObject);
    }
}
