using UnityEngine;

/// <summary>
/// Attached to a spawn-point object in the level, this component decides
/// whether to spawn a collectable at that point and, if so, which one.
///
/// The selection uses a two-stage weighted random process:
///   1. A single roll determines whether anything spawns at all, and if so
///      whether it will be a fuel pickup or a resource (bottle/candle).
///   2. If a resource was chosen, a second roll picks the specific type,
///      with rarer items (candle, blue bottle) gated behind lower thresholds.
///
/// All thresholds are serialised so designers can tune spawn rates in the Inspector
/// without touching code.
/// </summary>
public class CollectableSpawnController : MonoBehaviour
{
    // ── Collectable Prefabs ──────────────────────────────────────────────────
    // Assigned in the Inspector; Instantiated by InitCollectable when selected.

    [SerializeField] private GameObject _redBottle;
    [SerializeField] private GameObject _greenBottle;
    [SerializeField] private GameObject _blueBottle;
    [SerializeField] private GameObject _candle;
    [SerializeField] private GameObject _fuel;

    // ── Spawn Chance Thresholds ──────────────────────────────────────────────
    // All values are compared against a roll in [0, 100).
    // A LOWER threshold means RARER (fewer rolls will land at or below it).

    [Header("Spawn Chances")]

    /// <summary>
    /// Roll must be AT OR BELOW this value for a resource (bottle/candle) to
    /// spawn instead of fuel. Values above this but below _fuelSpawnChance
    /// result in a fuel pickup.
    /// </summary>
    [SerializeField] private int _resourceSpawnChance = 20;

    /// <summary>
    /// Roll must be AT OR BELOW this value for anything to spawn at all.
    /// Rolls above this threshold produce an empty spawn point.
    /// </summary>
    [SerializeField] private int _fuelSpawnChance = 50;

    /// <summary>Cumulative threshold for spawning a red bottle in the resource roll.</summary>
    [SerializeField] private int _redBottleSpawnChance = 70;

    /// <summary>Cumulative threshold for spawning a green bottle in the resource roll.</summary>
    [SerializeField] private int _greenBottleSpawnChance = 50;

    /// <summary>Cumulative threshold for spawning a blue bottle in the resource roll.</summary>
    [SerializeField] private int _blueBottleSpawnChance = 20;

    /// <summary>
    /// Cumulative threshold for spawning a candle in the resource roll.
    /// Checked first, making it the rarest resource since it occupies the
    /// smallest slice of [0, 100).
    /// </summary>
    [SerializeField] private int _candleSpawnChance = 10;

    // ── Unity Lifecycle ──────────────────────────────────────────────────────

    /// <summary>
    /// Runs the two-stage spawn lottery once when the spawn point is created.
    /// Nothing is spawned if the dice rolls unfavourably at either stage.
    ///
    /// Stage 1 — spawn gate and type selection (single roll):
    /// <list type="bullet">
    ///   <item>roll &gt; _fuelSpawnChance  → nothing spawns (empty point)</item>
    ///   <item>roll &gt; _resourceSpawnChance → fuel pickup spawns</item>
    ///   <item>roll ≤ _resourceSpawnChance  → proceed to stage 2 (resource roll)</item>
    /// </list>
    ///
    /// Stage 2 — resource type selection (fresh roll, checked low → high):
    /// <list type="bullet">
    ///   <item>≤ _candleSpawnChance      → candle (rarest)</item>
    ///   <item>≤ _blueBottleSpawnChance  → blue bottle</item>
    ///   <item>≤ _greenBottleSpawnChance → green bottle</item>
    ///   <item>≤ _redBottleSpawnChance   → red bottle (most common resource)</item>
    ///   <item>above all thresholds      → nothing spawns</item>
    /// </list>
    /// </summary>
    private void Start()
    {
        int randomNumber = Random.Range(0, 100);

        // ── Stage 1: spawn gate ──────────────────────────────────────────────
        // If the roll is above the fuel threshold, this spawn point stays empty
        if (randomNumber > _fuelSpawnChance)
            return;

        // Roll landed in the fuel-only zone (above resource threshold, within fuel threshold)
        // → spawn a fuel pickup and exit
        if (randomNumber > _resourceSpawnChance)
        {
            InitCollectable(_fuel);
            return;
        }

        // ── Stage 2: resource type selection ────────────────────────────────
        // A fresh roll gives each resource type its own independent probability slice.
        // Thresholds are checked from lowest to highest so rarer types are
        // not accidentally swallowed by a wider threshold checked first.
        randomNumber = Random.Range(0, 100);

        if (randomNumber <= _candleSpawnChance) InitCollectable(_candle);
        else if (randomNumber <= _blueBottleSpawnChance) InitCollectable(_blueBottle);
        else if (randomNumber <= _greenBottleSpawnChance) InitCollectable(_greenBottle);
        else if (randomNumber <= _redBottleSpawnChance) InitCollectable(_redBottle);
        // Rolls above all thresholds → no resource spawns (intentional gap)
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Instantiates the given collectable prefab at this spawn point's world position
    /// and parents it to this transform so it moves with the level geometry if needed.
    /// </summary>
    private void InitCollectable(GameObject collectable)
    {
        GameObject temp = Instantiate(collectable, transform.position, Quaternion.identity);
        temp.transform.parent = transform;
    }
}