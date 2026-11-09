using UnityEngine;

public sealed class MagicMissleProjectile : Projectile
{
    [SerializeField, Min(0.1f)] private float _searchRadius = 4f;
    [SerializeField, Min(1f)] private float _turnSpeed = 360f;
    private ActorHealth _target;
    private float _searchDelay;
    public Weapon Source { get; private set; }

    public void SetSource(Weapon source) => Source = source;

    public override void Launch(ProjectileData data)
    {
        _target = null;
        _searchDelay = 0f;
        base.Launch(data);
        if (Source != null)
            SetPiercing(ExtraPiercing + Source.TargetLimit - 1);
    }

    private void Update()
    {
        if (!IsFlying)
            return;
        if (_target == null || _target.IsDead)
        {
            _searchDelay -= Time.deltaTime;
            if (_searchDelay > 0f)
                return;
            _searchDelay = 0.2f;
            float nearest = _searchRadius * _searchRadius;
            _target = null;
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, _searchRadius, Data.LayerMask))
            {
                Enemy enemy = hit.GetComponentInParent<Enemy>();
                if (enemy == null)
                    continue;
                ActorHealth health = enemy.GetComponent<ActorHealth>();
                float distance = ((Vector2)(enemy.transform.position - transform.position)).sqrMagnitude;
                if (!health.IsDead && distance < nearest)
                {
                    _target = health;
                    nearest = distance;
                }
            }
        }
        if (_target == null)
            return;
        Vector2 direction = _target.transform.position - transform.position;
        if (direction.sqrMagnitude < 0.0001f)
            return;
        float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.z,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, _turnSpeed * Time.deltaTime);
        SetDirection(Quaternion.Euler(0f, 0f, angle) * Vector2.right);
    }

    protected override bool CanHit(Collider2D collider)
    {
        return base.CanHit(collider) && ((collider.GetComponentInParent<Enemy>() != null || collider.GetComponentInParent<EnchantRune>() != null) || (!collider.isTrigger && collider.GetComponentInParent<IDamageable>() == null));
    }

    protected override void OnDisable()
    {
        _target = null;
        Source = null;
        _searchDelay = 0f;
        base.OnDisable();
    }
}
