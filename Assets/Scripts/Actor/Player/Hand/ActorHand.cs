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

    public WorldItem DropItem(WorldItem worldItem, GameObject itemPrefab)
    {
        NetworkObject player = GetComponentInParent<NetworkObject>();
        if (player == null || !player.IsSpawned || !player.NetworkManager.IsServer || itemPrefab == null)
            return null;

        if (worldItem == null)
        {
            if (_worldItemPrefab == null)
                return null;

            worldItem = Instantiate(_worldItemPrefab, _handTransform.position, Quaternion.identity);
            worldItem.SetItem(itemPrefab);
            worldItem.NetworkObject.Spawn();
            return worldItem;
        } 

        worldItem.SetItem(itemPrefab);
        worldItem.ShowAt(_handTransform.position);
        return worldItem;
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
