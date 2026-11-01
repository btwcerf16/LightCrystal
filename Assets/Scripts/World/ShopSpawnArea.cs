using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class ShopSpawnArea : MonoBehaviour
{
    [SerializeField] private Tilemap _ground;
    [SerializeField, Min(0)] private int _edgePadding = 8;

    public bool IsInsideMap(Vector3 position)
    {
        if (_ground == null)
            return false;
        Vector3Int cell = _ground.WorldToCell(position);
        BoundsInt bounds = _ground.cellBounds;
        return cell.x >= bounds.xMin + _edgePadding && cell.x < bounds.xMax - _edgePadding
            && cell.y >= bounds.yMin + _edgePadding && cell.y < bounds.yMax - _edgePadding
            && _ground.HasTile(cell);
    }

    public bool TryGetPosition(Vector2 crystalPosition, float minimumDistance, out Vector3 position)
    {
        position = Vector3.zero;
        if (_ground == null)
            return false;
        BoundsInt bounds = _ground.cellBounds;
        if (bounds.size.x <= _edgePadding * 2 || bounds.size.y <= _edgePadding * 2)
            return false;

        NetworkShop[] shops = FindObjectsByType<NetworkShop>(FindObjectsSortMode.None);
        for (int attempt = 0; attempt < 256; attempt++)
        {
            var cell = new Vector3Int(Random.Range(bounds.xMin + _edgePadding, bounds.xMax - _edgePadding),
                Random.Range(bounds.yMin + _edgePadding, bounds.yMax - _edgePadding), 0);
            Vector3 candidate = _ground.GetCellCenterWorld(cell);
            if (!IsInsideMap(candidate) || ((Vector2)candidate - crystalPosition).sqrMagnitude < minimumDistance * minimumDistance)
                continue;

            bool overlapsShop = false;
            foreach (NetworkShop shop in shops)
            {
                if (((Vector2)shop.transform.position - (Vector2)candidate).sqrMagnitude < 64f)
                {
                    overlapsShop = true;
                    break;
                }
            }
            if (overlapsShop)
                continue;
            position = candidate;
            return true;
        }
        return false;
    }
}
