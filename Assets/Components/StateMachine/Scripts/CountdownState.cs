using UnityEngine;

public class CountdownState : State
{
    private float _initialTime = 3.5f;
    private float _timer;

    public float Timer => _timer;

    public CountdownState(StateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        _timer = _initialTime;
    }

    public override void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0)
        {
            return;
        }

        // Go to game state
        State gameState = new GameState(StateMachine);
        StateMachine.ChangeState(gameState);
    }

    public override void Exit()
    {

    }
}

