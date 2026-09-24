using System;

public interface ISanity
{
    event Action<float, float> SanityChanged;
    event Action SanityDepleted;

    float CurrentSanity { get; }
    float MaxSanity { get; }
    bool IsSanityDepleted { get; }

    float DrainSanity(float amount);
    float RestoreSanity(float amount);
    void EmptySanity();
}