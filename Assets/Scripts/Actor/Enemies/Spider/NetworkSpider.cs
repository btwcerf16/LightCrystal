using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(NetworkAnimator))]
public sealed class NetworkSpider : NetworkBehaviour
{
    private readonly NetworkVariable<Vector2> _aim = new(Vector2.down);
    private Spider _spider;
    private Rigidbody2D _body;
    private NetworkAnimator _animator;
    private SpellHolder _spells;
    private void Awake()
    {
        _spider = GetComponent<Spider>(); _body = GetComponent<Rigidbody2D>(); _animator = GetComponent<NetworkAnimator>(); _spells = GetComponent<SpellHolder>();
    }
    public override void OnNetworkSpawn()
    {
        _body.simulated = IsServer;
        _spider.enabled = IsServer;
        _spells.Initialize(IsServer);
        if (IsServer)
        {
            _spider.AimChanged += PublishAim;
            _spider.AnimationTriggered += PlayAnimation;
            _spells.SpellCast += PlayWebAnimation;
            _spider.SetAim(Vector2.down);
            if (_spider.MainTarget == null) _spider.SetMainTarget(FindFirstObjectByType<Crystal>()?.transform);
            _spider.Initialize(_spider.MainTarget);
        }
        else
        {
            _aim.OnValueChanged += ReceiveAim;
            _spider.SetAim(_aim.Value);
        }
    }
    private void PublishAim(Vector2 direction) => _aim.Value = direction;
    private void ReceiveAim(Vector2 previous, Vector2 direction) => _spider.SetAim(direction);
    private void PlayAnimation(string trigger) => _animator.SetTrigger(trigger);
    private void PlayWebAnimation(int index) => _animator.SetTrigger("Cast");
    public override void OnNetworkDespawn()
    {
        _spider.AimChanged -= PublishAim;
        _spider.AnimationTriggered -= PlayAnimation;
        _spells.SpellCast -= PlayWebAnimation;
        _aim.OnValueChanged -= ReceiveAim;
        _spells.ClearSpells();
        _spider.enabled = false;
        _body.simulated = false;
    }
}
