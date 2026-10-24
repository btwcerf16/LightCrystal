using UnityEngine;

public abstract class Effect : MonoBehaviour
{
    public float TimeRemaining { get; internal set; }
    public EffectData EffectData { get; private set; }

    public void Initialize(EffectData effectData)
    {
        EffectData = effectData;
        TimeRemaining = effectData.EffectDuration;
    }

    public virtual void EffectStart(ActorStats owner) { }
    public virtual void EffectTick(ActorStats owner, float deltaTime) { }
    public virtual void EffectEnd(ActorStats owner) { }
}
