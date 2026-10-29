using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class PlayerItemTooltipUI : NetworkBehaviour
{
    private readonly List<RaycastResult> _uiHits = new();
    private readonly List<Collider2D> _worldHits = new();
    private HoverTooltipUI _tooltip;
    private PointerEventData _pointerData;
    private PlayerInputReader _input;
    private Camera _camera;

    public override void OnNetworkSpawn() => Bind();
    public override void OnGainedOwnership() => Bind();
    public override void OnLostOwnership() => Unbind();
    public override void OnNetworkDespawn() => Unbind();
    private void OnEnable() => Bind();
    private void OnDisable() => Unbind();

    private void Bind()
    {
        if (!IsSpawned || !IsClient || !IsOwner || !isActiveAndEnabled || _tooltip != null)
            return;
        _input = GetComponent<PlayerInputReader>();
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        _pointerData = new PointerEventData(EventSystem.current);
        _tooltip = HoverTooltipUI.Create(transform, "ItemTooltipCanvas", "ItemTooltip");
    }

    private void LateUpdate()
    {
        if (_tooltip == null)
            return;
        if (Pointer.current == null || Pointer.current.press.isPressed || (_input != null && _input.IsMenuOpen))
        {
            _tooltip.Hide();
            return;
        }
        Vector2 pointer = Pointer.current.position.ReadValue();
        ITooltipSource source = GetHoveredSource(pointer);
        if (source == null)
        {
            _tooltip.Hide();
            return;
        }
        if (source is Item item)
            _tooltip.SetRarity(item.Rarity);
        else
            _tooltip.SetBorderColor(new Color(0.3f, 0.75f, 0.85f));
        _tooltip.Show(source, source.TooltipTitle, source.TooltipDescription, pointer, source.TooltipDelay);
    }

    private ITooltipSource GetHoveredSource(Vector2 pointer)
    {
        if (EventSystem.current != null)
        {
            _pointerData.position = pointer;
            _uiHits.Clear();
            EventSystem.current.RaycastAll(_pointerData, _uiHits);
            foreach (RaycastResult hit in _uiHits)
            {
                if (!(hit.module is GraphicRaycaster))
                    continue;
                ItemSlotUI slot = hit.gameObject.GetComponentInParent<ItemSlotUI>();
                return slot != null ? slot.CurrentItem : null;
            }
        }
        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null)
            return null;
        Ray ray = _camera.ScreenPointToRay(pointer);
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        if (!plane.Raycast(ray, out float distance))
            return null;
        _worldHits.Clear();
        Physics2D.OverlapPoint(ray.GetPoint(distance), ContactFilter2D.noFilter, _worldHits);
        foreach (Collider2D hit in _worldHits)
        {
            WorldItem worldItem = hit.GetComponentInParent<WorldItem>();
            if (worldItem != null && worldItem.CanBePicked)
                return worldItem.ItemPrefab;
            ITooltipSource source = hit.GetComponentInParent<ITooltipSource>();
            if (source != null)
                return source;
        }
        return null;
    }

    private void Unbind()
    {
        if (_tooltip == null)
            return;
        _tooltip.gameObject.SetActive(false);
        Destroy(_tooltip.gameObject);
        _tooltip = null;
    }
}
