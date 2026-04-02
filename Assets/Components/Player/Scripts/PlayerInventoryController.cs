using System;
using UnityEngine;

/// <summary>
/// Singleton controller that manages the player's inventory:
/// fuel, collectables (red/green/blue bottles, candles), lives,
/// and the red-blue potion double-pickup effect.
///
/// All inventory values are exposed as properties that enforce
/// clamping to [0, max] and fire the appropriate events so the UI
/// and other systems stay in sync automatically.
/// </summary>
public class PlayerInventoryController : MonoBehaviour
{
    /// <summary>
    /// Global singleton reference. Set during Awake; any duplicate
    /// instance is immediately destroyed.
    /// </summary>
    public static PlayerInventoryController Instance;

    /// <summary>
    /// Maximum number of lives the player can hold.
    /// Exposed publicly so other systems (e.g. UI, game-over logic) can read it.
    /// </summary>
    [SerializeField] public static int MaxLives = 5;

    // Current live count. Read externally via the Lives property.
    // Written only through save-load to avoid bypassing future validation.
    private int _lives = 0;
    public int Lives => _lives;

    // ── Red-Blue Potion ─────────────────────────────────────────────────────
    // When active, every collectable pick-up grants 2 units instead of 1.

    /// <summary>Whether the double-pickup potion effect is currently running.</summary>
    private bool _redBluePotionActive = false;

    /// <summary>How many seconds the red-blue potion effect lasts.</summary>
    [SerializeField] private float _redBluePotionTimer = 15f;

    /// <summary>Elapsed time since the potion was activated; reset when it expires.</summary>
    private float _timer = 0f;

    // ── Fuel ────────────────────────────────────────────────────────────────

    /// <summary>Maximum amount of fuel the player can carry.</summary>
    [SerializeField] private int _maxFuel = 10;

    /// <summary>Maximum amount of any generic resource (bottles, candles).</summary>
    [SerializeField] private int _maxResource = 10;

    /// <summary>
    /// Backing field for <see cref="Fuel"/>.
    /// Always access fuel through the property so clamping and events are guaranteed.
    /// </summary>
    [SerializeField] private int _fuel;

    /// <summary>
    /// Current fuel level. Assignments outside [0, <see cref="_maxFuel"/>] are
    /// silently ignored. Every assignment (including no-ops) broadcasts
    /// <see cref="RunnerEventSystem.OnFuelLevelChange"/> so the UI never drifts.
    /// </summary>
    public int Fuel
    {
        get => _fuel;
        set
        {
            // Only update the backing field if the new value is within valid range.
            // Out-of-range writes are discarded rather than clamped so callers
            // don't accidentally overfill or underflow the tank.
            if (value >= 0 && value <= _maxFuel)
                _fuel = value;

            // Notify subscribers (e.g. fuel-bar UI) with the current level.
            // Fired unconditionally so the UI stays accurate even when a
            // write was rejected.
            RunnerEventSystem.OnFuelLevelChange?.Invoke(Fuel);
        }
    }

    // ── Collectable Resources ────────────────────────────────────────────────
    // Each resource follows the same pattern:
    //   • Backing field serialised for Inspector visibility / save-load.
    //   • Property clamps to [0, _maxResource] and fires OnCollectableValueChanged.

    [SerializeField] private int _redBottleCount = 0;
    /// <summary>Number of red bottles currently in the player's inventory.</summary>
    public int RedBottleCount
    {
        get => _redBottleCount;
        set
        {
            if (value >= 0 && value <= _maxResource)
                _redBottleCount = value;

            RunnerEventSystem.OnCollectableValueChanged?.Invoke();
        }
    }

    [SerializeField] private int _greenBottleCount = 0;
    /// <summary>Number of green bottles currently in the player's inventory.</summary>
    public int GreenBottleCount
    {
        get => _greenBottleCount;
        set
        {
            if (value >= 0 && value <= _maxResource)
                _greenBottleCount = value;

            RunnerEventSystem.OnCollectableValueChanged?.Invoke();
        }
    }

