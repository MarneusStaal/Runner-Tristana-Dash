using UnityEngine;

public class CandleCollectable : BaseCollectable
{
    public override void CollectableEffect()
    {
        RunnerEventSystem.OnCollectablePickUp?.Invoke(CollectableType.Candle);

        Destroy(gameObject);
    }
}
