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
    private float _remainingLifetime;
    private float _triggerInterval;
    private int _remainingOperationsCount;
    private bool _isInitialized;
    protected TrapData TrapParameters { get; private set; }

    public static event Action<Trap, ActorStats> Caught;
    public event Action Finished;

    public virtual void Initialize(TrapData trapData)
    {
        TrapParameters = trapData;
        _owner = trapData.Owner;
        _effectOnCaught = trapData.EffectOnCaught;
        _targetsLayerMask = trapData.TargetsLayerMask;
        _lifeTime = Mathf.Max(0f, trapData.LifeTime);
        _operationsCount = Mathf.Max(0, trapData.OperationsCount);
        _remainingLifetime = _lifeTime;
        _remainingOperationsCount = _operationsCount;
        _triggerInterval = Mathf.Max(0f, trapData.TriggerInterval);
        _nextTriggerTimes.Clear();
        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized)
            return;
        _remainingLifetime -= Time.deltaTime;
        if (_remainingLifetime <= 0f)
            Finish();
    }

    private void OnTriggerEnter2D(Collider2D targetCollider) => TryCatch(targetCollider);

    private void OnTriggerStay2D(Collider2D targetCollider)
    {
        if (_triggerInterval > 0f)
            TryCatch(targetCollider);
    }

    private void TryCatch(Collider2D targetCollider)
    {
        if (!isActiveAndEnabled || !_isInitialized || _remainingLifetime <= 0f)
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
        _nextTriggerTimes[caughtActor] = Time.time + _triggerInterval;
        OnCaught(targetCollider);
        // Zero operations means the trap is limited only by its lifetime.
        if (_remainingOperationsCount > 0 && --_remainingOperationsCount == 0)
            Finish();
    }

    protected virtual void OnCaught(Collider2D targetCollider)
    {
        ActorStats caughtActor = targetCollider.GetComponentInParent<ActorStats>();
        if (!caughtActor.GetComponent<ActorHealth>().IsDead && _effectOnCaught != null)
            caughtActor.GetComponent<EffectHandler>()?.AddEffect(_effectOnCaught);
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
