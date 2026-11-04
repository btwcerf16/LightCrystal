using UnityEngine;

public sealed class RoyalScissorsItem : Item
{
    [SerializeField] private float _executionThreshold = 0.03f;
    [SerializeField] private float _thresholdPerExecution = 0.005f;
    [SerializeField] private float _maximumThreshold = 0.08f;
    [SerializeField] private float _range = 2f;
    [SerializeField] private float _activeCooldown = 20f;
    [SerializeField] private RitualBuffData _hauntingSpirit;
    private ActorHealth _ownerHealth;
    private ActorHealth _target;
    private int _executions;
    private bool _isExecuting;
    public override string DisplayName => "Ножницы королевского портного";
    public override float BaseCooldown => _activeCooldown;
    public float ExecutionThreshold => Mathf.Min(_maximumThreshold, _executionThreshold + _executions * _thresholdPerExecution);

    public override void Equip()
    {
        if (IsEquipped) return;
        base.Equip();
        if (!IsServer) return;
        _ownerHealth = _ownerStats.GetComponent<ActorHealth>();
        _ownerHealth.EnemyHit += OnHit;
    }

    private void OnHit(ActorHealth enemy)
    {
        if (_isExecuting || enemy.IsDead || !enemy.GetComponent<Enemy>().CanBeExecuted) return;
        if (enemy.CurrentHealth / enemy.MaxHealth <= ExecutionThreshold)
            Execute(enemy);
    }

    private bool Execute(ActorHealth enemy)
    {
        _isExecuting = true;
        enemy.TakeDamage(enemy.CurrentHealth, EDamageType.Pure, _ownerStats);
        _isExecuting = false;
        return enemy.IsDead;
    }

    protected override bool CanUse()
    {
        _target = null;
        if (_hauntingSpirit == null) return false;
        float distance = _range * _range;
        Vector2 forward = _ownerStats.GetComponentInChildren<ActorHand>().AimDirection;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(_ownerStats.transform.position, _range))
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.CanBeExecuted) continue;
            ActorHealth health = enemy.GetComponent<ActorHealth>();
            Vector2 offset = enemy.transform.position - _ownerStats.transform.position;
            if (!health.IsDead && offset.sqrMagnitude <= distance && Vector2.Dot(forward, offset) >= 0f)
            {
                _target = health;
                distance = offset.sqrMagnitude;
            }
        }
        return _target != null;
    }

    public override void Use()
    {
        if (!IsServer || _target == null || _target.IsDead) return;
        if (Execute(_target))
        {
            _executions++;
            _ownerStats.GetComponent<EffectHandler>().AddEffect(_hauntingSpirit);
        }
    }

    public override void Drop()
    {
        if (_ownerHealth != null) _ownerHealth.EnemyHit -= OnHit;
        base.Drop();
    }
}
