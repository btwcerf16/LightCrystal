using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class EnchanterFleeSensor : MonoBehaviour
{
    private readonly HashSet<ActorHealth> _players = new();
    private SkeletonEnchanter _enemy;
    private NetworkObject _networkObject;
    private int _playerLayer;

    private void Awake()
    {
        _enemy = GetComponentInParent<SkeletonEnchanter>();
        _networkObject = _enemy.GetComponent<NetworkObject>();
        _playerLayer = LayerMask.NameToLayer("Player");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_networkObject.IsSpawned || !_networkObject.NetworkManager.IsServer || !_enemy.isActiveAndEnabled)
            return;
        if (other.gameObject.layer != _playerLayer)
            return;

        ActorHealth player = other.GetComponent<ActorHealth>();
        if (player.IsDead || !_players.Add(player))
            return;

        player.Died += RefreshThreat;
        RefreshThreat();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer != _playerLayer)
            return;

        ActorHealth player = other.GetComponent<ActorHealth>();
        if (!_players.Remove(player))
            return;

        player.Died -= RefreshThreat;
        RefreshThreat();
    }

    private void RefreshThreat()
    {
        Transform nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (ActorHealth player in _players)
        {
            if (player == null || player.IsDead || !player.isActiveAndEnabled)
                continue;
            float distance = ((Vector2)(player.transform.position - transform.position)).sqrMagnitude;
            if (distance >= nearestDistance)
                continue;
            nearest = player.transform;
            nearestDistance = distance;
        }
        _enemy.UpdateThreat(nearest);
    }

    private void OnDisable()
    {
        foreach (ActorHealth player in _players)
            if (player != null)
                player.Died -= RefreshThreat;
        _players.Clear();
        if (_enemy != null)
            _enemy.UpdateThreat(null);
    }
}
