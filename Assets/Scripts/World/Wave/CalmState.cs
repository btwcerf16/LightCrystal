public class CalmState : State
{
    private readonly WaveDirector _director;

    public CalmState(WaveDirector director)
    {
        _director = director;
    }

    public override void Enter()
    {
        _director.StartCalm();
    }

    public override void Update()
    {
        _director.UpdateCalm(UnityEngine.Time.deltaTime);
    }
}
