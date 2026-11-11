using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(Trap))]
public sealed class NetworkTrap : NetworkBehaviour
{
    [SerializeField] private Animator _warningAnimator;
    [SerializeField] private ParticleSystem _triggerEffectPrefab;
    private readonly NetworkVariable<Vector3> _warningData = new();
    private Trap _trap;

    private void Awake() => _trap = GetComponent<Trap>();

    public override void OnNetworkSpawn()
    {
        _trap.enabled = IsServer;
        if (IsServer)
            _trap.Finished += Despawn;

        if (_warningAnimator == null)
            return;

        if (IsServer)
            _warningData.Value = new Vector3(_trap.Radius, _trap.TriggerDelay, (float)NetworkManager.ServerTime.Time + _trap.TriggerDelay);
        _warningData.OnValueChanged += RefreshWarning;
        RefreshWarning(default, _warningData.Value);
    }

    public override void OnNetworkDespawn()
    {
        _trap.Finished -= Despawn;
        _warningData.OnValueChanged -= RefreshWarning;
    }

    private void RefreshWarning(Vector3 previous, Vector3 warning)
    {
        float radius = warning.x;
        float duration = warning.y;
        if (duration <= 0f)
            return;

        float elapsed = (float)NetworkManager.ServerTime.Time + duration - warning.z;
        _warningAnimator.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        _warningAnimator.speed = 1f / duration;
        _warningAnimator.Play("Awake", 0, Mathf.Clamp01(elapsed / duration));
        _warningAnimator.Update(0f);
    }

    private void Despawn()
    {
        if (!IsServer || !IsSpawned)
            return;

        if (_triggerEffectPrefab != null)
            PlayTriggerEffectRpc(transform.position, _trap.Radius);

        NetworkManager.GetComponent<NetworkObjectPool>().ReturnObject(NetworkObject);
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void PlayTriggerEffectRpc(Vector3 position, float radius)
    {
        ParticleSystem particles = Instantiate(_triggerEffectPrefab, position, Quaternion.identity);
        particles.transform.localScale *= radius;
        particles.Play(true);
    }
}
