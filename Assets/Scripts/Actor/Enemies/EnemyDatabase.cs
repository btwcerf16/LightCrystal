using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(menuName = "Enemies/Enemy Database")]
public sealed class EnemyDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public Enemy Prefab;
        [Min(1)] public int Weight = 1;
    }

    [SerializeField] private Entry[] _enemies = new Entry[0];

    public Entry[] Enemies => _enemies;

    public int GetWeight(Enemy enemy)
    {
        if (enemy == null || !enemy.TryGetComponent(out NetworkObject instance))
            return 0;
        foreach (Entry entry in _enemies)
        {
            if (entry != null && entry.Prefab != null
                && entry.Prefab.TryGetComponent(out NetworkObject prefab)
                && prefab.PrefabIdHash == instance.PrefabIdHash)
                return Mathf.Max(0, entry.Weight);
        }
        return 0;
    }

    private void OnValidate()
    {
        HashSet<Enemy> prefabs = new HashSet<Enemy>();
        foreach (Entry enemy in _enemies)
        {
            if (enemy == null || enemy.Prefab == null)
            {
                Debug.LogError("Enemy database contains an empty entry.", this);
                continue;
            }

            if (enemy.Weight <= 0)
                Debug.LogError($"Enemy {enemy.Prefab.name} must have a positive weight.", this);
            if (!prefabs.Add(enemy.Prefab))
                Debug.LogError($"Enemy {enemy.Prefab.name} is listed more than once.", this);
        }
    }
}
