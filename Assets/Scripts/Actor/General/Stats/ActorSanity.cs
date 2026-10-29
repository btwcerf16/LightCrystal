using System;
using UnityEngine;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(ActorStats))]
public sealed class ActorSanity : MonoBehaviour, ISanity, ILightSensitive
{
    [SerializeField, Min(0f)] private float _sanityDrainRate = 1f;
    private ActorStats _stats;
    private ActorHealth _health;
    private int _lightSources;

    public event Action<float, float> SanityChanged;
    public event Action SanityDepleted;

    public float CurrentSanity { get; private set; }
    public float MaxSanity => _stats.MaxSanity.Value;
    public bool IsSanityDepleted => CurrentSanity <= 0f;
    public bool IsInLight => _lightSources > 0;

    private void Awake()
    {
        _stats = GetComponent<ActorStats>();
        _health = GetComponent<ActorHealth>();
        CurrentSanity = MaxSanity;
    }

    private void OnEnable()
    {
        _stats.MaxSanity.ValueChanged += OnMaxSanityChanged;
    }

    private void OnDisable()
    {
        _stats.MaxSanity.ValueChanged -= OnMaxSanityChanged;
    }

    public void EnterLight()
    {
        _lightSources++;
    }

    public void ExitLight()
    {
        _lightSources = Mathf.Max(0, _lightSources - 1);
    }

    public void ProcessDarkness(float deltaTime)
    {
        if (IsInLight || !isActiveAndEnabled || (_health != null && _health.IsDead))
            return;
        DrainSanity(_sanityDrainRate * deltaTime);
    }

    public float DrainSanity(float amount)
    {
        if (amount <= 0f || IsSanityDepleted)
            return 0f;

        float resistance = _stats.MadnessResistance.Value;
        float resultingAmount = amount * (1f - resistance);

        float previousSanity = CurrentSanity;
        CurrentSanity = Mathf.Max(CurrentSanity - resultingAmount, 0f);
        float drainedSanity = previousSanity - CurrentSanity;

        SanityChanged?.Invoke(CurrentSanity, MaxSanity);

        if (CurrentSanity <= 0f)
            SanityDepleted?.Invoke();

        return drainedSanity;
    }

    public bool TrySpendSanity(float amount)
    {
        if (amount < 0f || CurrentSanity < amount)
            return false;
        if (amount == 0f)
            return true;

        CurrentSanity -= amount;
        SanityChanged?.Invoke(CurrentSanity, MaxSanity);
        if (CurrentSanity <= 0f)
            SanityDepleted?.Invoke();
        return true;
    }

    public void ReceiveLight(float amount)
    {
        if (_health != null && _health.IsDead)
            return;
        RestoreSanity(amount);
    }

    public float RestoreSanity(float amount)
    {
        if (amount <= 0f)
            return 0f;

        float previousSanity = CurrentSanity;
        CurrentSanity = Mathf.Min(CurrentSanity + amount, MaxSanity);
        float restoredSanity = CurrentSanity - previousSanity;

        if (restoredSanity > 0f)
            SanityChanged?.Invoke(CurrentSanity, MaxSanity);

        return restoredSanity;
    }

    public void EmptySanity()
    {
        if (IsSanityDepleted)
            return;

        CurrentSanity = 0f;
        SanityChanged?.Invoke(CurrentSanity, MaxSanity);
        SanityDepleted?.Invoke();
    }

    private void OnMaxSanityChanged(float maxSanity)
    {
        CurrentSanity = Mathf.Min(CurrentSanity, maxSanity);
        SanityChanged?.Invoke(CurrentSanity, maxSanity);
    }
}
