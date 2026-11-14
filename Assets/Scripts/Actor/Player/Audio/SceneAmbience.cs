using UnityEngine;

public sealed class SceneAmbience : MonoBehaviour
{
    [SerializeField] private AudioClip _clip;
    [SerializeField, Range(0f, 1f)] private float _volume = 0.1f;
    [SerializeField] private bool _useWindVolume;

    private AudioSource _source;

    private void Awake()
    {
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.clip = _clip;
        _source.loop = true;
        _source.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        if (_useWindVolume)
            LocalPlayerSettings.WindVolumeChanged += ApplyWindVolume;
        ApplyWindVolume(LocalPlayerSettings.WindVolume);
        _source.Play();
    }

    private void OnDisable()
    {
        if (_useWindVolume)
            LocalPlayerSettings.WindVolumeChanged -= ApplyWindVolume;
        _source.Stop();
    }

    private void ApplyWindVolume(float volume)
    {
        _source.volume = _useWindVolume ? _volume * volume : _volume;
    }
}
