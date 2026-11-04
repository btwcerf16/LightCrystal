using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(SpellHolder))]
public sealed class NetworkSpellHolder : NetworkBehaviour
{
    private NetworkList<double> _cooldowns;
    private NetworkList<float> _castProgress;
    private SpellHolder _holder;
    [SerializeField] private SpriteRenderer _bodyRenderer;
    private readonly NetworkVariable<int> _characterIndex = new(0);
    private int _initialCharacter;
    public void SetInitialCharacter(int index) => _initialCharacter = index;

    [ConsoleCommand]
    private string SetSpells(CharacterConfig character)
    {
        if (!IsServer)
            throw new System.InvalidOperationException("Команда выполняется на сервере.");
        int index = NetworkManager.GetComponent<LobbyConnection>().Characters.IndexOf(character);
        if (index < 0) throw new System.ArgumentException("Класс не найден в каталоге.");
        if (GetComponent<CourierBagSpell>() != null && index != _characterIndex.Value)
        {
            Inventory inventory = GetComponent<Inventory>();
            for (int i = 6; i < inventory.Size; i++)
                if (inventory.GetItem(i) != null)
                    throw new System.InvalidOperationException("Сначала освободите дополнительные ячейки сумки.");
        }
        if (_characterIndex.Value != index) _characterIndex.Value = index;
        return "Способности: " + character.DisplayName;
    }

    private void ReceiveCharacter(int previous, int current)
    {
        CharacterConfig character = NetworkManager.GetComponent<LobbyConnection>().Characters.Get(current);
        if (character == null) return;
        if (_bodyRenderer != null)
        {
            _bodyRenderer.sprite = character.BodySprite;
            _bodyRenderer.color = Color.white;
        }
        _holder.SetSpells(character.Spells, IsServer);
        if (IsServer)
        {
            _cooldowns.Clear();
            _castProgress.Clear();
            for (int i = 0; i < _holder.Count; i++)
            {
                _cooldowns.Add(0);
                _castProgress.Add(-1f);
            }
        }
        else
            ReceiveCooldowns(default);
    }

    private void Awake()
    {
        _holder = GetComponent<SpellHolder>();
        _cooldowns = new NetworkList<double>();
        _castProgress = new NetworkList<float>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) _characterIndex.Value = _initialCharacter;
        _characterIndex.OnValueChanged += ReceiveCharacter;
        ReceiveCharacter(0, _characterIndex.Value);
        if (IsServer)
        {
            _holder.SpellCast += PublishCooldown;
            _holder.CooldownsChanged += PublishCooldowns;
            _holder.CastFailed += ShowCastFailure;
        }
        else
        {
            _cooldowns.OnListChanged += ReceiveCooldowns;
            ReceiveCooldowns(default);
        }
        if (IsOwner)
        {
            _holder.CastRequested += RequestCast;
            _holder.CancelCastRequested += RequestCancelCast;
        }
    }

    public override void OnNetworkDespawn()
    {
        _characterIndex.OnValueChanged -= ReceiveCharacter;
        _holder.CastRequested -= RequestCast;
        _holder.CancelCastRequested -= RequestCancelCast;
        _holder.SpellCast -= PublishCooldown;
        _holder.CooldownsChanged -= PublishCooldowns;
        _holder.CastFailed -= ShowCastFailure;
        _cooldowns.OnListChanged -= ReceiveCooldowns;
        _holder.ClearSpells();
    }

    public float GetCastProgress(int index)
    {
        if (IsServer && index >= 0 && index < _holder.Spells.Count)
            return _holder.Spells[index] != null ? _holder.Spells[index].CastProgress : -1f;
        return index >= 0 && index < _castProgress.Count ? _castProgress[index] : -1f;
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer)
            return;
        for (int i = 0; i < _castProgress.Count; i++)
        {
            float progress = i < _holder.Spells.Count && _holder.Spells[i] != null ? _holder.Spells[i].CastProgress : -1f;
            if (_castProgress[i] != progress)
                _castProgress[i] = progress;
        }
    }

    private void RequestCancelCast() => CancelCastRpc();

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void CancelCastRpc()
    {
        _holder.CancelCast();
    }

    private void RequestCast(int index, SpellCastData data)
    {
        NetworkObjectReference target = new NetworkObjectReference((NetworkObject)null);
        if (data.Target != null)
        {
            NetworkObject targetObject = data.Target.GetComponentInParent<NetworkObject>();
            if (targetObject != null && targetObject.IsSpawned)
                target = targetObject;
        }
        CastSpellRpc(index, data.Position, data.Direction, target);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void CastSpellRpc(int index, Vector2 position, Vector2 direction, NetworkObjectReference target)
    {
        if (!float.IsFinite(position.x) || !float.IsFinite(position.y) ||
            !float.IsFinite(direction.x) || !float.IsFinite(direction.y))
            return;
        target.TryGet(out NetworkObject targetObject, NetworkManager);
        _holder.CastSpell(index, new SpellCastData
        {
            Position = position,
            Direction = direction,
            Target = targetObject != null ? targetObject.gameObject : null,
            Caster = gameObject
        });
    }

    private void ShowCastFailure(string message)
    {
        GetComponent<NetworkPlayerWallet>().ShowHint(message);
    }

    private void PublishCooldowns()
    {
        for (int i = 0; i < _holder.Spells.Count; i++)
            if (_holder.Spells[i] != null)
                PublishCooldown(i);
    }

    private void PublishCooldown(int index)
    {
        _cooldowns[index] = _holder.Spells[index].CooldownEndsAt;
    }

    private void ReceiveCooldowns(NetworkListEvent<double> change)
    {
        for (int i = 0; i < _cooldowns.Count && i < _holder.Spells.Count; i++)
        {
            Spell spell = _holder.Spells[i];
            if (spell != null)
                spell.SetCooldown(_cooldowns[i]);
        }
    }
}
