public class WaveStartState : State
{
    private readonly WaveDirector _director;

    public WaveStartState(WaveDirector director)
    {
        _director = director;
    }

    public override void Enter()
    {
        _director.StartWave();
    }

    public override void Update()
    {
        _director.UpdateWave();
    }
}
