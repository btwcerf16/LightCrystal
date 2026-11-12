using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(TrollBoss), typeof(NetworkTransform), typeof(NetworkAnimator))]
public sealed class NetworkTrollBoss : NetworkBehaviour
{
    [SerializeField] private ParticleSystem _dustPrefab;
    private readonly NetworkVariable<bool> _facesLeft = new();
    private TrollBoss _boss;
    private Rigidbody2D _body;
    private NetworkAnimator _animator;

    private void Awake()
    {
        _boss = GetComponent<TrollBoss>();
        _body = GetComponent<Rigidbody2D>();
        _animator = GetComponent<NetworkAnimator>();
    }

    public override void OnNetworkSpawn()
    {
        _body.simulated = IsServer;
        _boss.enabled = IsServer;
        if (IsServer)
        {
            _facesLeft.Value = false;
            _boss.SetFacing(false);
            _boss.FacingChanged += PublishFacing;
            _boss.AnimationTriggered += PlayAnimation;
            _boss.DustRaised += PlayDustRpc;
            _boss.Initialize(_boss.MainTarget);
        }
        else
        {
            _facesLeft.OnValueChanged += ReceiveFacing;
            _boss.SetFacing(_facesLeft.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        _boss.FacingChanged -= PublishFacing;
        _boss.AnimationTriggered -= PlayAnimation;
        _boss.DustRaised -= PlayDustRpc;
        _facesLeft.OnValueChanged -= ReceiveFacing;
        _boss.enabled = false;
        _body.simulated = false;
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void PlayDustRpc(Vector2 position, Vector2 scale)
    {
        if (_dustPrefab == null)
            return;

        ParticleSystem dust = Instantiate(_dustPrefab, position, Quaternion.identity);
        dust.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        dust.Play(true);
    }

    private void PlayAnimation(string trigger) => _animator.SetTrigger(trigger);
    private void PublishFacing(bool facesLeft) => _facesLeft.Value = facesLeft;
    private void ReceiveFacing(bool previous, bool facesLeft) => _boss.SetFacing(facesLeft);
}