    [SerializeField] private int _blueBottleCount = 0;
    /// <summary>Number of blue bottles currently in the player's inventory.</summary>
    public int BlueBottleCount
    {
        get => _blueBottleCount;
        set
        {
            if (value >= 0 && value <= _maxResource)
                _blueBottleCount = value;

            RunnerEventSystem.OnCollectableValueChanged?.Invoke();
        }
    }

    [SerializeField] private int _candleCount = 0;
    /// <summary>Number of candles currently in the player's inventory.</summary>
    public int CandleCount
    {
        get => _candleCount;
        set
        {
            if (value >= 0 && value <= _maxResource)
                _candleCount = value;

            RunnerEventSystem.OnCollectableValueChanged?.Invoke();
        }
    }

    // ── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        // Enforce singleton: keep the first instance, destroy any duplicates
        // that may appear (e.g. after a scene reload).
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return; // Skip event subscription for the duplicate
        }

        // Subscribe to game events. Matching unsubscription happens in OnDestroy.
        RunnerEventSystem.OnCollectablePickUp += HandleCollectablePickUp;
        RunnerEventSystem.OnSaveLoaded += HandleSaveLoaded;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent ghost callbacks after this object is destroyed
        // (e.g. if the player object is torn down between scenes).
        RunnerEventSystem.OnCollectablePickUp -= HandleCollectablePickUp;
        RunnerEventSystem.OnSaveLoaded -= HandleSaveLoaded;
    }

    private void Update()
    {
        // Only run the potion countdown when the effect is active,
        // keeping the Update cost near-zero the rest of the time.
        if (!_redBluePotionActive) return;

        _timer += Time.deltaTime;

        // Expire the potion once its duration has elapsed.
        if (_timer > _redBluePotionTimer)
        {
            _redBluePotionActive = false;
            _timer = 0f;
        }
    }

    // ── Event Handlers ───────────────────────────────────────────────────────

    /// <summary>
    /// Responds to <see cref="RunnerEventSystem.OnCollectablePickUp"/>.
    /// Increments the matching inventory slot by 1 normally, or by 2 when
    /// the red-blue potion double-pickup effect is active.
    /// </summary>
    private void HandleCollectablePickUp(CollectableType type)
    {
        // Determine the bonus multiplier once rather than repeating the ternary
        // inside each case. With potion active the increment is 2, otherwise 1.
        int increment = _redBluePotionActive ? 2 : 1;

        switch (type)
        {
            case CollectableType.Fuel: Fuel += increment; break;
            case CollectableType.RedBottle: RedBottleCount += increment; break;
            case CollectableType.GreenBottle: GreenBottleCount += increment; break;
            case CollectableType.BlueBottle: BlueBottleCount += increment; break;
            case CollectableType.Candle: CandleCount += increment; break;
            // Unknown types are intentionally ignored; no error is raised
            // to keep new collectable types from breaking existing save files.
            default: break;
        }
    }

    /// <summary>
    /// Responds to <see cref="RunnerEventSystem.OnSaveLoaded"/>.
    /// Restores all inventory values from the provided save data, bypassing
    /// the property setters for <c>_lives</c> since lives are not validated
    /// the same way as collectables.
    /// </summary>
    private void HandleSaveLoaded(SaveData data)
    {
        // Restore collectable counts through their properties so clamping
        // and UI-update events fire as normal.
        RedBottleCount = data.RedBottleCount;
        BlueBottleCount = data.BlueBottleCount;
        GreenBottleCount = data.GreenBottleCount;
        CandleCount = data.CandleCount;

        // Lives are written directly to the backing field; add a setter
        // with validation here if lives need range enforcement in future.
        _lives = data.Lives;

        // Re-activate the potion effect if it was running when the game was saved.
        // Note: the remaining duration is not saved, so the full timer restarts.
        if (data.RedBluePotionActive)
            _redBluePotionActive = true;
    }
}