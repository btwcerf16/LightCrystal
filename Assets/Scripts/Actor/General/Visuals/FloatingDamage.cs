using DG.Tweening;
using UnityEngine;

public static class FloatingDamage
{
    private static readonly ObjectPool<TextMesh> Pool = new(CreateText, 128);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPool() => Pool.Clear();

    private static TextMesh CreateText()
    {
        TextMesh text = new GameObject("DamageNumber").AddComponent<TextMesh>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 48;
        text.characterSize = 0.08f;
        text.anchor = TextAnchor.MiddleCenter;
        MeshRenderer renderer = text.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = text.font.material;
        renderer.sortingOrder = 100;
        return text;
    }

    public static void Show(Vector3 position, float damage)
    {
        if (damage <= 0f)
            return;
        TextMesh text = Pool.GetObject();
        text.text = "−" + damage.ToString("F1");
        text.color = Color.white;
        Transform popup = text.transform;
        popup.position = position + new Vector3(Random.Range(-0.15f, 0.15f), 1.2f, 0f);
        popup.localScale = Vector3.one * 0.6f;
        DOTween.Sequence()
            .Append(popup.DOMoveY(popup.position.y + 0.7f, 0.7f).SetEase(Ease.OutQuad))
            .Join(popup.DOScale(1f, 0.15f).SetEase(Ease.OutBack))
            .Join(DOTween.To(() => text.color, color => text.color = color, new Color(1f, 1f, 1f, 0f), 0.4f).SetDelay(0.3f))
            .OnComplete(() => Pool.ReturnObject(text))
            .SetLink(text.gameObject);
    }
}
