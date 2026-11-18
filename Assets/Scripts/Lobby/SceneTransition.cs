using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class SceneTransition : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Sprite _crystalSprite;
    public Text LoadingProgressText;
    public Image LoadingProgressBar;
    public static SceneTransition Instance { get; private set; }
    private CanvasGroup _group;
    private Tween _fade;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        GetComponent<Canvas>().sortingOrder = 1000;
        gameObject.AddComponent<GraphicRaycaster>();
        _group = gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
        MenuCrystalVisual.Create(_panel.transform, _crystalSprite, new Vector2(0f, 48f), 0.6f);
        CreateLoadingHint();
        _panel.SetActive(false);
        if (LoadingProgressText != null) LoadingProgressText.font = HudStyle.PixelFont;
    }

    private void CreateLoadingHint()
    {
        Text hint = new GameObject("LoadingHint", typeof(RectTransform)).AddComponent<Text>();
        hint.transform.SetParent(_panel.transform, false);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        hint.rectTransform.anchoredPosition = new Vector2(0f, -110f);
        hint.rectTransform.sizeDelta = new Vector2(680f, 60f);
        hint.alignment = TextAnchor.MiddleCenter;
        hint.font = HudStyle.PixelFont;
        hint.fontSize = 24;
        hint.color = HudStyle.Text;
        hint.raycastTarget = false;
        hint.text = "Свет кристалла восстанавливает рассудок.";
    }

    public IEnumerator Cover()
    {
        _fade?.Kill();
        _panel.SetActive(true);
        _group.blocksRaycasts = true;
        if (LoadingProgressText != null)
        {
            LoadingProgressText.text = "Загрузка…";
            LoadingProgressText.gameObject.SetActive(true);
        }
        if (LoadingProgressBar != null)
        {
            LoadingProgressBar.fillAmount = 0f;
            LoadingProgressBar.gameObject.SetActive(true);
        }
        _fade = _group.DOFade(1f, 1f).SetUpdate(true);
        yield return _fade.WaitForCompletion();
    }

    public void Open()
    {
        if (!_panel.activeSelf)
            return;

        _fade?.Kill();
        if (LoadingProgressText != null) LoadingProgressText.gameObject.SetActive(false);
        if (LoadingProgressBar != null) LoadingProgressBar.gameObject.SetActive(false);
        _group.blocksRaycasts = false;
        _fade = _group.DOFade(0f, 1f).SetUpdate(true).OnComplete(() => _panel.SetActive(false));
    }

    private void OnDestroy()
    {
        _fade?.Kill();
        if (Instance == this) Instance = null;
    }
}
