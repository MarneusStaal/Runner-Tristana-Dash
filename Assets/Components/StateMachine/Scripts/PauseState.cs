using UnityEngine;
using UnityEngine.InputSystem;

public class PauseState : State
{
    public PauseState(StateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        SceneLoaderService.LoadPauseMenu();
    }

    public override void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            State gameState = new GameState(StateMachine);
            StateMachine.ChangeState(gameState);
        }
    }

    public override void Exit()
    {
        SceneLoaderService.UnloadPauseMenu();
    }
}
