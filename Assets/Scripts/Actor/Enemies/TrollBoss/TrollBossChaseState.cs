public sealed class TrollBossChaseState : State
{
    private readonly TrollBoss _boss;

    public TrollBossChaseState(TrollBoss boss) => _boss = boss;

    public override void Enter() => _boss.Arise();
    public override void Update() => _boss.UpdateCombat();
}
