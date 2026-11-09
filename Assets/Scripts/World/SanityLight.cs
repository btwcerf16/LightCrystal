using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class SanityLight : MonoBehaviour
{
    private readonly Dictionary<ActorSanity, int> _players = new();

    private void OnTriggerEnter2D(Collider2D target)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;
        ActorSanity sanity = target.GetComponentInParent<ActorSanity>();
        if (sanity == null)
            return;
        if (_players.TryGetValue(sanity, out int contacts))
            _players[sanity] = contacts + 1;
        else
        {
            _players.Add(sanity, 1);
            sanity.EnterLight();
        }
    }

    private void OnTriggerExit2D(Collider2D target)
    {
        ActorSanity sanity = target.GetComponentInParent<ActorSanity>();
        if (sanity == null || !_players.TryGetValue(sanity, out int contacts))
            return;
        if (contacts > 1)
            _players[sanity] = contacts - 1;
        else
        {
            _players.Remove(sanity);
            sanity.ExitLight();
        }
    }

    private void OnDisable()
    {
        foreach (ActorSanity sanity in _players.Keys)
            if (sanity != null)
                sanity.ExitLight();
        _players.Clear();
    }
}
