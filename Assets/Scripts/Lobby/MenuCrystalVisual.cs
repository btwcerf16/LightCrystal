using UnityEngine;
using UnityEngine.UI;

public sealed class MenuCrystalVisual : MonoBehaviour
{
    private RectTransform _crystal;
    private Image _glow;
    private readonly Image[] _particles = new Image[12];
    private Texture2D _glowTexture;
    private Sprite _glowSprite;

    public static void Create(Transform parent, Sprite sprite, Vector2 position, float scale)
    {
        if (sprite == null)
            return;

        RectTransform root = CreateRect("CrystalVisual", parent, Vector2.zero);
        root.anchoredPosition = position;
        root.localScale = Vector3.one * scale;
        root.gameObject.AddComponent<MenuCrystalVisual>().Build(sprite);
    }

    private void Build(Sprite sprite)
    {
        const int size = 96;
        _glowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        _glowTexture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 point = new Vector2(x, y) * (2f / (size - 1)) - Vector2.one;
            float intensity = 1f - Mathf.Clamp01(point.magnitude);
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255f * intensity * intensity));
        }
        _glowTexture.SetPixels32(pixels);
        _glowTexture.Apply(false, true);
        _glowSprite = Sprite.Create(_glowTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));

        _glow = CreateImage("CrystalGlow", new Vector2(880f, 740f));
        _glow.sprite = _glowSprite;
        for (int i = 0; i < _particles.Length; i++)
        {
            _particles[i] = CreateImage("CrystalParticle", Vector2.one * (3f + i % 3));
            _particles[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        Image crystal = CreateImage("Crystal", new Vector2(256f, 384f));
        crystal.sprite = sprite;
        crystal.preserveAspect = true;
        _crystal = crystal.rectTransform;
        LateUpdate();
    }

    private void LateUpdate()
    {
        float time = Time.unscaledTime;
        float rise = Mathf.Sin(time * 1.3f) * 14f;
        _crystal.anchoredPosition = new Vector2(0f, rise);
        _crystal.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 0.8f) * 2.5f);
        _glow.rectTransform.anchoredPosition = _crystal.anchoredPosition;
        _glow.color = new Color(0.34f, 0.7f, 0.8f, 0.27f + Mathf.Sin(time * 1.3f) * 0.06f);
        for (int i = 0; i < _particles.Length; i++)
        {
            float progress = Mathf.Repeat(time * (0.16f + i % 3 * 0.02f) + i * 0.173f, 1f);
            float x = (i * 137 % 277) - 138f + Mathf.Sin(time * 1.1f + i) * 11f;
            _particles[i].rectTransform.anchoredPosition = new Vector2(x, -154f + 330f * progress + rise);
            _particles[i].color = new Color(0.58f, 0.83f, 0.86f, Mathf.Sin(progress * Mathf.PI) * 0.55f);
        }
    }

    private Image CreateImage(string name, Vector2 size)
    {
        Image image = CreateRect(name, transform, size).gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        return rect;
    }

    private void OnDestroy()
    {
        Destroy(_glowSprite);
        Destroy(_glowTexture);
    }
}
