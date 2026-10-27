public abstract class PassiveSpell : Spell
{
    public override void Activate()
    {
        if (IsActive)
            return;
        base.Activate();
        Apply();
    }

    public override void Deactivate()
    {
        if (!IsActive)
            return;
        Remove();
        base.Deactivate();
    }

    public override bool CanCast(SpellCastData data) => false;
    protected override void Cast(SpellCastData data) { }
    protected abstract void Apply();
    protected abstract void Remove();
}
