using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class DefeatScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup _screenGroup;
    [SerializeField] private RectTransform _defeatTitle;
    [SerializeField] private Text _completedWavesText;
    private Sequence _screenAnimation;

    public void Show(int completedWaves)
    {
        _completedWavesText.text = "Пройдено волн: " + completedWaves;
        _screenGroup.alpha = 0f;
        _defeatTitle.localScale = Vector3.one * 0.96f;
        _screenAnimation = DOTween.Sequence()
            .Append(_screenGroup.DOFade(1f, 0.8f))
            .Join(_defeatTitle.DOScale(1.03f, 3f).SetEase(Ease.OutSine))
            .SetUpdate(true);
    }

    private void OnDestroy() => _screenAnimation?.Kill();
}
