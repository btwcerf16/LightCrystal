using UnityEngine;

public sealed class SpellRangePreview : MonoBehaviour
{
    public void Show(float radius)
    {
        transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        gameObject.SetActive(radius > 0f);
    }

    public void Hide() => gameObject.SetActive(false);
}
