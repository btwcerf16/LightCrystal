using UnityEngine;

public sealed class LightningRodEffect : MonoBehaviour
{
    private LineRenderer _bolt;
    private LineRenderer _halo;
    private LineRenderer _ring;
    private float _endsAt;
    private float _duration;
    private float _width;

    public void Play(Material material, Vector2[] path, float radius)
    {
        if (_bolt == null)
        {
            _halo = WeaponEffectLine.Create(transform, "Lightning halo", material, 7);
            _bolt = WeaponEffectLine.Create(transform, "Lightning bolt", material, 8);
            _ring = WeaponEffectLine.Create(transform, "Lightning strike radius", material, 6);
        }

        bool storm = radius > 0f;
        _width = storm ? 0.15f : 0.055f;
        _duration = storm ? 0.28f : 0.15f;
        _endsAt = Time.time + _duration;

        const int subdivisions = 5;
        int pointCount = (path.Length - 1) * subdivisions + 1;
        _bolt.positionCount = _halo.positionCount = pointCount;
        for (int i = 0; i < path.Length - 1; i++)
        {
            Vector2 start = path[i];
            Vector2 end = path[i + 1];
            Vector2 side = Vector2.Perpendicular((end - start).normalized);
            float bend = Mathf.Min(Vector2.Distance(start, end) * 0.17f, storm ? 0.6f : 0.28f);
            for (int j = 0; j < subdivisions; j++)
            {
                Vector2 point = Vector2.Lerp(start, end, (float)j / subdivisions);
                if (j != 0)
                {
                    float sign = (i + j) % 2 == 0 ? 1f : -1f;
                    point += side * (sign * bend * Random.Range(0.45f, 1f));
                }
                _bolt.SetPosition(i * subdivisions + j, point);
                _halo.SetPosition(i * subdivisions + j, point);
            }
        }
        _bolt.SetPosition(pointCount - 1, path[^1]);
        _halo.SetPosition(pointCount - 1, path[^1]);

        _ring.positionCount = storm ? 33 : 0;
        for (int i = 0; i < _ring.positionCount; i++)
        {
            float angle = i * (Mathf.PI * 2f / 32f);
            _ring.SetPosition(i, path[^1] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
        Fade(1f);
    }

    private void Update()
    {
        if (_bolt != null && _bolt.enabled)
            Fade(Mathf.Clamp01((_endsAt - Time.time) / _duration));
    }

    private void Fade(float alpha)
    {
        Paint(_bolt, _width, new Color(1f, 0.95f, 0.66f, alpha));
        Paint(_halo, _width * 2.7f, new Color(1f, 0.49f, 0.08f, alpha * 0.6f));
        Paint(_ring, _width * 0.35f, new Color(1f, 0.72f, 0.20f, alpha * 0.65f));
    }

    private static void Paint(LineRenderer line, float width, Color color)
    {
        line.widthMultiplier = width;
        line.startColor = line.endColor = color;
        line.enabled = line.positionCount > 0 && color.a > 0f;
    }
}
