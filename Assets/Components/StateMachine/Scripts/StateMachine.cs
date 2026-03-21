using UnityEngine;
using UnityEngine.EventSystems;

public class StateMachine
{
    public State CurrentState;

    public void ChangeState(State newState)
    {
        //Debug.Log("Changing state from: " + CurrentState?.GetType().Name + " to: " + newState.GetType().Name);

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();

        RunnerEventSystem.OnStateChanged?.Invoke(CurrentState);
    }

    public void Update()
    {
        CurrentState?.Update();
    }
}
