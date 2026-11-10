using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(ActorSanity), typeof(EffectHandler))]
public sealed class ActorMadness : NetworkBehaviour
{
    [SerializeField] private MadnessEffectData _madnessEffect;
    [SerializeField] private bool _canReceiveMadness = true;
    private ActorSanity _sanity;
    private EffectHandler _effects;
    private Effect _activeMadness;
    public bool CanReceiveMadness => _canReceiveMadness;

    private void Awake()
    {
        _sanity = GetComponent<ActorSanity>();
        _effects = GetComponent<EffectHandler>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;
        _sanity.SanityChanged += UpdateMadness;
        UpdateMadness(_sanity.CurrentSanity, _sanity.MaxSanity);
    }

    public void SetCanReceiveMadness(bool canReceiveMadness)
    {
        if (!IsServer)
            return;
        _canReceiveMadness = canReceiveMadness;
        UpdateMadness(_sanity.CurrentSanity, _sanity.MaxSanity);
    }

    private void UpdateMadness(float currentSanity, float maxSanity)
    {
        if (!_canReceiveMadness || maxSanity <= 0f || currentSanity >= maxSanity * 0.5f)
        {
            if (_activeMadness != null)
                _effects.RemoveEffect(_activeMadness);
            _activeMadness = null;
        }
        else if (_activeMadness == null && currentSanity <= maxSanity * 0.4f)
            _activeMadness = _effects.AddEffect(_madnessEffect);
    }

    public override void OnNetworkDespawn()
    {
        _sanity.SanityChanged -= UpdateMadness;
        if (IsServer && _activeMadness != null)
            _effects.RemoveEffect(_activeMadness);
        _activeMadness = null;
    }
}
