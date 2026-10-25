public sealed class StateMachine
{
    public State CurrentState { get; private set; }

    public void Initialize(State startingState)
    {
        ChangeState(startingState);
    }

    public void ChangeState(State state)
    {
        if (state == null || CurrentState == state)
            return;

        State previous = CurrentState;
        CurrentState = null;
        previous?.Exit();
        CurrentState = state;
        state.Enter();
    }

    public void Update()
    {
        CurrentState?.Update();
    }

    public void Stop()
    {
        State previous = CurrentState;
        CurrentState = null;
        previous?.Exit();
    }
}
