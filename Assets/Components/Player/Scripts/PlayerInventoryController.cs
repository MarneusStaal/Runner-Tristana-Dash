using System;
using UnityEngine;

public class PlayerInventoryController : MonoBehaviour
{
    [SerializeField] private int _maxFuel = 10;
    [SerializeField] private int _fuel;
    public int Fuel
    {  get
       {
            return _fuel;
       }
       set
       {
            if (value >= 0 &&  value <= _maxFuel) _fuel = value;
            
            RunnerEventSystem.OnFuelLevelChange?.Invoke(Fuel);
       }
    }

    private void Awake()
    {
        RunnerEventSystem.OnCollectablePickUp += HandleCollectablePickUp;
    }

    private void OnDestroy()
    {
        RunnerEventSystem.OnCollectablePickUp -= HandleCollectablePickUp;
    }

    private void HandleCollectablePickUp(CollectableType type)
    {
        if (type == CollectableType.Fuel) Fuel++;
    }
}
