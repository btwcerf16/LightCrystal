using Unity.Netcode;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
    [SerializeField] private Sprite _icon;
    [SerializeField, Min(0)] private int _itemId = 0;
    [SerializeField] private EItemRarity _rarity = EItemRarity.Common;
    [SerializeField, Min(0)] private int _price;
    [SerializeField, Min(0f)] private float _cooldown;
    [SerializeField, TextArea] private string _description;
    [SerializeField, TextArea] private string _statsDescription;

    private NetworkObject _ownerNetworkObject;
    private ActorStats _ownerStats;
    public GameObject WorldItem {  get; private set; }
    public bool IsEquipped { get; private set; }
    public virtual string DisplayName => name;
    public virtual Sprite Icon => _icon;
    public int ItemId => _itemId;
    public EItemRarity Rarity => _rarity;
    public int Price => _price;
    public ulong Uid { get; private set; }
    public float BaseCooldown => _cooldown;
    public string Description => TooltipText.Format(this, _description);
    public string StatsDescription => TooltipText.Format(this, _statsDescription);
    public float CooldownDuration { get; private set; }
    public double CooldownEndsAt { get; private set; }
    public float CooldownRemaining
    {
        get
        {
            if (_ownerNetworkObject == null || !_ownerNetworkObject.IsSpawned)
                return 0f;
            return Mathf.Max(0f, (float)(CooldownEndsAt - _ownerNetworkObject.NetworkManager.ServerTime.Time));
        }
    }

    public bool TryUse()
    {
        if (!IsEquipped || _ownerNetworkObject == null || !_ownerNetworkObject.IsSpawned || !_ownerNetworkObject.NetworkManager.IsServer || CooldownRemaining > 0f)
            return false;

        float cdr = _ownerStats != null ? _ownerStats.CooldownReduction.Value : 0f;
        CooldownDuration = _cooldown * (1f - Mathf.Clamp(cdr, 0f, 0.9f));
        CooldownEndsAt = _ownerNetworkObject.NetworkManager.ServerTime.Time + CooldownDuration;
        Use();
        return true;
    }

    public void SetCooldown(float duration, double endsAt)
    {
        CooldownDuration = duration;
        CooldownEndsAt = endsAt;
    }
    public abstract void Use();
    public virtual void Equip()
    {
        _ownerNetworkObject = GetComponentInParent<NetworkObject>();
        _ownerStats = GetComponentInParent<ActorStats>();
        IsEquipped = true;
    }

    public virtual void Drop()
    {
        IsEquipped = false;
    }
    public void SetWorldItem(GameObject worldItem)
    {
        WorldItem = worldItem;
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
