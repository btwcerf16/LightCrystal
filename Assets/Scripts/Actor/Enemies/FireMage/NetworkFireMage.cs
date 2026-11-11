using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(NetworkAnimator))]
public sealed class NetworkFireMage : NetworkBehaviour
{
    private readonly NetworkVariable<bool> _facesLeft = new();
    private FireMage _mage;
    private Rigidbody2D _body;
    private NetworkAnimator _animator;
    private SpellHolder _spells;

    private void Awake()
    {
        _mage = GetComponent<FireMage>();
        _body = GetComponent<Rigidbody2D>();
        _animator = GetComponent<NetworkAnimator>();
        _spells = GetComponent<SpellHolder>();
    }

    public override void OnNetworkSpawn()
    {
        _body.simulated = IsServer;
        _mage.enabled = IsServer;
        _spells.Initialize(IsServer);
        if (IsServer)
        {
            _facesLeft.Value = false;
            _mage.SetFacing(false);
            _mage.FacingChanged += PublishFacing;
            _mage.AnimationTriggered += PlayAnimation;
            _spells.SpellCast += PlayCast;
            if (_mage.MainTarget == null)
                _mage.SetMainTarget(FindFirstObjectByType<Crystal>()?.transform);
            _mage.Initialize(_mage.MainTarget);
            return;
        }

        _facesLeft.OnValueChanged += ReceiveFacing;
        _mage.SetFacing(_facesLeft.Value);
    }

    public override void OnNetworkDespawn()
    {
        _facesLeft.OnValueChanged -= ReceiveFacing;
        _mage.FacingChanged -= PublishFacing;
        _mage.AnimationTriggered -= PlayAnimation;
        _spells.SpellCast -= PlayCast;
        _spells.ClearSpells();
        _mage.enabled = false;
        _body.simulated = false;
    }

    private void PublishFacing(bool facesLeft) => _facesLeft.Value = facesLeft;
    private void ReceiveFacing(bool previous, bool facesLeft) => _mage.SetFacing(facesLeft);
    private void PlayAnimation(string trigger) => _animator.SetTrigger(trigger);
    private void PlayCast(int index) => PlayAnimation("Cast");
}
