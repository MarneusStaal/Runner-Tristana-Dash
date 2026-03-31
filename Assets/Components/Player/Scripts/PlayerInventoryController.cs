using System;
using UnityEngine;

// Manages the player's inventory, currently limited to fuel.
// The Fuel property enforces clamping and automatically notifies the UI on every change.
public class PlayerInventoryController : MonoBehaviour
{
    public static PlayerInventoryController Instance;

    [SerializeField] public static int MaxLives = 5;

    private int _lives = 0;
    public int Lives => _lives;

    // Upper bound for the fuel value; fuel cannot be added beyond this amount
    [SerializeField] private int _maxFuel = 10;

    [SerializeField] private int _maxResource = 10;
    // Backing field for the Fuel property; use the property externally to ensure clamping and events fire
    [SerializeField] private int _fuel;
    public int Fuel
    {
        get
        {
            return _fuel;
        }
        set
        {
            // Silently ignore out-of-range assignments (below 0 or above _maxFuel)
            if (value >= 0 && value <= _maxFuel) _fuel = value;

            // Always broadcast the current fuel level, even if the value was clamped,
            // so the UI stays in sync regardless of the source of the change
            RunnerEventSystem.OnFuelLevelChange?.Invoke(Fuel);
        }
    }

    // =======================================

    [SerializeField] private int _redBottleCount = 0;
    public int RedBottleCount
    {
        get
        {
            return _redBottleCount;
        }
        set
        {
            // Silently ignore out-of-range assignments (below 0 or above _maxFuel)
            if (value >= 0 && value <= _maxResource) _redBottleCount = value;

            // Always broadcast the current fuel level, even if the value was clamped,
            // so the UI stays in sync regardless of the source of the change
            RunnerEventSystem.OnCollectableValueChanged?.Invoke();
        }
    }

    [SerializeField] private int _greenBottleCount = 0;
    public int GreenBottleCount
    {
        get
        {
            return _greenBottleCount;
        }
        set
        {
            // Silently ignore out-of-range assignments (below 0 or above _maxFuel)
            if (value >= 0 && value <= _maxResource) _greenBottleCount = value;

            // Always broadcast the current fuel level, even if the value was clamped,
            // so the UI stays in sync regardless of the source of the change
            RunnerEventSystem.OnCollectableValueChanged?.Invoke();
        }
    }

    [SerializeField] private int _blueBottleCount = 0;
    public int BlueBottleCount
    {
        get
        {
            return _blueBottleCount;
        }
        set
        {
            // Silently ignore out-of-range assignments (below 0 or above _maxFuel)
            if (value >= 0 && value <= _maxResource) _blueBottleCount = value;

            // Always broadcast the current fuel level, even if the value was clamped,
            // so the UI stays in sync regardless of the source of the change
            RunnerEventSystem.OnCollectableValueChanged?.Invoke();
        }
    }

    [SerializeField] private int _candleCount = 0;
    public int CandleCount
    {
        get
        {
            return _candleCount;
        }
        set
        {
            // Silently ignore out-of-range assignments (below 0 or above _maxFuel)
            if (value >= 0 && value <= _maxResource) _candleCount = value;

            // Always broadcast the current fuel level, even if the value was clamped,
            // so the UI stays in sync regardless of the source of the change
            RunnerEventSystem.OnCollectableValueChanged?.Invoke();
        }
    }

    // =======================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        RunnerEventSystem.OnCollectablePickUp += HandleCollectablePickUp;
        RunnerEventSystem.OnSaveLoaded += HandleSaveLoaded;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent ghost callbacks after the object is destroyed
        RunnerEventSystem.OnCollectablePickUp -= HandleCollectablePickUp;
        RunnerEventSystem.OnSaveLoaded -= HandleSaveLoaded;
    }

    // Called when the player picks up any collectable; increments the correct collectable type in the inventory
    private void HandleCollectablePickUp(CollectableType type)
    {
        switch (type)
        {
            case CollectableType.Fuel: Fuel++; break;
            case CollectableType.RedBottle: RedBottleCount++; break;
            case CollectableType.GreenBottle: GreenBottleCount++; break;
            case CollectableType.BlueBottle: BlueBottleCount++; break;
            case CollectableType.Candle: CandleCount++; break;
            default: break;
        }
    }

    private void HandleSaveLoaded(SaveData data)
    {
        RedBottleCount = data.RedBottleCount;
        BlueBottleCount = data.BlueBottleCount;
        GreenBottleCount = data.GreenBottleCount;
        CandleCount = data.CandleCount;
        _lives = data.Lives;
    }
}