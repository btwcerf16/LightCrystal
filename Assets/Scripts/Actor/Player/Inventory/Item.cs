using Unity.VisualScripting;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
    [SerializeField] private Sprite _icon;
    [SerializeField, Min(0)] private int _itemId = 0;
    public GameObject WorldItem {  get; private set; }
    public bool IsEquipped { get; private set; }
    public virtual string DisplayName => name;
    public virtual Sprite Icon => _icon;
    public int ItemId => _itemId;
    public ulong Uid { get; private set; }
    public abstract void Use();
    public virtual void Equip()
    {
        IsEquipped = true;
    }

    public virtual void Drop()
    {
        IsEquipped = false;
    }
    public void SetWorldItem(GameObject worldItem)
    {
        if (WorldItem != null) 
        {
            WorldItem = worldItem;
        }
    }
    protected void SetItemId(int id)
    {
        _itemId = id;
    }
    public void SetUid(ulong uid)
    {
        Uid = uid;
    }
}
