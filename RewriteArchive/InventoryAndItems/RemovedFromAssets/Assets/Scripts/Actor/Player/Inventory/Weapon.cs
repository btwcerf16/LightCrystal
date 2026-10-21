using UnityEngine;

public abstract class Weapon : Item
{
    [SerializeField] protected float _damage;
    [SerializeField] protected Transform _attackPoint;
    [SerializeField] protected Animator _animator;
    public abstract void BeginPrimaryUse();
    public abstract void EndPrimaryUse();
    public abstract void BeginSecondaryUse();
    public abstract void EndSecondaryUse();
    public abstract void CancelUse();
    public virtual void SetAim(Vector2 aim) { }
}

