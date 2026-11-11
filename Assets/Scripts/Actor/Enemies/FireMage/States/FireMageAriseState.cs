public sealed class FireMageAriseState : State
{
    private readonly FireMage _mage;

    public FireMageAriseState(FireMage mage) => _mage = mage;

    public override void Enter() => _mage.Arise();
}
