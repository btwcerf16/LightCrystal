public abstract class ToggleSpell : Spell
{
    private bool _isToggled;
    public virtual bool IsToggled => _isToggled;
    public override int MaxCharges => 1;

    protected sealed override void Cast(SpellCastData data)
    {
        if (IsToggled)
        {
            StopToggle();
            return;
        }

        _isToggled = true;
        OnToggleChanged(true);
    }

    protected virtual void OnToggleChanged(bool isToggled) { }

    protected void StopToggle()
    {
        if (!_isToggled)
            return;

        _isToggled = false;
        OnToggleChanged(false);
    }

    public override void Deactivate()
    {
        StopToggle();
        base.Deactivate();
    }
}
