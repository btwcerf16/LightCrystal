using UnityEngine;

public sealed class Enchanter : MonoBehaviour
{
    [SerializeField] private EnchantRune _runePrefab;
    [SerializeField] private Transform _runePoint;
    private EnchantRune _rune;
    public EnchantRune Rune => _rune;

    public void Initialize(Sprite runeSprite, EnchantData enchant)
    {
        if (runeSprite == null || enchant == null)
        {
            Debug.LogError("Не найдены спрайт руны или зачарование.", this);
            return;
        }
        if (_rune == null)
            _rune = Instantiate(_runePrefab, _runePoint);
        _rune.Initialize(runeSprite, enchant);
    }
}
