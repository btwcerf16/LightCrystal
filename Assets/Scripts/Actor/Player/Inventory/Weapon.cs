using UnityEngine;

public abstract class Weapon : Item
{
    [SerializeField] protected float _damage;
    [SerializeField] protected Transform _attackPoint;
    [SerializeField] protected Animator _animator;
    private Stat _attackSpeed;

    public float Damage => _damage;

    public override void Equip()
    {
        if (_attackSpeed != null)
            _attackSpeed.ValueChanged -= UpdateAttackSpeed;

        base.Equip();
        _attackSpeed = GetComponentInParent<ActorStats>()?.AttackSpeed;
        if (_attackSpeed != null)
            _attackSpeed.ValueChanged += UpdateAttackSpeed;
        UpdateAttackSpeed(_attackSpeed != null ? _attackSpeed.Value : 1f);
    }

    public override void Drop()
    {
        if (_attackSpeed != null)
            _attackSpeed.ValueChanged -= UpdateAttackSpeed;
        _attackSpeed = null;
        UpdateAttackSpeed(1f);
        base.Drop();
    }

    private void UpdateAttackSpeed(float value)
    {
        if (_animator != null)
            _animator.SetFloat("AttackSpeed", Mathf.Max(0f, value));
    }

    protected virtual void OnDestroy()
    {
        if (_attackSpeed != null)
            _attackSpeed.ValueChanged -= UpdateAttackSpeed;
    }

    public abstract void BeginPrimaryUse();
    public abstract void EndPrimaryUse();
    public abstract void BeginSecondaryUse();
    public abstract void EndSecondaryUse();
    public abstract void CancelUse();
    public virtual void SetAim(Vector2 aim) { }
}

