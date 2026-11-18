using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(NetworkAnimator))]
public sealed class NetworkSkeletonEnchanter : NetworkBehaviour
{
    private readonly NetworkVariable<bool> _facesLeft = new();
    private SkeletonEnchanter _enemy;
    private NetworkAnimator _animator;
    private SpellHolder _spells;
    private Rigidbody2D _body;

    private void Awake()
    {
        _enemy = GetComponent<SkeletonEnchanter>();
        _animator = GetComponent<NetworkAnimator>();
        _spells = GetComponent<SpellHolder>();
        _body = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        _body.simulated = IsServer;
        _enemy.enabled = IsServer;
        _spells.Initialize(IsServer);
        if (IsServer)
        {
            _facesLeft.Value = false;
            _enemy.SetFacing(false);
            _enemy.FacingChanged += PublishFacing;
            _enemy.DiedVisual += PlayDeath;
            _spells.SpellCast += PlayCast;
            _enemy.Initialize(_enemy.MainTarget != null ? _enemy.MainTarget : FindFirstObjectByType<Crystal>()?.transform);
        }
        else
        {
            _facesLeft.OnValueChanged += ReceiveFacing;
            _enemy.SetFacing(_facesLeft.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        _facesLeft.OnValueChanged -= ReceiveFacing;
        _enemy.FacingChanged -= PublishFacing;
        _enemy.DiedVisual -= PlayDeath;
        _spells.SpellCast -= PlayCast;
        _enemy.enabled = false;
        _spells.ClearSpells();
        _body.simulated = false;
    }

    private void PublishFacing(bool facesLeft) => _facesLeft.Value = facesLeft;
    private void ReceiveFacing(bool previous, bool facesLeft) => _enemy.SetFacing(facesLeft);
    private void PlayCast(int index) => _animator.SetTrigger("Cast");
    private void PlayDeath() => _animator.SetTrigger("Death");
}
