public sealed class SpiderAriseState : State
{
    private readonly Spider _spider;
    public SpiderAriseState(Spider spider) => _spider = spider;
    public override void Enter() => _spider.Arise();
}
