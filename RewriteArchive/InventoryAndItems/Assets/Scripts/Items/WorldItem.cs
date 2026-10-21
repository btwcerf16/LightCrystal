using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(Collider2D))]
public sealed class WorldItem : NetworkBehaviour
{
    private readonly NetworkVariable<bool> _held = new(false);
    private readonly NetworkVariable<Vector2> _worldPosition = new();
    private readonly HashSet<ActorHand> _hands = new();
    private Collider2D _pickupCollider;

    public Item Item { get; private set; }
    public bool IsAvailable => IsSpawned && !_held.Value;

    private void Awake()
    {
        Item = GetComponentInChildren<Item>(true);
        _pickupCollider = GetComponent<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        _held.OnValueChanged += OnHeldChanged;
        _worldPosition.OnValueChanged += OnWorldPositionChanged;

        if (IsServer)
            _worldPosition.Value = transform.position;
        else
            SetWorldPosition(_worldPosition.Value);

        Refresh();
    }

    public override void OnNetworkDespawn()
    {
        _held.OnValueChanged -= OnHeldChanged;
        _worldPosition.OnValueChanged -= OnWorldPositionChanged;
        _hands.Clear();
        Item.SetOwner(null);
    }

    public bool IsTouchedBy(ActorHand hand) => IsAvailable && _hands.Contains(hand);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!CompareTag("Weapon") && !CompareTag("Item"))
            return;

        ActorHand hand = other.GetComponentInParent<ActorHand>();

        if (hand == null)
            return;

        _hands.Add(hand);
        hand.AddPickupItem(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        ActorHand hand = other.GetComponentInParent<ActorHand>();

        if (hand == null)
            return;

        _hands.Remove(hand);
        hand.RemovePickupItem(this);
    }

    public bool TryClaim()
    {
        if (!IsServer || !IsAvailable)
            return false;

        _held.Value = true;
        return true;
    }

    public void Release(Vector2 position)
    {
        if (!IsSpawned || !IsServer)
            return;

        Item.SetOwner(null);
        _worldPosition.Value = position;
        SetWorldPosition(position);
        _held.Value = false;
    }

    private void OnHeldChanged(bool previous, bool current) => Refresh();

    private void OnWorldPositionChanged(Vector2 previous, Vector2 current) => SetWorldPosition(current);

    private void SetWorldPosition(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    private void Refresh()
    {
        _pickupCollider.enabled = !_held.Value;

        if (!_held.Value)
        {
            Item.transform.SetParent(transform, false);
            Item.gameObject.SetActive(true);
        }
    }
}
