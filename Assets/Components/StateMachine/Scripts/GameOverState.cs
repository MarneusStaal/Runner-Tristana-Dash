using UnityEngine;

/// <summary>
/// State entered when the player dies. Responsible for two things:
///   1. Triggering the game-over scene transition.
///   2. Persisting end-of-run data: deactivating potions, recording the run
///      score, updating the all-time best, and either resetting the full
///      save (when all lives are spent) or decrementing lives and carrying
///      collectables forward to the next attempt.
/// </summary>
public class GameOverState : State
{
    public GameOverState(StateMachine stateMachine) : base(stateMachine) { }

    /// <summary>
    /// Called once when the state machine enters this state.
    /// Loads the game-over scene and immediately writes the updated save
    /// so progress is never lost if the app is closed on the results screen.
    /// </summary>
    public override void Enter()
    {
        SceneLoaderService.LoadGameOver();

        SaveData save = CreateSaveData();
        SaveService.Save(save);
    }

    /// <summary>
    /// No per-frame logic is needed in the game-over state;
    /// progression is event- or UI-driven from the results screen.
    /// </summary>
    public override void Update() { }

    /// <summary>
    /// No cleanup is needed when leaving this state;
    /// the next state sets up its own context independently.
    /// </summary>
    public override void Exit() { }

    // ── Save Helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the <see cref="SaveData"/> object to persist after this run ends.
    ///
    /// Logic summary:
    /// <list type="bullet">
    ///   <item>All active potion effects are cleared (they don't survive death).</item>
    ///   <item>The current run score is appended to the score history.</item>
    ///   <item>The cumulative best score is updated if the new total is higher.</item>
    ///   <item>
    ///     If the number of recorded scores has reached <see cref="PlayerInventoryController.MaxLives"/>,
    ///     all lives are spent: the run history and all collectables are wiped and
    ///     lives are reset to the maximum for a fresh start.
    ///   </item>
    ///   <item>
    ///     Otherwise, one life is deducted and collectables are carried forward
    ///     so the player keeps what they gathered this run.
    ///   </item>
    /// </list>
    /// </summary>
    private SaveData CreateSaveData()
    {
        // Start from the existing save so persistent data (best score, history) is preserved
        SaveData save = SaveService.Load();

        // ── Deactivate all potion effects ────────────────────────────────────
        // Potions are single-run bonuses and must not carry over after death
        save.RedGreenPotionActive = false;
        save.RedBluePotionActive = false;
        save.RedCandlePotionActive = false;

        // ── Record this run's score ──────────────────────────────────────────
        // Each entry in save.scores represents one completed run
        save.scores.Add(GameManager.Instance.Score);

        // ── Update the all-time best score ───────────────────────────────────
        // The best score is defined as the highest ever cumulative total across
        // all runs in the current life cycle (scores list)
        int tempBest = 0;
        foreach (int score in save.scores)
            tempBest += score;

        if (tempBest > save.BestScore)
            save.BestScore = tempBest;

        // ── Determine outcome: game-over reset vs. life-lost continue ────────
        if (save.scores.Count >= PlayerInventoryController.MaxLives)
        {
            // All lives exhausted — full reset for the next play-through:
            // clear run history so the score cycle starts fresh
            save.scores.Clear();

            // Restore lives to the maximum so the player starts with a full set
            save.Lives = PlayerInventoryController.MaxLives;

            // Wipe all collectables — a full reset grants no carry-over inventory
            save.GreenBottleCount = 0;
            save.BlueBottleCount = 0;
            save.RedBottleCount = 0;
            save.CandleCount = 0;
        }
        else
        {
            // Lives remain — deduct one and carry the current inventory forward
            // so the player keeps collectables gathered during this run
            save.Lives = PlayerInventoryController.Instance.Lives - 1;

            save.GreenBottleCount = PlayerInventoryController.Instance.GreenBottleCount;
            save.BlueBottleCount = PlayerInventoryController.Instance.BlueBottleCount;
            save.RedBottleCount = PlayerInventoryController.Instance.RedBottleCount;
            save.CandleCount = PlayerInventoryController.Instance.CandleCount;
        }

        return save;
    }
}