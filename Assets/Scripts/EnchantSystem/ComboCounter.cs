using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkObject))]
public sealed class ComboCounter : NetworkBehaviour
{
    [SerializeField] private Canvas _canvas;
    [SerializeField] private CanvasGroup _labelGroup;
    [SerializeField] private Text _label;

    private readonly NetworkVariable<int> _count = new(0,
        NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
    private Tween _pulse;
    private Tween _fade;

    internal void SetCount(int count) => _count.Value = count;

    public override void OnNetworkSpawn()
    {
        if (!IsClient || !IsOwner)
            return;

        _canvas.enabled = true;
        _count.OnValueChanged += ShowCount;
        ShowCount(0, _count.Value);
    }

    public override void OnNetworkDespawn()
    {
        _count.OnValueChanged -= ShowCount;
        _pulse?.Kill();
        _fade?.Kill();
        _canvas.enabled = false;
        _labelGroup.alpha = 0f;
    }

    private void ShowCount(int previous, int count)
    {
        _pulse?.Kill();
        _fade?.Kill();
        if (count <= 0)
        {
            if (_labelGroup.alpha > 0f)
                _fade = _labelGroup.DOFade(0f, 0.2f);
            return;
        }

        _label.text = $"КОМБО ×{count}";
        Color red = new Color(1f, 0.15f, 0.12f);
        _label.color = count <= 25
            ? Color.Lerp(Color.white, red, (count - 1f) / 24f)
            : Color.Lerp(red, new Color(0.55f, 0.04f, 0.12f), (count - 25f) / 25f);
        _labelGroup.alpha = 1f;
        Transform labelTransform = _labelGroup.transform;
        labelTransform.localScale = Vector3.one;
        _pulse = labelTransform.DOPunchScale(new Vector3(0.18f, 0.18f, 0f), 0.22f, 3, 0.6f);
    }
}
