using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class NetworkEffects : NetworkBehaviour
{
    [Serializable]
    private struct Preset
    {
        public string Id;
        public GameObject Prefab;
        public float Duration;
        public int PathLineCount;
        public float WidthAtEnd;
        public Vector2 DetailOpacityRange;
    }

    private struct Playback
    {
        public Transform Root;
        public ObjectPool<Transform> Pool;
        public ParticleSystem Particles;
        public LineRenderer[] Lines;
        public LineRenderer[] Templates;
        public Preset Settings;
        public float EndsAt;
    }

    [SerializeField] private Preset[] _presets = Array.Empty<Preset>();
    private readonly Dictionary<string, int> _presetIndices = new();
    private readonly Dictionary<int, ObjectPool<Transform>> _pools = new();
    private readonly Dictionary<int, LineRenderer[]> _lineTemplates = new();
    private readonly List<Playback> _playing = new();

    private void Awake()
    {
        for (int i = 0; i < _presets.Length; i++)
            if (!string.IsNullOrEmpty(_presets[i].Id) && _presets[i].Prefab != null)
                _presetIndices.TryAdd(_presets[i].Id, i);
    }

    public static void Play(NetworkManager manager, string effectId, Vector3 position, float scale = 1f,
        Vector2[] path = null, Vector2[] detail = null)
    {
        if (string.IsNullOrEmpty(effectId) || manager == null || !manager.IsListening || !manager.IsServer)
            return;

        path ??= Array.Empty<Vector2>();
        detail ??= Array.Empty<Vector2>();
        foreach (NetworkClient client in manager.ConnectedClientsList)
        {
            NetworkObject player = client.PlayerObject;
            if (player != null && player.IsSpawned && player.TryGetComponent(out NetworkEffects effects))
                effects.PlayEffectRpc(effectId, position, scale, path, detail);
        }
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void PlayEffectRpc(string effectId, Vector3 position, float scale, Vector2[] path, Vector2[] detail)
    {
        if (!_presetIndices.TryGetValue(effectId, out int index))
            return;

        Preset settings = _presets[index];
        if (!_pools.TryGetValue(index, out ObjectPool<Transform> pool))
        {
            pool = new ObjectPool<Transform>(() => Instantiate(settings.Prefab).transform, 32);
            _pools.Add(index, pool);
            _lineTemplates.Add(index, settings.Prefab.GetComponentsInChildren<LineRenderer>(true));
        }

        Transform root = pool.GetObject();
        root.position = position;
        root.localScale = Vector3.one * scale;
        var playback = new Playback
        {
            Root = root,
            Pool = pool,
            Particles = root.GetComponent<ParticleSystem>(),
            Lines = root.GetComponentsInChildren<LineRenderer>(true),
            Templates = _lineTemplates[index],
            Settings = settings,
            EndsAt = Time.time + settings.Duration
        };

        for (int i = 0; i < playback.Lines.Length; i++)
        {
            LineRenderer line = playback.Lines[i];
            Vector2[] points = i < settings.PathLineCount ? path : detail;
            line.positionCount = points.Length;
            for (int j = 0; j < points.Length; j++)
                line.SetPosition(j, points[j]);
        }
        Paint(playback, 1f);
        if (playback.Particles != null)
        {
            playback.Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            playback.Particles.Play(true);
        }
        _playing.Add(playback);
    }

    private void Update()
    {
        for (int i = _playing.Count - 1; i >= 0; i--)
        {
            Playback playback = _playing[i];
            if (playback.Root == null)
            {
                _playing.RemoveAt(i);
                continue;
            }

            bool waitingForParticles = playback.Particles != null && playback.Particles.IsAlive(true);
            float remaining = playback.EndsAt - Time.time;
            if (remaining <= 0f && !waitingForParticles)
            {
                playback.Pool.ReturnObject(playback.Root);
                _playing.RemoveAt(i);
            }
            else if (playback.Settings.Duration > 0f)
            {
                Paint(playback, Mathf.Clamp01(remaining / playback.Settings.Duration));
            }
        }
    }

    private static void Paint(Playback playback, float fraction)
    {
        Preset settings = playback.Settings;
        for (int i = 0; i < playback.Lines.Length; i++)
        {
            LineRenderer template = playback.Templates[i];
            LineRenderer line = playback.Lines[i];
            float opacity = i < settings.PathLineCount
                ? fraction
                : Mathf.InverseLerp(settings.DetailOpacityRange.x, settings.DetailOpacityRange.y, fraction);
            line.widthMultiplier = template.widthMultiplier * Mathf.Lerp(settings.WidthAtEnd, 1f, opacity);
            Color start = template.startColor;
            Color end = template.endColor;
            start.a *= opacity;
            end.a *= opacity;
            line.startColor = start;
            line.endColor = end;
            line.enabled = line.positionCount >= 2 && opacity > 0f;
        }
    }

    public override void OnNetworkDespawn()
    {
        foreach (Playback playback in _playing)
            if (playback.Root != null)
                Destroy(playback.Root.gameObject);
        _playing.Clear();
        foreach (ObjectPool<Transform> pool in _pools.Values)
            pool.Clear();
        _pools.Clear();
        _lineTemplates.Clear();
    }
}
