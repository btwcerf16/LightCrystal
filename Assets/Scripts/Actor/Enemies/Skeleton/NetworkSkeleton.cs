using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(NetworkAnimator))]
public sealed class NetworkSkeleton : NetworkBehaviour
{
    private readonly NetworkVariable<Vector2> _aim = new(Vector2.right);
    private Skeleton _skeleton;
    private NetworkAnimator _animator;
    private Rigidbody2D _body;

    private void Awake()
    {
        _skeleton = GetComponent<Skeleton>();
        _animator = GetComponent<NetworkAnimator>();
        _body = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        _body.simulated = IsServer;
        _skeleton.enabled = IsServer;
        if (IsServer)
        {
            _skeleton.AimChanged += PublishAim;
            _skeleton.AnimationTriggered += PlayAnimation;
            Crystal crystal = FindFirstObjectByType<Crystal>();
            if (_skeleton.MainTarget == null && crystal != null)
                _skeleton.SetMainTarget(crystal.transform);
            _skeleton.Initialize(_skeleton.MainTarget);
        }
        else
        {
            _aim.OnValueChanged += ReceiveAim;
            _skeleton.SetAim(_aim.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        _skeleton.AimChanged -= PublishAim;
        _skeleton.AnimationTriggered -= PlayAnimation;
        _aim.OnValueChanged -= ReceiveAim;
        _skeleton.enabled = false;
        _body.simulated = false;
    }

    private void PublishAim(Vector2 direction) => _aim.Value = direction;
    private void ReceiveAim(Vector2 previous, Vector2 direction) => _skeleton.SetAim(direction);
    private void PlayAnimation(string trigger) => _animator.SetTrigger(trigger);
}
