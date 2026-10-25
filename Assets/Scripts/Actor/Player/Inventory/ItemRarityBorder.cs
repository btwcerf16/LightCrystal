using UnityEngine;
using UnityEngine.UI;

public sealed class ItemRarityBorder : MaskableGraphic
{
    private EItemRarity _rarity;

    public static ItemRarityBorder Create(Transform parent)
    {
        ItemRarityBorder border = new GameObject("RarityOrnament", typeof(RectTransform), typeof(CanvasRenderer))
            .AddComponent<ItemRarityBorder>();
        border.transform.SetParent(parent, false);
        border.rectTransform.anchorMin = Vector2.zero;
        border.rectTransform.anchorMax = Vector2.one;
        border.rectTransform.offsetMin = border.rectTransform.offsetMax = Vector2.zero;
        border.raycastTarget = false;
        return border;
    }

    public void SetRarity(EItemRarity rarity)
    {
        _rarity = rarity;
        color = ItemRarityColors.Get(rarity);
        enabled = rarity >= EItemRarity.Epic;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (_rarity < EItemRarity.Epic)
            return;

        Rect rect = rectTransform.rect;
        float size = Mathf.Min(14f, rect.width * 0.24f, rect.height * 0.24f);
        if (size <= 0f)
            return;

        DrawCorner(mesh, new Vector2(rect.xMin + 2f, rect.yMax - 2f), new Vector2(1f, -1f), size);
        DrawCorner(mesh, new Vector2(rect.xMax - 2f, rect.yMax - 2f), new Vector2(-1f, -1f), size);
        DrawCorner(mesh, new Vector2(rect.xMin + 2f, rect.yMin + 2f), Vector2.one, size);
        DrawCorner(mesh, new Vector2(rect.xMax - 2f, rect.yMin + 2f), new Vector2(-1f, 1f), size);

        if (_rarity == EItemRarity.Legendary)
        {
            DrawCrest(mesh, new Vector2(rect.center.x, rect.yMax - 3f), -1f, size);
            DrawCrest(mesh, new Vector2(rect.center.x, rect.yMin + 3f), 1f, size);
        }
    }

    private void DrawCorner(VertexHelper mesh, Vector2 origin, Vector2 direction, float size)
    {
        if (_rarity == EItemRarity.Mythical)
        {
            Line(mesh, Point(origin, direction, size, 0f, 1f), Point(origin, direction, size, 0.12f, 0.12f));
            Line(mesh, Point(origin, direction, size, 0.12f, 0.12f), Point(origin, direction, size, 1f, 0f));
            Line(mesh, Point(origin, direction, size, 0.12f, 0.12f), Point(origin, direction, size, 0.7f, 0.7f));
            Diamond(mesh, Point(origin, direction, size, 0.55f, 0.55f), 2.2f);
            return;
        }

        Curve(mesh, origin, direction, size, new Vector2(0f, 1f), Vector2.zero,
            new Vector2(1.2f, 0f), new Vector2(0.85f, 0.55f));
        Curve(mesh, origin, direction, size, new Vector2(0.85f, 0.55f), new Vector2(0.65f, 0.9f),
            new Vector2(0.25f, 0.5f), new Vector2(0.6f, 0.35f));

        if (_rarity == EItemRarity.Legendary)
        {
            Curve(mesh, origin, direction, size, new Vector2(1f, 0f), Vector2.zero,
                new Vector2(0f, 1.2f), new Vector2(0.55f, 0.85f));
            Diamond(mesh, Point(origin, direction, size, 0.65f, 0.65f), 2.5f);
        }
    }

    private void DrawCrest(VertexHelper mesh, Vector2 center, float direction, float size)
    {
        Vector2 gem = center + Vector2.up * (direction * 2f);
        Diamond(mesh, gem, 2.5f);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 wing = center + new Vector2(side * size * 0.55f, direction * 5f);
            Line(mesh, gem + Vector2.right * (side * 3f), wing);
            Line(mesh, wing, center + Vector2.right * (side * size));
        }
    }

    private Vector2 Point(Vector2 origin, Vector2 direction, float size, float x, float y)
    {
        return origin + Vector2.Scale(direction, new Vector2(x, y)) * size;
    }

    private void Curve(VertexHelper mesh, Vector2 origin, Vector2 direction, float size,
        Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        Vector2 previous = Point(origin, direction, size, a.x, a.y);
        for (int i = 1; i <= 10; i++)
        {
            float t = i / 10f;
            float inverse = 1f - t;
            Vector2 point = inverse * inverse * inverse * a + 3f * inverse * inverse * t * b
                + 3f * inverse * t * t * c + t * t * t * d;
            Vector2 next = Point(origin, direction, size, point.x, point.y);
            Line(mesh, previous, next);
            previous = next;
        }
    }

    private void Line(VertexHelper mesh, Vector2 start, Vector2 end)
    {
        Stroke(mesh, start, end, 3.2f, new Color(0.018f, 0.016f, 0.014f));
        Stroke(mesh, start, end, 1.3f, color);
    }

    private void Stroke(VertexHelper mesh, Vector2 start, Vector2 end, float width, Color tint)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x) * (width * 0.5f);
        int index = mesh.currentVertCount;
        mesh.AddVert(start - normal, tint, Vector2.zero);
        mesh.AddVert(start + normal, tint, Vector2.zero);
        mesh.AddVert(end + normal, tint, Vector2.zero);
        mesh.AddVert(end - normal, tint, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index, index + 2, index + 3);
    }

    private void Diamond(VertexHelper mesh, Vector2 center, float radius)
    {
        int index = mesh.currentVertCount;
        mesh.AddVert(center + Vector2.up * radius, color, Vector2.zero);
        mesh.AddVert(center + Vector2.right * radius, color, Vector2.zero);
        mesh.AddVert(center - Vector2.up * radius, color, Vector2.zero);
        mesh.AddVert(center - Vector2.right * radius, color, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index, index + 2, index + 3);
    }
}
