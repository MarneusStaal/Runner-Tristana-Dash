using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GameState : State
{
    public GameState(StateMachine stateMachine) : base(stateMachine) { }

    private bool _gameOver;

    public override void Enter()
    {
        RunnerEventSystem.OnGameOver += HandleGameOver;
    }

    public override void Update()
    {
        if (_gameOver)
        {
            State gameState = new GameOverState(StateMachine);
            StateMachine.ChangeState(gameState);
        }

        else if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            State gameState = new PauseState(StateMachine);
            StateMachine.ChangeState(gameState);
        }
    }

    public override void Exit()
    {
        RunnerEventSystem.OnGameOver -= HandleGameOver;
    }

    private void HandleGameOver() => _gameOver = true;
}