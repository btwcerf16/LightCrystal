public sealed class TrollBossDeathState : State
{
    private readonly TrollBoss _boss;

    public TrollBossDeathState(TrollBoss boss) => _boss = boss;

    public override void Enter() => _boss.Death();
}
