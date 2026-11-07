using DG.Tweening;
using TMPro;
using UnityEngine;

public static class FloatingDamage
{
    private static readonly ObjectPool<TextMeshPro> Pool = new(CreateText, 128);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPool() => Pool.Clear();

    private static TextMeshPro _prefab;

    private static TextMeshPro CreateText()
    {
        if (_prefab == null)
            _prefab = Resources.Load<TextMeshPro>("DamageNumber");
        return Object.Instantiate(_prefab);
    }

    public static void Show(Vector3 position, float damage, EDamageType type = EDamageType.Physical, bool critical = false)
    {
        if (damage <= 0f)
            return;
        TextMeshPro text = Pool.GetObject();
        text.text = "−" + damage.ToString("F1");
        Color tint = critical ? new Color(1f, 0.2f, 0.18f)
            : type == EDamageType.Magic ? new Color(0.25f, 0.8f, 1f) : Color.white;
        text.color = tint;
        Transform popup = text.transform;
        popup.position = position + new Vector3(Random.Range(-0.15f, 0.15f), 1.2f, 0f);
        popup.localScale = Vector3.one * 0.6f;
        DOTween.Sequence()
            .Append(popup.DOMoveY(popup.position.y + 0.7f, 0.7f).SetEase(Ease.OutQuad))
            .Join(popup.DOScale(1f, 0.15f).SetEase(Ease.OutBack))
            .Join(DOTween.To(() => text.color, color => text.color = color, new Color(tint.r, tint.g, tint.b, 0f), 0.4f).SetDelay(0.3f))
            .OnComplete(() => Pool.ReturnObject(text))
            .SetLink(text.gameObject);
    }
}
