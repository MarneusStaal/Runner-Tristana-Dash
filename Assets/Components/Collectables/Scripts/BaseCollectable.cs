using UnityEngine;

public abstract class BaseCollectable : MonoBehaviour, ICollectable
{
    public virtual void CollectableEffect()
    {
        throw new System.NotImplementedException();
    }
}
