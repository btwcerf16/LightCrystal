using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(SpellHolder))]
public sealed class NetworkSpellHolder : NetworkBehaviour
{
    private NetworkList<NetworkSpellChargeState> _chargeStates;
    private NetworkList<float> _castProgress;
    private SpellHolder _holder;
    private PlayerBodyAnimation _bodyAnimation;
    private readonly NetworkVariable<int> _characterIndex = new(0);
    private readonly NetworkVariable<uint> _toggledSpellSlots = new();
    private int _initialCharacter;
    public CharacterConfig Character { get; private set; }
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
        Character = character;
        _bodyAnimation.SetModel(character.ModelPrefab);
        _holder.SetSpells(character.Spells, IsServer);
        if (IsServer)
        {
            _chargeStates.Clear();
            _castProgress.Clear();
            _toggledSpellSlots.Value = 0;
            for (int i = 0; i < _holder.Count; i++)
            {
                _chargeStates.Add(GetChargeState(_holder.Spells[i]));
                _castProgress.Add(-1f);
            }
        }
        else
            ReceiveChargeStates(default);
    }

    private void Awake()
    {
        _holder = GetComponent<SpellHolder>();
        _bodyAnimation = GetComponent<PlayerBodyAnimation>();
        _chargeStates = new NetworkList<NetworkSpellChargeState>();
        _castProgress = new NetworkList<float>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) _characterIndex.Value = _initialCharacter;
        _characterIndex.OnValueChanged += ReceiveCharacter;
        ReceiveCharacter(0, _characterIndex.Value);
        if (IsServer)
        {
            _holder.CastFailed += ShowCastFailure;
        }
        else
        {
            _chargeStates.OnListChanged += ReceiveChargeStates;
            ReceiveChargeStates(default);
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
        _holder.CastFailed -= ShowCastFailure;
        _chargeStates.OnListChanged -= ReceiveChargeStates;
        _holder.ClearSpells();
    }

    public float GetCastProgress(int index)
    {
        if (IsServer && index >= 0 && index < _holder.Spells.Count)
            return _holder.Spells[index] != null ? _holder.Spells[index].CastProgress : -1f;
        return index >= 0 && index < _castProgress.Count ? _castProgress[index] : -1f;
    }

    public bool IsSpellToggled(int index)
    {
        if (index < 0 || index >= _holder.Spells.Count || _holder.Spells[index] is not ToggleSpell toggle)
            return false;
        if (!IsSpawned || IsServer)
            return toggle.IsToggled;
        return index < 32 && (_toggledSpellSlots.Value & (1u << index)) != 0;
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer)
            return;
        uint toggledSpellSlots = 0;
        for (int i = 0; i < _castProgress.Count; i++)
        {
            Spell spell = i < _holder.Spells.Count ? _holder.Spells[i] : null;
            float progress = spell != null ? spell.CastProgress : -1f;
            if (_castProgress[i] != progress)
                _castProgress[i] = progress;
            NetworkSpellChargeState state = GetChargeState(spell);
            if (!_chargeStates[i].Equals(state))
                _chargeStates[i] = state;
            if (i < 32 && spell is ToggleSpell { IsToggled: true })
                toggledSpellSlots |= 1u << i;
        }
        if (_toggledSpellSlots.Value != toggledSpellSlots)
            _toggledSpellSlots.Value = toggledSpellSlots;
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

    private static NetworkSpellChargeState GetChargeState(Spell spell)
    {
        return spell == null ? default : new NetworkSpellChargeState
        {
            AvailableCharges = spell.AvailableCharges,
            RechargesAt = spell.CooldownEndsAt
        };
    }

    private void ReceiveChargeStates(NetworkListEvent<NetworkSpellChargeState> change)
    {
        for (int i = 0; i < _chargeStates.Count && i < _holder.Spells.Count; i++)
        {
            Spell spell = _holder.Spells[i];
            if (spell != null)
                spell.ApplyChargeState(_chargeStates[i]);
        }
    }
}
