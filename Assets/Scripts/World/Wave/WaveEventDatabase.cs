using System;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(menuName = "World/Wave Event Database")]
public sealed class WaveEventDatabase : ScriptableObject
{
    [Serializable]
    private struct Entry
    {
        public NetworkObject Prefab;
        [Min(0)] public int WaveWeight;
        [Min(0)] public int StandaloneWeight;

        public int GetWeight(bool isWaveEvent)
        {
            return Prefab != null ? Mathf.Max(0, isWaveEvent ? WaveWeight : StandaloneWeight) : 0;
        }
    }

    [SerializeField] private Entry[] _events = Array.Empty<Entry>();

    public NetworkObject GetRandomPrefab(bool isWaveEvent)
    {
        int totalWeight = 0;
        foreach (Entry entry in _events)
            totalWeight += entry.GetWeight(isWaveEvent);
        if (totalWeight <= 0)
            return null;

        int roll = Random.Range(0, totalWeight);
        foreach (Entry entry in _events)
        {
            roll -= entry.GetWeight(isWaveEvent);
            if (roll < 0)
                return entry.Prefab;
        }
        return null;
    }
}
