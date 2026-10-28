using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(NetworkAnimator))]
public sealed class NetworkGoblin : NetworkBehaviour
{
    private Goblin _goblin;
    private NetworkAnimator _networkAnimator;
    private Rigidbody2D _body;

    private NetworkVariable<Vector2> _aimVector = new(Vector2.right);

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
            _goblin.AimChanged += PublishAim;
            _goblin.AnimationTriggered += PlayAnimation;
            _goblin.Initialize(_goblin.MainTarget);
        }
        else
        {
            _aimVector.OnValueChanged += ReceiveAim;
            _goblin.SetAim(_aimVector.Value);
        }

    }

    public override void OnNetworkDespawn()
    {
        _goblin.AnimationTriggered -= PlayAnimation;
        _aimVector.OnValueChanged -= ReceiveAim;
        _goblin.AimChanged -= PublishAim;
        _goblin.enabled = false;
        _body.simulated = false;

    }

    private void PublishAim(Vector2 direction)
    {
        _aimVector.Value = direction;
    }

    private void ReceiveAim(Vector2 previous, Vector2 direction)
    {
        _goblin.SetAim(direction);
    }

    private void PlayAnimation(string trigger)
    {
        _networkAnimator.SetTrigger(trigger);
    }

}
