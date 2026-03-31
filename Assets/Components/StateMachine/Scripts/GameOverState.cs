using UnityEngine;

public class GameOverState : State
{
    public GameOverState(StateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        SceneLoaderService.LoadGameOver();

        SaveData save = CreateSaveData();
        SaveService.Save(save);
    }

    public override void Update()
    {
    }

    public override void Exit()
    {

    }

    private SaveData CreateSaveData()
    {
        SaveData save = SaveService.Load();

        save.scores.Add(GameManager.Instance.Score);

        int tempBest = 0;
        foreach (int score in save.scores)
        {
            tempBest += score;
        }

        if (tempBest > save.BestScore) save.BestScore = tempBest;

        if (save.scores.Count >= PlayerInventoryController.MaxLives)
        {
            save.scores.Clear();

            save.Lives = PlayerInventoryController.MaxLives;

            save.GreenBottleCount = 0;
            save.BlueBottleCount = 0;
            save.RedBottleCount = 0;
            save.CandleCount = 0;
        }
        else
        {
            save.Lives = PlayerInventoryController.Instance.Lives -  1;

            save.GreenBottleCount = PlayerInventoryController.Instance.GreenBottleCount;
            save.BlueBottleCount = PlayerInventoryController.Instance.BlueBottleCount;
            save.RedBottleCount = PlayerInventoryController.Instance.RedBottleCount;
            save.CandleCount = PlayerInventoryController.Instance.CandleCount;
        }

        return save;
    }
}
