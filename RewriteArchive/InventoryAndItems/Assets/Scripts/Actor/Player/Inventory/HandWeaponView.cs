using UnityEngine;

[RequireComponent(typeof(ActorArsenal))]
public sealed class HandWeaponView : MonoBehaviour
{
    private ActorArsenal _arsenal;
    private SpriteRenderer _renderer;
    private Sprite _emptyHand;
    private Quaternion _restRotation;
    private ActorHand _hand;
    private Weapon _shownWeapon;

    private void Awake()
    {
        _arsenal = GetComponent<ActorArsenal>();
        _hand = GetComponentInChildren<ActorHand>();
        _renderer = _hand.VisualRenderer;
        _emptyHand = _renderer.sprite;
        _restRotation = _renderer.transform.localRotation;
    }

    private void OnEnable()
    {
        _arsenal.ActiveWeaponChanged += Show;
        Show(_arsenal.ActiveWeaponIndex, _arsenal.ActiveWeapon);
    }

    private void OnDisable()
    {
        _arsenal.ActiveWeaponChanged -= Show;
        _renderer.sprite = _emptyHand;
        _renderer.transform.localRotation = _restRotation;
    }

    private void Show(int index, Weapon weapon)
    {
        if (_shownWeapon != null && _shownWeapon != weapon && _shownWeapon.Owner != null)
            _shownWeapon.gameObject.SetActive(false);

        _shownWeapon = weapon;
        _renderer.enabled = weapon == null;

        if (weapon == null)
        {
            _renderer.sprite = _emptyHand;
            return;
        }

        weapon.gameObject.SetActive(true);
        weapon.transform.SetParent(_hand.HandTransform, false);
        weapon.transform.localPosition = Vector3.zero;
    }
}
