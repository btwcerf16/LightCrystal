using System;
using System.Collections;
using UnityEngine;

public sealed class WorldAudio : MonoBehaviour
{
    [Serializable]
    private struct Sound
    {
        public AudioClip Clip;
        [Min(0f)] public float Radius;
        [Range(0f, 1f)] public float Volume;
    }

    [SerializeField] private Sound[] _sounds = Array.Empty<Sound>();

    private ObjectPool<AudioSource> _sources;

    private void Awake()
    {
        _sources = new ObjectPool<AudioSource>(CreateSource);
    }

    public void Play(int soundId, Vector3 position)
    {
        if (!isActiveAndEnabled || soundId < 0 || soundId >= _sounds.Length)
            return;

        Sound sound = _sounds[soundId];
        if (sound.Clip == null || sound.Radius <= 0f || sound.Volume <= 0f)
            return;

        float distance = Vector2.Distance(transform.position, position);
        if (distance >= sound.Radius)
            return;

        AudioSource source = _sources.GetObject();
        source.clip = sound.Clip;
        source.volume = sound.Volume * (1f - distance / sound.Radius);
        source.Play();
        StartCoroutine(ReturnWhenFinished(source));
    }

    private AudioSource CreateSource()
    {
        var source = new GameObject("World Sound").AddComponent<AudioSource>();
        source.transform.SetParent(transform, false);
        source.playOnAwake = false;
        return source;
    }

    private IEnumerator ReturnWhenFinished(AudioSource source)
    {
        yield return new WaitUntil(() => !source.isPlaying);
        source.clip = null;
        _sources.ReturnObject(source);
    }
}
