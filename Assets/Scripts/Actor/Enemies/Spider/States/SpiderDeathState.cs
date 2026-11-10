public sealed class SpiderDeathState : State
{
    private readonly Spider _spider;
    public SpiderDeathState(Spider spider) => _spider = spider;
    public override void Enter() => _spider.Death();
}
