using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(NetworkAnimator))]
public sealed class NetworkGoblin : NetworkBehaviour
{
    private readonly NetworkVariable<Vector2> _aim = new(Vector2.right);
    private Goblin _goblin;
    private NetworkAnimator _networkAnimator;
    private Rigidbody2D _body;

    private void Awake()
    {
        _goblin = GetComponent<Goblin>();
        _networkAnimator = GetComponent<NetworkAnimator>();
        _body = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        _body.simulated = IsServer;
        _goblin.enabled = IsServer;
        if (IsServer)
        {
            _goblin.AnimationTriggered += PlayAnimation;
            _goblin.AimChanged += PublishAim;
            _goblin.Initialize(_goblin.MainTarget);
        }
        else
        {
            _aim.OnValueChanged += ReceiveAim;
            _goblin.SetAim(_aim.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        _goblin.AnimationTriggered -= PlayAnimation;
        _goblin.AimChanged -= PublishAim;
        _aim.OnValueChanged -= ReceiveAim;
        _goblin.enabled = false;
        _body.simulated = false;
    }

    private void PlayAnimation(string trigger)
    {
        _networkAnimator.SetTrigger(trigger);
    }

    private void PublishAim(Vector2 direction)
    {
        _aim.Value = direction;
    }

    private void ReceiveAim(Vector2 previous, Vector2 direction)
    {
        _goblin.SetAim(direction);
    }
}
