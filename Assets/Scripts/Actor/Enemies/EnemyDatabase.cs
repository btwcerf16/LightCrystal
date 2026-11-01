using System;
using System.Collections.Generic;
using UnityEngine;

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
