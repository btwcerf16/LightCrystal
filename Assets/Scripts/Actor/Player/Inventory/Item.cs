using Unity.VisualScripting;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
    public bool IsEquipped { get; private set; }

    public abstract void Use();
    public virtual void Equip()
    {
        IsEquipped = true;
    }

    public virtual void Drop()
    {
        IsEquipped = false;
    }
}
