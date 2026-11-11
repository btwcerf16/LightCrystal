public sealed class FireMageDeathState : State
{
    private readonly FireMage _mage;

    public FireMageDeathState(FireMage mage) => _mage = mage;

    public override void Enter() => _mage.Death();
}
