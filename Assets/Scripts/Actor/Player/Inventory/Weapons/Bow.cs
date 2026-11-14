using UnityEngine;

public abstract class Bow : Weapon
{
    [SerializeField, Min(0.01f)] private float _drawDuration = 1f;
    [SerializeField] private Transform _bowVisual;
    [SerializeField] private Transform _upperString;
    [SerializeField] private Transform _lowerString;
    [SerializeField] private SpriteRenderer _nockedArrow;
    [SerializeField] private Vector2 _stringEnd = new Vector2(1.5f, 13f) / 32f;
    [SerializeField, Min(0f)] private float _stringPullDistance = 9f / 32f;

    private float _drawProgress;
    private NetworkPlayerArsenal _networkArsenal;

    protected virtual bool FiresOnRelease => true;
    public override float ChargeProgress => _isAttacking || _drawProgress >= 1f ? _drawProgress : -1f;

    public override void Equip()
    {
        base.Equip();
        _networkArsenal = OwnerStats.GetComponent<NetworkPlayerArsenal>();
        if (_hand != null)
            _hand.VisualRenderer.enabled = false;
    }

    public override bool BeginPrimaryUse()
    {
        if (!IsServer || !IsEquipped || !IsAvailable || IsOwnerStunned || _isAttacking)
            return false;

        if (!FiresOnRelease && _drawProgress >= 1f)
        {
            Fire();
            CancelUse();
            return true;
        }

        _isAttacking = true;
        return true;
    }

    private void Update()
    {
        if (!_isAttacking)
            return;

        float drawSpeed = Mathf.Max(0f, OwnerStats.AttackSpeed.Value);
        _drawProgress = Mathf.MoveTowards(_drawProgress, 1f, Time.deltaTime * drawSpeed / _drawDuration);
    }

    public override void EndPrimaryUse()
    {
        if (!_isAttacking)
            return;

        if (FiresOnRelease)
        {
            Fire();
            CancelUse();
            return;
        }

        _isAttacking = false;
        if (_drawProgress < 1f)
            _drawProgress = 0f;
    }

    private void Fire()
    {
        if (!IsServer || !IsEquipped || !IsAvailable || IsOwnerStunned || _drawProgress < 1f)
            return;

        ShootArrow();
    }

    protected abstract void ShootArrow();

    public override void CancelUse()
    {
        _isAttacking = false;
        _drawProgress = 0f;
    }

    public override void Drop()
    {
        _isAttacking = false;
        if (FiresOnRelease || _drawProgress < 1f)
            CancelUse();
        base.Drop();
        UpdateDrawVisual(-1f);
        if (_hand != null)
            _hand.VisualRenderer.enabled = true;
    }

    private void LateUpdate()
    {
        if (IsEquipped && _networkArsenal != null)
            UpdateDrawVisual(_networkArsenal.WeaponCharge);
    }

    protected virtual void UpdateDrawVisual(float charge)
    {
        Vector2 stringCenter = new Vector2(_stringEnd.x - Mathf.Clamp01(charge) * _stringPullDistance, 0f);
        StretchString(_upperString, _stringEnd, stringCenter);
        StretchString(_lowerString, new Vector2(_stringEnd.x, -_stringEnd.y), stringCenter);

        _nockedArrow.enabled = charge >= 0f;
        float arrowHalfLength = _nockedArrow.sprite.bounds.extents.x * _nockedArrow.transform.localScale.x;
        _nockedArrow.transform.localPosition = stringCenter + Vector2.right * arrowHalfLength;
        float shakeAngle = charge >= 0f && charge < 1f ? Mathf.Sin(Time.time * 75f) * 2f : 0f;
        _bowVisual.localRotation = Quaternion.Euler(0f, 0f, shakeAngle);
    }

    private static void StretchString(Transform segment, Vector2 end, Vector2 center)
    {
        Vector2 direction = end - center;
        segment.localPosition = (end + center) * 0.5f;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        segment.localRotation = Quaternion.Euler(0f, 0f, angle);
        segment.localScale = new Vector3(1f, direction.magnitude * 32f, 1f);
    }

    public override void Use() { }
    public override void EndSecondaryUse() { }
}
