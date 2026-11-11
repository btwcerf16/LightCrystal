using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public abstract class Trap : MonoBehaviour
{
    [SerializeField] protected LayerMask _targetsLayerMask;
    [SerializeField, Min(0)] protected int _operationsCount = 1;
    [SerializeField, Min(0f)] private float _lifeTime = 10f;
    [SerializeField] protected ActorStats _owner;
    [SerializeField] protected EffectData _effectOnCaught;
    private readonly Dictionary<ActorStats, float> _nextTriggerTimes = new();
    private CircleCollider2D _circle;
    private float _prefabRadius;
    private float _remainingLifetime;
    private float _remainingTriggerDelay;
    private float _triggerInterval;
    private int _remainingOperationsCount;
    private bool _isInitialized;
    protected TrapData TrapParameters { get; private set; }

    public static event Action<Trap, ActorStats> Caught;
    public event Action Finished;

    public float Radius => _circle.radius;
    public float TriggerDelay => TrapParameters.TriggerDelay;

    private void Awake()
    {
        _circle = GetComponent<CircleCollider2D>();
        _prefabRadius = _circle.radius;
    }

    public virtual void Initialize(TrapData trapData)
    {
        TrapParameters = trapData;
        _owner = trapData.Owner;
        _effectOnCaught = trapData.EffectOnCaught;
        _targetsLayerMask = trapData.TargetsLayerMask;
        _lifeTime = Mathf.Max(0f, trapData.LifeTime);
        _operationsCount = Mathf.Max(0, trapData.OperationsCount);
        _remainingLifetime = _lifeTime;
        _remainingTriggerDelay = Mathf.Max(0f, trapData.TriggerDelay);
        _circle.radius = trapData.Radius > 0f ? trapData.Radius : _prefabRadius;
        _remainingOperationsCount = _operationsCount;
        _triggerInterval = Mathf.Max(0f, trapData.TriggerInterval);
        _nextTriggerTimes.Clear();
        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized)
            return;
        if (TriggerDelay > 0f)
        {
            _remainingTriggerDelay -= Time.deltaTime;
            if (_remainingTriggerDelay <= 0f)
            {
                Vector2 center = transform.TransformPoint(_circle.offset);
                foreach (Collider2D target in Physics2D.OverlapCircleAll(center, Radius, _targetsLayerMask))
                    TryCatch(target);
                Finish();
            }
            return;
        }

        _remainingLifetime -= Time.deltaTime;
        if (_remainingLifetime <= 0f)
            Finish();
    }

    private void OnTriggerEnter2D(Collider2D targetCollider)
    {
        if (TriggerDelay <= 0f)
            TryCatch(targetCollider);
    }

    private void OnTriggerStay2D(Collider2D targetCollider)
    {
        if (TriggerDelay <= 0f && _triggerInterval > 0f)
            TryCatch(targetCollider);
    }

    private void TryCatch(Collider2D targetCollider)
    {
        if (!isActiveAndEnabled || !_isInitialized || (TriggerDelay <= 0f && _remainingLifetime <= 0f))
            return;
        if ((_targetsLayerMask.value & (1 << targetCollider.gameObject.layer)) == 0)
            return;
        ActorStats caughtActor = targetCollider.GetComponentInParent<ActorStats>();
        if (caughtActor == null || caughtActor == _owner)
            return;
        ActorHealth caughtHealth = caughtActor.GetComponent<ActorHealth>();
        if (caughtHealth == null || caughtHealth.IsDead)
            return;
        if (_nextTriggerTimes.TryGetValue(caughtActor, out float nextTriggerTime) && Time.time < nextTriggerTime)
            return;
        _nextTriggerTimes[caughtActor] = TriggerDelay > 0f ? float.PositiveInfinity : Time.time + _triggerInterval;
        OnCaught(targetCollider);
        // Zero operations means the trap is limited only by its lifetime.
        if (_remainingOperationsCount > 0 && --_remainingOperationsCount == 0)
            Finish();
    }

    protected virtual void OnCaught(Collider2D targetCollider)
    {
        ActorStats caughtActor = targetCollider.GetComponentInParent<ActorStats>();
        if (!caughtActor.GetComponent<ActorHealth>().IsDead && _effectOnCaught != null)
        {
            Effect effect = caughtActor.GetComponent<EffectHandler>()?.AddEffect(_effectOnCaught);
            if (effect is BurnEffect burn)
                burn.Configure(_owner);
        }
        Caught?.Invoke(this, caughtActor);
    }

    public ActorStats GetOwner() => _owner;

    private void Finish()
    {
        if (!_isInitialized)
            return;
        _isInitialized = false;
        Finished?.Invoke();
    }

    private void OnDisable()
    {
        _isInitialized = false;
        _nextTriggerTimes.Clear();
        TrapParameters = default;
        _owner = null;
        _effectOnCaught = null;
    }
}
