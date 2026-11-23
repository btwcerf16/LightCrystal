using Unity.Netcode;
using UnityEngine;

public sealed class ActorHand : MonoBehaviour
{
    private static readonly Quaternion FlipAroundX = Quaternion.Euler(180f, 0f, 0f);

    [SerializeField] private Transform _handTransform;
    [SerializeField] private SpriteRenderer _visualHandSpriteRender;
    [SerializeField] private WorldItem _worldItemPrefab;
    [SerializeField, Min(0f)] private float _maxRadius = 1f;

    private EffectHandler _effects;
    private Weapon _weapon;
    private Quaternion _weaponLocalRotation;

    private void Awake()
    {
        _effects = GetComponentInParent<EffectHandler>();
    }

    public Vector2 CurrentOffset => _handTransform.localPosition;
    public Transform HandTransform => _handTransform;
    public Vector2 Position => _handTransform.position;
    public Vector2 ClampOffset(Vector2 offset)
    {
        if (float.IsNaN(offset.x) || float.IsNaN(offset.y) || float.IsInfinity(offset.x) || float.IsInfinity(offset.y))
            return Vector2.zero;
        return Vector2.ClampMagnitude(offset, _maxRadius);
    }

    public Vector2 AimDirection => _handTransform.right;
    public SpriteRenderer VisualRenderer => _visualHandSpriteRender;

    public Vector2 Offset { get; private set; }

    private void LateUpdate()
    {
        bool shouldFlip = _handTransform.localPosition.x < 0f;

        if (_visualHandSpriteRender.flipX != shouldFlip)
            _visualHandSpriteRender.flipX = shouldFlip;

        if (_weapon != null)
            RotateWeapon(_weapon, _weaponLocalRotation);
    }

    internal void SetWeapon(Weapon weapon)
    {
        if (_weapon == weapon)
            return;

        if (_weapon != null)
            ClearWeapon(_weapon);

        _weapon = weapon;
        _weaponLocalRotation = weapon.transform.localRotation;
        RotateWeapon(weapon, _weaponLocalRotation);
    }

    internal void ClearWeapon(Weapon weapon)
    {
        if (_weapon != weapon)
            return;

        weapon.transform.localRotation = _weaponLocalRotation;
        _weapon = null;
    }

    internal static void RotateWeapon(Weapon weapon, Quaternion localRotation)
    {
        Transform weaponTransform = weapon.transform;
        if (weapon.RotationOnlyByZ)
            RotateOnlyByZ(weaponTransform, localRotation);
        else
            RotateByZAndX(weaponTransform, localRotation);
    }

    // The mount already provides the Z rotation.
    private static void RotateOnlyByZ(Transform weapon, Quaternion localRotation)
    {
        if (weapon.localRotation != localRotation)
            weapon.localRotation = localRotation;
    }

    private static void RotateByZAndX(Transform weapon, Quaternion localRotation)
    {
        if (weapon.parent.right.x < -0.0001f)
            localRotation = FlipAroundX * localRotation;

        RotateOnlyByZ(weapon, localRotation);
    }

    public bool DropItem(Item item, Vector3 position)
    {
        NetworkObject player = GetComponentInParent<NetworkObject>();
        if (player == null || !player.IsSpawned || !player.NetworkManager.IsServer || item == null)
            return false;

        WorldItem worldItem = item.WorldItem != null ? item.WorldItem.GetComponent<WorldItem>() : null;
        if (worldItem == null)
        {
            if (_worldItemPrefab == null)
                return false;

            worldItem = Instantiate(_worldItemPrefab, position, Quaternion.identity);
            worldItem.SetItem(item.gameObject);
            worldItem.NetworkObject.Spawn();
        }
        else
        {
            worldItem.SetItem(item.gameObject);
            worldItem.ShowAt(position);
        }

        item.SetWorldItem(worldItem.gameObject);
        return true;
    }

    public void SetOffset(Vector2 offset)
    {
        if (_effects != null && _effects.IsStunned)
            return;
        Offset = ClampOffset(offset);
        _handTransform.localPosition = Offset;

        if (Offset.sqrMagnitude < 0.000001f)
            return;

        float angle = Mathf.Atan2(Offset.y, Offset.x) * Mathf.Rad2Deg;
        _handTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        if (_weapon != null)
            RotateWeapon(_weapon, _weaponLocalRotation);
    }

}
