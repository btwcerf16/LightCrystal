using Unity.VisualScripting;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
    [SerializeField] private Sprite _icon;
    public GameObject WorldItem {  get; private set; }
    public bool IsEquipped { get; private set; }
    public virtual string DisplayName => name;
    public virtual Sprite Icon => _icon;
    public int ItemId { get; private set; }
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
        ItemId = id;
    }
}
