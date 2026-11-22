using UnityEngine;

internal static class WeaponEffectLine
{
    internal static LineRenderer Create(Transform parent, string name, Material material, int sortingOrder)
    {
        GameObject visual = new(name);
        visual.transform.SetParent(parent, false);
        LineRenderer line = visual.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.sortingOrder = sortingOrder;
        line.useWorldSpace = true;
        line.positionCount = 0;
        line.enabled = false;
        return line;
    }
}
