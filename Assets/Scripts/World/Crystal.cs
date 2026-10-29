using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public sealed class Crystal : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float _radius = 6f;
    [SerializeField, Min(0f)] private float _power = 5f;
    [SerializeField, Min(0.05f)] private float _tickInterval = 0.2f;
    [SerializeField] private LayerMask _targetMask = ~0;

    private float _elapsed;
    private readonly List<Collider2D> _colliders = new();
    private readonly HashSet<ILightSensitive> _targets = new();
    private readonly HashSet<ILightSensitive> _previousTargets = new();

    private void Awake()
    {
        UpdateLight();
    }

    private void OnValidate()
    {
        UpdateLight();
    }

    private void OnDisable()
    {
        _elapsed = 0f;
        ClearTargets();
    }

    public void SetDestroyed(bool destroyed)
    {
        GetComponent<Light2D>().enabled = !destroyed;
        enabled = !destroyed;
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            _elapsed = 0f;
            ClearTargets();
            return;
        }

        _elapsed += Time.deltaTime;
        if (_elapsed < _tickInterval)
            return;

        float amount = _power * _elapsed;
        _elapsed = 0f;
        Illuminate(amount);
    }

    private void Illuminate(float amount)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(_targetMask);
        filter.useTriggers = true;
        Physics2D.OverlapCircle(transform.position, _radius, filter, _colliders);
        _targets.Clear();

        foreach (Collider2D collider in _colliders)
        {
            ILightSensitive target = collider.GetComponentInParent<ILightSensitive>();
            if (target == null || !_targets.Add(target))
                continue;
            if (!_previousTargets.Contains(target))
                target.EnterLight();
            target.ReceiveLight(amount);
        }

        foreach (ILightSensitive target in _previousTargets)
        {
            if (!_targets.Contains(target) && target is Component component && component != null)
                target.ExitLight();
        }
        _previousTargets.Clear();
        _previousTargets.UnionWith(_targets);
    }

    private void ClearTargets()
    {
        foreach (ILightSensitive target in _previousTargets)
        {
            if (target is Component component && component != null)
                target.ExitLight();
        }
        _previousTargets.Clear();
        _targets.Clear();
    }

    private void UpdateLight()
    {
        Light2D light = GetComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.pointLightOuterRadius = _radius;
        light.pointLightInnerRadius = _radius * 0.75f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
