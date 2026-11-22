using Unity.Netcode;
using UnityEngine;

internal interface IWeaponVisualReceiver
{
    void PlayVisual(GameObject owner, Vector2[] points, bool charged, float radius);
}

public sealed class NetworkWeaponVisuals : NetworkBehaviour
{
    private ItemFactory _itemFactory;

    public override void OnNetworkSpawn() => _itemFactory = NetworkManager.GetComponent<ItemFactory>();

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    internal void PlayRpc(int itemId, Vector2[] points, bool charged = false, float radius = 0f)
    {
        if (points.Length >= 2 && _itemFactory.GetPrefab(itemId) is IWeaponVisualReceiver receiver)
            receiver.PlayVisual(gameObject, points, charged, radius);
    }
}
