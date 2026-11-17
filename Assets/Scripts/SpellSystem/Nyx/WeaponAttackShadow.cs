using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public sealed class WeaponAttackShadow : NetworkBehaviour
{
    private const float Opacity = 0.42f;
    private const float FadeDuration = 0.2f;
    [SerializeField] private Transform _weaponMount;

    private Animator _model;
    private Animator _modelPrefab;
    private Weapon _weapon;
    private SpriteRenderer[] _renderers;
    private Color[] _originalColors;
    private float _fadeRemaining;
    private bool _playing;
    private EffectHandler _ownerEffects;

    public void Play(NetworkObject owner, int itemId, float reach, float physicalDamage)
    {
        StartAttack(owner, itemId, reach, physicalDamage, transform.position, transform.rotation);
        PlayRpc(owner, itemId, reach, transform.position, transform.rotation);
    }

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void PlayRpc(NetworkObjectReference ownerReference, int itemId, float reach, Vector3 position, Quaternion rotation)
    {
        if (ownerReference.TryGet(out NetworkObject owner, NetworkManager))
            StartAttack(owner, itemId, reach, 0f, position, rotation);
    }

    private void StartAttack(NetworkObject owner, int itemId, float reach, float physicalDamage, Vector3 position, Quaternion rotation)
    {
        _ownerEffects = owner.GetComponent<EffectHandler>();
        ActorHand hand = owner.GetComponentInChildren<ActorHand>();
        _weaponMount.SetPositionAndRotation(position + rotation * Vector3.right * reach, rotation);

        if (_weapon == null || _weapon.ItemId != itemId)
        {
            if (_weapon != null)
            {
                _weapon.gameObject.SetActive(false);
                Destroy(_weapon.gameObject);
            }
            Weapon prefab = NetworkManager.GetComponent<ItemFactory>().GetPrefab(itemId) as Weapon;
            _weapon = prefab != null ? Instantiate(prefab, _weaponMount, false) : null;
        }
        if (_weapon == null)
            return;

        _weapon.BindForUse(owner.GetComponent<ActorStats>(), hand, physicalDamage);
        SpriteRenderer weaponRenderer = _weapon.GetComponentInChildren<SpriteRenderer>(true);
        if (weaponRenderer != null)
            weaponRenderer.enabled = true;

        if (IsClient)
        {
            Animator modelPrefab = owner.GetComponent<NetworkSpellHolder>().Character?.ModelPrefab;
            if (_modelPrefab != modelPrefab || _model == null)
            {
                if (_model != null)
                {
                    _model.gameObject.SetActive(false);
                    Destroy(_model.gameObject);
                }
                _modelPrefab = modelPrefab;
                _model = modelPrefab != null ? Instantiate(modelPrefab, transform, false) : null;
            }
            if (_model != null)
            {
                _model.transform.SetPositionAndRotation(position, Quaternion.identity);
                _model.gameObject.SetActive(true);
                Vector3 scale = _modelPrefab.transform.localScale;
                if ((rotation * Vector3.right).x < 0f)
                    scale.x = -scale.x;
                _model.transform.localScale = scale;
            }
            _renderers = GetComponentsInChildren<SpriteRenderer>();
            _originalColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _originalColors[i] = _renderers[i].color;
            SetOpacity(Opacity);
        }

        _fadeRemaining = FadeDuration;
        _playing = _weapon.BeginPrimaryUse();
    }

    private void Update()
    {
        if (!IsSpawned)
            return;
        if (_weapon == null)
        {
            if (IsServer)
                NetworkObject.Despawn();
            return;
        }

        if (_playing && _weapon.IsAttacking && _ownerEffects != null && !_ownerEffects.IsStunned && !_ownerEffects.IsDowned && _weapon.OwnerStats.AttackSpeed.Value > 0f)
            return;
        if (_playing && _weapon.IsAttacking)
            _weapon.CancelUse();
        _playing = false;
        _fadeRemaining = Mathf.Max(0f, _fadeRemaining - Time.deltaTime);
        SetOpacity(Opacity * _fadeRemaining / FadeDuration);
        if (IsServer && _fadeRemaining == 0f)
            NetworkObject.Despawn();
    }

    private void SetOpacity(float opacity)
    {
        if (_renderers == null)
            return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null)
                continue;
            Color color = _originalColors[i];
            color.a *= opacity;
            _renderers[i].color = color;
        }
    }

    public override void OnNetworkDespawn()
    {
        SetOpacity(1f);
        _renderers = null;
        _originalColors = null;
        _ownerEffects = null;
        _playing = false;
        if (_weapon != null)
        {
            _weapon.UnbindForUse();
            _weapon.gameObject.SetActive(false);
        }
        if (_model != null)
            _model.gameObject.SetActive(false);
    }
}
