using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class EnchantRune : MonoBehaviour, IDamageable
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private EnchantData _enchant;
    private NetworkObject _networkObject;
    public Sprite Sprite => _spriteRenderer.sprite;
    public EnchantData Enchant => _enchant;
    public float CurrentHealth => IsDead ? 0f : 1f;
    public float MaxHealth => 1f;
    public bool IsDead { get; private set; }
    public event Action<float, float> HealthChanged;
    public event Action Died;
    private bool IsServer => _networkObject != null && _networkObject.IsSpawned && _networkObject.NetworkManager.IsServer;

    public void Initialize(Sprite runeSprite, EnchantData enchant)
    {
        _spriteRenderer.sprite = runeSprite;
        _enchant = enchant;
        _networkObject = GetComponentInParent<NetworkObject>();
    }

    public float TakeDamage(float amount, EDamageType damageType, ActorStats attacker = null)
    {
        if (!IsServer || IsDead || amount <= 0f || attacker == null || attacker.GetComponent<PlayerInputReader>() == null)
            return 0f;
        ActorArsenal arsenal = attacker.GetComponent<ActorArsenal>();
        Weapon weapon = arsenal != null ? arsenal.ActiveWeapon : null;
        if (weapon == null)
        {
            attacker.GetComponent<NetworkPlayerWallet>()?.ShowHint("Возьмите оружие в руки");
            return 0f;
        }
        if (!weapon.AddEnchant(_enchant))
        {
            string message = weapon.GetEnchantState().GetId(weapon.EnchantCapacity - 1) != 0 ? "На оружии нет места для зачарования" : "Это зачарование нельзя повторить";
            attacker.GetComponent<NetworkPlayerWallet>()?.ShowHint(message);
            return 0f;
        }
        Kill();
        return 1f;
    }

    public float Heal(float amount) => 0f;

    public void Kill()
    {
        if (!IsServer || IsDead)
            return;
        IsDead = true;
        HealthChanged?.Invoke(0f, MaxHealth);
        Died?.Invoke();
    }

    public void SetBroken(bool broken)
    {
        IsDead = broken;
        gameObject.SetActive(!broken);
    }
}
