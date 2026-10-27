using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(ActorStats), typeof(ActorHealth))]
public sealed class SpellHolder : MonoBehaviour
{
    [SerializeField] private SpellConfig[] _spellConfigs = new SpellConfig[0];
    private readonly List<Spell> _spells = new();
    private ActorHealth _health;
    private NetworkObject _networkObject;
    private bool _canProcess;

    public IReadOnlyList<Spell> Spells => _spells;
    public int Count => _spellConfigs.Length;

    public SpellConfig GetConfig(int index)
    {
        return index >= 0 && index < _spellConfigs.Length ? _spellConfigs[index] : null;
    }
    public event Action<int, SpellCastData> CastRequested;
    public event Action<int> SpellCast;

    private void Awake()
    {
        _health = GetComponent<ActorHealth>();
        _networkObject = GetComponent<NetworkObject>();
    }

    private void OnEnable()
    {
        if (_networkObject == null)
            Initialize(true);
    }

    public void Initialize(bool canProcess)
    {
        ClearSpells();
        _canProcess = canProcess;
        foreach (SpellConfig config in _spellConfigs)
        {
            Spell spell = config != null ? config.AddSpell(gameObject) : null;
            _spells.Add(spell);
            if (spell == null)
                continue;
            spell.Initialize(config);
            spell.SetOwner(gameObject);
            spell.enabled = _canProcess;
            if (_canProcess)
                spell.Activate();
        }
    }

    public void CastSpell(int index, SpellCastData data)
    {
        if (!isActiveAndEnabled || _health.IsDead || index < 0 || index >= _spells.Count)
            return;
        if (!_canProcess)
        {
            CastRequested?.Invoke(index, data);
            return;
        }
        Spell spell = _spells[index];
        if (spell != null && spell.TryCast(data))
            SpellCast?.Invoke(index);
    }

    public void ClearSpells()
    {
        _canProcess = false;
        foreach (Spell spell in _spells)
        {
            if (spell == null)
                continue;
            spell.Deactivate();
            spell.enabled = false;
            Destroy(spell);
        }
        _spells.Clear();
    }

    private void OnDisable()
    {
        ClearSpells();
    }
}
