using System;
using UnityEngine;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(ActorStats))]
public sealed class ActorSanity : MonoBehaviour, ISanity
{
    private ActorStats _stats;

    public event Action<float, float> SanityChanged;
    public event Action SanityDepleted;

    public float CurrentSanity { get; private set; }
    public float MaxSanity => _stats.MaxSanity.Value;
    public bool IsSanityDepleted => CurrentSanity <= 0f;

    private void Awake()
    {
        _stats = GetComponent<ActorStats>();
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