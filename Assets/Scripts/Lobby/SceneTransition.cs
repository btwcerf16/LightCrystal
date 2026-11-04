using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class SceneTransition : MonoBehaviour
{
    public Text LoadingProgressText;
    public Image LoadingProgressBar;
    public static SceneTransition Instance { get; private set; }
    private Animator _animator;
    private CanvasGroup _group;
    private bool _covered;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _animator = GetComponent<Animator>();
        _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        GetComponent<Canvas>().sortingOrder = 1000;
        gameObject.AddComponent<GraphicRaycaster>();
        _group = gameObject.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = false;
        if (LoadingProgressText != null) LoadingProgressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public IEnumerator Cover()
    {
        _covered = false;
        _group.blocksRaycasts = true;
        _animator.ResetTrigger("End");
        _animator.SetTrigger("Start");
        if (LoadingProgressText != null) LoadingProgressText.text = "Загрузка…";
        if (LoadingProgressBar != null) LoadingProgressBar.fillAmount = 0f;
        while (!_covered) yield return null;
    }

    public void OnAnimationOver() => _covered = true;

    public void Open()
    {
        if (LoadingProgressBar != null) LoadingProgressBar.fillAmount = 1f;
        _animator.ResetTrigger("Start");
        _animator.SetTrigger("End");
        _group.blocksRaycasts = false;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }
}
