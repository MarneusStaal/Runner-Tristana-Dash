using UnityEngine;

public class GameOverState : State
{
    public GameOverState(StateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        SceneLoaderService.LoadGameOver();
    }

    public override void Update()
    {
    }

    public override void Exit()
    {

    }
}
