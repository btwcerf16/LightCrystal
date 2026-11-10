using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class Enchanter : MonoBehaviour
{
    [SerializeField] private EnchantRune _runePrefab;
    [SerializeField] private EnchantRune _disenchantRunePrefab;
    [SerializeField] private Transform _runePoint;
    private readonly List<EnchantRune> _runes = new();
    public IReadOnlyList<EnchantRune> Runes => _runes;
    public EnchantRune Rune => _runes.Count > 0 ? _runes[0] : null;
    public EnchantRune DisenchantRune => _runes.Find(rune => rune.RemovesEnchant);
    public event Action<int> RuneBroken;

    public void Initialize(Sprite[] sprites, EnchantData[] enchants, bool singleChoice)
    {
        if (_runes.Count > 0)
            return;
        for (int i = 0; i < enchants.Length; i++)
        {
            EnchantRune prefab = enchants[i] != null ? _runePrefab : _disenchantRunePrefab;
            EnchantRune rune = Instantiate(prefab, _runePoint);
            rune.transform.localPosition = Vector3.right * ((i - (enchants.Length - 1) * 0.5f) * 1.4f);
            rune.Initialize(sprites[i], enchants[i]);
            rune.SetSingleChoice(singleChoice);
            int index = i;
            rune.Died += () => RuneBroken?.Invoke(index);
            _runes.Add(rune);
        }
    }
}
