using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class EventNotificationsUI : MonoBehaviour
{
    private readonly Queue<(string Text, Color TextColor)> _messages = new();
    private RectTransform _panel;
    private Text _messageText;
    private CanvasGroup _canvasGroup;
    private Coroutine _displayMessages;
    private Tween _fade;
    private string _lastMessage;
    private Color _lastTextColor;
    private bool _isMenuOpen;

    public void Initialize(Transform canvas)
    {
        transform.SetParent(canvas, false);
        Canvas notificationCanvas = gameObject.AddComponent<Canvas>();
        notificationCanvas.overrideSorting = true;
        notificationCanvas.sortingOrder = 101;
        _panel = (RectTransform)transform;
        _panel.anchorMin = _panel.anchorMax = new Vector2(0f, 0.5f);
        _panel.pivot = new Vector2(0f, 0.5f);
        _panel.anchoredPosition = new Vector2(18f, 0f);
        _panel.sizeDelta = new Vector2(290f, 100f);
        Image background = gameObject.AddComponent<Image>();
        background.color = HudStyle.Surface;
        background.raycastTarget = false;
        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;

        Image accent = new GameObject("Accent", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        accent.transform.SetParent(transform, false);
        accent.rectTransform.anchorMin = Vector2.zero;
        accent.rectTransform.anchorMax = new Vector2(0f, 1f);
        accent.rectTransform.pivot = Vector2.zero;
        accent.rectTransform.sizeDelta = new Vector2(2f, 0f);
        accent.color = new Color(0.76f, 0.64f, 0.38f);
        accent.raycastTarget = false;

        _messageText = new GameObject("Message", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        _messageText.transform.SetParent(transform, false);
        _messageText.rectTransform.anchorMin = Vector2.zero;
        _messageText.rectTransform.anchorMax = Vector2.one;
        _messageText.rectTransform.offsetMin = new Vector2(14f, 12f);
        _messageText.rectTransform.offsetMax = new Vector2(-14f, -12f);
        _messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _messageText.fontSize = 18;
        _messageText.color = HudStyle.Text;
        _messageText.alignment = TextAnchor.MiddleLeft;
        _messageText.raycastTarget = false;
    }

    public void Show(string message, Color textColor)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        _lastMessage = message;
        _lastTextColor = textColor;
        if (_isMenuOpen)
        {
            SetMessage(message, textColor);
            _canvasGroup.alpha = 1f;
            return;
        }
        _messages.Enqueue((message, textColor));
        if (_displayMessages == null)
            _displayMessages = StartCoroutine(DisplayMessages());
    }

    public void SetMenuOpen(bool isOpen)
    {
        _isMenuOpen = isOpen;
        if (_displayMessages != null)
        {
            StopCoroutine(_displayMessages);
            _displayMessages = null;
        }
        _messages.Clear();
        _fade?.Kill();
        if (isOpen && !string.IsNullOrEmpty(_lastMessage))
            SetMessage(_lastMessage, _lastTextColor);
        _canvasGroup.alpha = isOpen && !string.IsNullOrEmpty(_lastMessage) ? 1f : 0f;
    }

    private void SetMessage(string message, Color textColor)
    {
        _messageText.text = message;
        _messageText.color = textColor;
        _panel.sizeDelta = new Vector2(290f, Mathf.Max(60f, _messageText.preferredHeight + 24f));
    }

    private IEnumerator DisplayMessages()
    {
        while (_messages.Count > 0)
        {
            var notification = _messages.Dequeue();
            SetMessage(notification.Text, notification.TextColor);
            _fade?.Kill();
            _fade = _canvasGroup.DOFade(1f, 0.2f).SetUpdate(true);
            yield return new WaitForSecondsRealtime(5f);
            _fade = _canvasGroup.DOFade(0f, 0.3f).SetUpdate(true);
            yield return new WaitForSecondsRealtime(0.35f);
        }
        _displayMessages = null;
    }

    private void OnDestroy() => _fade?.Kill();
}
