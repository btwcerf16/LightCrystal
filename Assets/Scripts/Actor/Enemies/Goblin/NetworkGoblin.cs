using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(NetworkAnimator))]
public sealed class NetworkGoblin : NetworkBehaviour
{
    private readonly NetworkVariable<Vector2> _aim = new(Vector2.right);
    [SerializeField] private SpriteRenderer _bodyRenderer;
    [SerializeField] private Color _hitColor = Color.red;
    [SerializeField, Min(0f)] private float _hitFlashDuration = 0.15f;

    private Goblin _goblin;
    private ActorHealth _health;
    private NetworkAnimator _networkAnimator;
    private Rigidbody2D _body;
    private Color _bodyColor;
    private Coroutine _hitFlash;

    private void Awake()
    {
        _goblin = GetComponent<Goblin>();
        _health = GetComponent<ActorHealth>();
        _networkAnimator = GetComponent<NetworkAnimator>();
        _body = GetComponent<Rigidbody2D>();
        if (_bodyRenderer != null)
            _bodyColor = _bodyRenderer.color;
    }

    public override void OnNetworkSpawn()
    {
        _body.simulated = IsServer;
        _goblin.enabled = IsServer;
        if (IsServer)
        {
            _goblin.AnimationTriggered += PlayAnimation;
            _goblin.AimChanged += PublishAim;
            _health.DamageTaken += OnDamageTaken;
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
        _health.DamageTaken -= OnDamageTaken;
        ResetHitFlash();
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

    private void OnDamageTaken(float damage)
    {
        FlashHitRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void FlashHitRpc()
    {
        if (_bodyRenderer == null)
            return;
        ResetHitFlash();
        _hitFlash = StartCoroutine(FlashHit());
    }

    private IEnumerator FlashHit()
    {
        _bodyRenderer.color = _hitColor;
        yield return new WaitForSeconds(_hitFlashDuration);
        _bodyRenderer.color = _bodyColor;
        _hitFlash = null;
    }

    private void OnDisable()
    {
        ResetHitFlash();
    }

    private void ResetHitFlash()
    {
        if (_hitFlash != null)
            StopCoroutine(_hitFlash);
        _hitFlash = null;
        if (_bodyRenderer != null)
            _bodyRenderer.color = _bodyColor;
    }
}
