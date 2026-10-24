using UnityEngine;
using System;
public sealed class ActorArsenal : MonoBehaviour
{
    [SerializeField] private Weapon[] _weapons = new Weapon[2];
    private int _activeWeaponIndex = 0;

    public Weapon GetWeapon(int index) => index >= 0 && index < _weapons.Length ? _weapons[index] : null;

    public int WeaponSize => _weapons.Length;
    public int ActiveWeaponIndex => _activeWeaponIndex;
    public Weapon ActiveWeapon => HasWeapon(_activeWeaponIndex) ? _weapons[_activeWeaponIndex] : null;

    public event Action<int, Weapon> ActiveWeaponChanged;
    public event Action ContentsChanged;
    public event Action<int, int> SlotChangeRequested;
    public event Action<int> WeaponSelectionRequested;
    public event Action<int> WeaponDropRequested;

    private bool _canUseWeapons = true;
    public bool CanUseWeapons => _canUseWeapons && ActiveWeapon != null;

    private void Awake()
    {
        for (int i = 0; i < _weapons.Length; i++)
            if (_weapons[i] != null)
                _weapons[i].gameObject.SetActive(i == _activeWeaponIndex);
        ActiveWeapon?.Equip();
    }

    public void RequestDropWeapon(int index)
    {
        WeaponDropRequested?.Invoke(index);
    }

    public int FindEmptySlot()
    {
        return Array.IndexOf(_weapons, null);
    }

    public void RequestChangeWeaponSlot(int previousIndex, int newIndex)
    {
        SlotChangeRequested?.Invoke(previousIndex, newIndex);
    }

    public void RequestSelectWeapon(int index)
    {
        WeaponSelectionRequested?.Invoke(index);
    }

    public bool TrySelectWeapon(int index)
    {
        if (index < 0 || index >= _weapons.Length)
            return false;

        if (index == _activeWeaponIndex)
            return true;

        if (ActiveWeapon != null)
        {
            ActiveWeapon.Drop();
            ActiveWeapon.gameObject.SetActive(false);
        }
        _activeWeaponIndex = index;
        if (ActiveWeapon != null)
            ActiveWeapon.gameObject.SetActive(true);
        ActiveWeapon?.Equip();
        ActiveWeaponChanged?.Invoke(_activeWeaponIndex, ActiveWeapon);
        return true;
    }

    public void SetWeapon(int index, Weapon weapon)
    {
        if (index < 0 || index >= _weapons.Length || _weapons[index] == weapon)
            return;
        if (index == _activeWeaponIndex)
        {
            if (_weapons[index] != null && _weapons[index].transform.IsChildOf(transform))
            {
                _weapons[index].Drop();
                _weapons[index].gameObject.SetActive(false);
            }
        }
        _weapons[index] = weapon;
        if (weapon != null)
            weapon.gameObject.SetActive(index == _activeWeaponIndex);
        if (index == _activeWeaponIndex)
        {
            weapon?.Equip();
            ActiveWeaponChanged?.Invoke(index, weapon);
        }
        ContentsChanged?.Invoke();
    }

    public void SetWeapon(int index, ulong uid)
    {
        if (index < 0 || index >= _weapons.Length)
            return;

        Weapon weapon = null;
        if (uid != 0)
        {
            Weapon[] weapons = GetComponentsInChildren<Weapon>(true);
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i].Uid == uid)
                {
                    weapon = weapons[i];
                    break;
                }
            }

            if (weapon == null)
            {
                Debug.LogError($"Weapon with UID {uid} was not found on this player.", this);
                return;
            }
        }

        SetWeapon(index, weapon);
    }

    public void EquipWeapon(Weapon weapon)
    {
        if (weapon == null || Array.IndexOf(_weapons, weapon) >= 0)
            return;

        for (int i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] == null)
            {
                SetWeapon(i, weapon);
                return;
            }
        }
        Debug.Log("Арсенал заполнен");
    }

    public void ChangeWeaponSlot(int previousIndex, int newIndex)
    {
        if (previousIndex < 0 || previousIndex >= _weapons.Length || newIndex < 0 || newIndex >= _weapons.Length || previousIndex == newIndex)
            return;

        Weapon previousActiveWeapon = ActiveWeapon;
        Weapon weapon = _weapons[newIndex];
        _weapons[newIndex] = _weapons[previousIndex];
        _weapons[previousIndex] = weapon;
        if (previousActiveWeapon != ActiveWeapon)
        {
            if (previousActiveWeapon != null)
            {
                previousActiveWeapon.Drop();
                previousActiveWeapon.gameObject.SetActive(false);
            }
            if (ActiveWeapon != null)
                ActiveWeapon.gameObject.SetActive(true);
            ActiveWeapon?.Equip();
            ActiveWeaponChanged?.Invoke(_activeWeaponIndex, ActiveWeapon);
        }
        ContentsChanged?.Invoke();
    }

    public void DropWeapon(int index)
    {
        if (!HasWeapon(index))
            return;
        SetWeapon(index, (Weapon)null);
    }

    private bool HasWeapon(int index)
    {
        return index >= 0 && index < _weapons.Length && _weapons[index] != null;
    }
    public void SetCanUseWeapon(bool canUseWeapon) { _canUseWeapons = canUseWeapon; }
}
