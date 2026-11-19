using Unity.Netcode;
using UnityEngine;

public sealed class SilenceMarkEffect : Effect
{
    private EffectData _silence;
    private float _duration;
    private SilenceMarkVisual _visual;

    public void SetSilence(EffectData silence, float duration, SilenceMarkVisual visualPrefab)
    {
        _silence = silence;
        _duration = duration;
        if (_visual == null && visualPrefab != null && TryGetComponent(out NetworkObject owner) && owner.IsSpawned && owner.NetworkManager.IsServer)
        {
            _visual = Instantiate(visualPrefab);
            _visual.NetworkObject.Spawn(true);
            _visual.NetworkObject.TrySetParent(owner, false);
        }
        if (_visual != null)
            _visual.Restart(TimeRemaining);
    }

    public override void EffectEnd(ActorStats owner)
    {
        if (_visual != null && _visual.IsSpawned && !_visual.NetworkManager.ShutdownInProgress)
            _visual.NetworkObject.Despawn();
        if (TimeRemaining <= 0f && !owner.GetComponent<ActorHealth>().IsDead)
            owner.GetComponent<EffectHandler>().AddEffect(_silence, _duration);
    }
}
