using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class SceneTransition : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
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
        _panel.SetActive(false);
        if (LoadingProgressText != null) LoadingProgressText.font = HudStyle.PixelFont;
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
