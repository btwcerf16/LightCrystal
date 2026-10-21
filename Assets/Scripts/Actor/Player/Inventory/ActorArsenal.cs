using UnityEngine;

public sealed class ActorArsenal : MonoBehaviour
{
    [SerializeField] private Weapon[] _weapons = new Weapon[1];
    private int _activeWeaponIndex = 0;

    public Weapon GetWeapon(int index) => index >= 0 && index < _weapons.Length ? _weapons[index] : null;

    public int WeaponSize => _weapons.Length;
    public int ActiveWeaponIndex => _activeWeaponIndex;
    public Weapon ActiveWeapon => HasWeapon(_activeWeaponIndex) ? _weapons[_activeWeaponIndex] : null;

    public event System.Action<int, Weapon> ActiveWeaponChanged;

    public bool CanUseWeapons { get; private set; }

    private void Awake()
    {
        CanUseWeapons = ActiveWeapon != null;
        ActiveWeapon?.Equip();
    }

    public bool TrySelectWeapon(int index)
    {
        if (index < 0 || index >= _weapons.Length)
            return false;

        if (index == _activeWeaponIndex)
            return true;

        ActiveWeapon?.Drop();
        _activeWeaponIndex = index;
        ActiveWeapon?.Equip();
        ActiveWeaponChanged?.Invoke(_activeWeaponIndex, ActiveWeapon);
        return true;
    }

    public void SetWeapon(int index, Weapon weapon)
    {
        if (index < 0 || index >= _weapons.Length || _weapons[index] == weapon)
            return;
        if (index == _activeWeaponIndex)
            _weapons[index]?.Drop();
        _weapons[index] = weapon;
        if (index == _activeWeaponIndex)
        {
            weapon?.Equip();
            ActiveWeaponChanged?.Invoke(index, weapon);
        }
    }

    public void EquipWeapon(Weapon weapon)
    {
        if (weapon == null || System.Array.IndexOf(_weapons, weapon) >= 0)
            return;

        for (int i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] == null)
            {
                _weapons[i] = weapon;
                if (i == _activeWeaponIndex)
                {
                    weapon.Equip();
                    ActiveWeaponChanged?.Invoke(_activeWeaponIndex, ActiveWeapon);
                }
                return;
            }
        }
        Debug.Log("Арсенал заполнен");
    }

    public void ChangeWeaponSlot(int previousIndex, int newIndex)
    {
        if (previousIndex < 0 || previousIndex >= _weapons.Length || newIndex < 0 || newIndex >= _weapons.Length)
            return;

        Weapon previousActiveWeapon = ActiveWeapon;
        Weapon weapon = _weapons[newIndex];
        _weapons[newIndex] = _weapons[previousIndex];
        _weapons[previousIndex] = weapon;
        if (previousActiveWeapon != ActiveWeapon)
        {
            previousActiveWeapon?.Drop();
            ActiveWeapon?.Equip();
            ActiveWeaponChanged?.Invoke(_activeWeaponIndex, ActiveWeapon);
        }
    }

    public void DropWeapon(int index)
    {
        if (!HasWeapon(index))
            return;
    }

    private bool HasWeapon(int index)
    {
        return index >= 0 && index < _weapons.Length && _weapons[index] != null;
    }
    public void SetCanUseWeapon(bool canUseWeapon) { CanUseWeapons = canUseWeapon; }
}
