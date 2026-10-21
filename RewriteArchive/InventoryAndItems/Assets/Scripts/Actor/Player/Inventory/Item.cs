using UnityEngine;

public abstract class Item : MonoBehaviour
{
    [SerializeField] private Sprite _icon;
    public virtual string DisplayName => name;
    public virtual Sprite Icon => _icon;
    public bool IsEquipped { get; private set; }
    public GameObject Owner { get; private set; }

    public abstract void Use();

    protected virtual void OnValidate()
    {
        if (!Application.isPlaying && TryGetComponent(out SpriteRenderer sprite))
            sprite.sprite = _icon;
    }

    public virtual void SetOwner(GameObject owner)
    {
        Owner = owner;
    }

    public virtual void Equip() => IsEquipped = true;
    public virtual void Drop() => IsEquipped = false;
}
