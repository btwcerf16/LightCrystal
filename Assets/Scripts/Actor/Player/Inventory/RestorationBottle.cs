using UnityEngine;

public sealed class RestorationBottle : Item
{
    [SerializeField] private EffectData _restorationEffect;
    private EffectHandler _effectHandler;

    public override string DisplayName => "Бутылка восстановления";
    public EffectData RestorationEffect => _restorationEffect;

    public override void Equip()
    {
        base.Equip();
        _effectHandler = GetComponentInParent<EffectHandler>();
    }

    public override void Use()
    {
        if (_effectHandler != null && _restorationEffect != null)
            _effectHandler.AddEffect(_restorationEffect);
    }
}
