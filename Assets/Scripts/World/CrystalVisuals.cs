using Unity.Netcode;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Rendering.Universal;

public sealed class CrystalVisuals : MonoBehaviour
{
    [SerializeField] private Transform _visual;
    [SerializeField] private LineRenderer[] _cracks;
    [SerializeField] private float[] _crackHealth = { 450f, 400f, 300f, 200f, 100f, 50f };
    [SerializeField, Min(0f)] private float _floatHeight = 0.12f;
    [SerializeField, Min(0.1f)] private float _floatPeriod = 3f;
    [SerializeField] private ParticleSystem _sparkles;
    private Light2D _light;
    private float _baseIntensity;
    private Tween _breathing;
    private Vector3 _position;
    private Tween _floating;

    private void Awake()
    {
        _position = _visual.localPosition;
        _light = GetComponent<Light2D>();
        _baseIntensity = _light.intensity;
    }

    private void OnEnable()
    {
        _visual.localPosition = _position - Vector3.up * _floatHeight;
        _floating = _visual.DOLocalMoveY(_position.y + _floatHeight, _floatPeriod * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
        _light.intensity = _baseIntensity * 0.88f;
        _breathing = DOTween.To(() => _light.intensity, value => _light.intensity = value,
            _baseIntensity * 1.08f, 2.1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        SyncFloating();
    }

    private void OnDisable()
    {
        _floating?.Kill();
        _floating = null;
        _breathing?.Kill();
        _breathing = null;
        if (_light != null)
            _light.intensity = _baseIntensity;
    }

    private void SyncFloating()
    {
        double time = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening
            ? NetworkManager.Singleton.ServerTime.Time : Time.timeAsDouble;
        _floating?.Goto((float)((time + _floatPeriod * 0.25f) % _floatPeriod), true);
        _breathing?.Goto((float)(time % 4.2), true);
    }

    public void SetHealth(float current, float maximum)
    {
        for (int i = 0; i < _cracks.Length; i++)
            _cracks[i].gameObject.SetActive(i < _crackHealth.Length && current <= _crackHealth[i]);
        if (current <= 0f)
        {
            _floating?.Pause();
            _breathing?.Pause();
            if (_sparkles != null)
                _sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        else
        {
            SyncFloating();
            if (_sparkles != null && !_sparkles.isPlaying)
                _sparkles.Play();
        }
    }
}
