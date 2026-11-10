using DG.Tweening;
using UnityEngine;

public sealed class RuneVisuals : MonoBehaviour
{
    [SerializeField] private Transform _visual;
    [SerializeField] private ParticleSystem _breakEffectPrefab;
    [SerializeField] private float _floatHeight = 0.1f;
    [SerializeField] private float _floatDuration = 1.4f;
    private Vector3 _startingPosition;
    private Tween _levitation;

    private void Awake() => _startingPosition = _visual.localPosition;

    private void OnEnable()
    {
        _visual.localPosition = _startingPosition - Vector3.up * _floatHeight;
        _levitation = _visual.DOLocalMoveY(_startingPosition.y + _floatHeight, _floatDuration).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDisable() => _levitation?.Kill();

    public void PlayBreakEffect()
    {
        ParticleSystem particles = Instantiate(_breakEffectPrefab, _visual.position, Quaternion.identity);
        particles.Play(true);
    }
}
