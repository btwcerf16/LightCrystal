using UnityEngine;

public sealed class MagicWandBeamEffect : MonoBehaviour
{
    private LineRenderer _halo;
    private LineRenderer _core;
    private LineRenderer _flash;
    private float _duration;
    private float _remaining;
    private float _width;

    public void Play(Material material, Vector2 origin, Vector2 end, bool charged)
    {
        if (_core == null)
        {
            _halo = WeaponEffectLine.Create(transform, "Wand beam halo", material, 6);
            _core = WeaponEffectLine.Create(transform, "Wand beam core", material, 7);
            _flash = WeaponEffectLine.Create(transform, "Wand muzzle flash", material, 8);
        }

        _duration = charged ? 0.24f : 0.14f;
        _remaining = _duration;
        _width = charged ? 0.18f : 0.105f;

        SetSegment(_halo, origin, end);
        SetSegment(_core, origin, end);

        Vector2 direction = (end - origin).normalized;
        Vector2 side = Vector2.Perpendicular(direction) * (_width * 0.9f);
        Vector2 back = origin - direction * (_width * 0.7f);
        _flash.positionCount = 3;
        _flash.SetPosition(0, back + side);
        _flash.SetPosition(1, origin);
        _flash.SetPosition(2, back - side);
        ApplyFade(1f);
    }

    private static void SetSegment(LineRenderer line, Vector2 origin, Vector2 end)
    {
        line.positionCount = 2;
        line.SetPosition(0, origin);
        line.SetPosition(1, end);
    }

    private void Update()
    {
        if (_remaining <= 0f)
            return;

        _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);
        ApplyFade(_remaining / _duration);
    }

    private void ApplyFade(float fraction)
    {
        Paint(_halo, _width * 1.6f, new Color(0.25f, 0.68f, 0.95f, 0.65f), fraction);
        Paint(_core, _width * 0.6f, new Color(0.77f, 0.96f, 1f), fraction);
        Paint(_flash, _width * 0.45f, new Color(0.84f, 1f, 1f), Mathf.Clamp01(fraction * 3f - 1.5f));
    }

    private static void Paint(LineRenderer line, float width, Color color, float fraction)
    {
        line.enabled = fraction > 0f;
        line.widthMultiplier = width * Mathf.Lerp(0.25f, 1f, fraction);
        color.a *= fraction;
        line.startColor = line.endColor = color;
    }
}
