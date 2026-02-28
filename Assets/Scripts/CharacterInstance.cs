using UnityEngine;
using System;

[Serializable]
public class CharacterInstance
{
    // Reference to the shared template
    public CharacterData data;

    [Header("Progression")]
    [SerializeField] private int level = 1;
    [SerializeField] private int currentExp = 0;

    [Header("Equipment")]
    [SerializeField] private WeaponData equippedWeapon;

    [Header("Runtime State")]
    [SerializeField] private int currentHp;
    private bool isAlive = true;

    // Events other systems (UI, combat) can subscribe to
    public event Action<int, int> OnHpChanged;   // (currentHp, maxHp)
    public event Action OnDeath;
    public event Action<WeaponData> OnWeaponChanged;

    // ── Computed Properties ──────────────────────────────────────────────────

    public int Level => level;
    public int CurrentHp => currentHp;
    public bool IsAlive => isAlive;
    public WeaponData EquippedWeapon => equippedWeapon;

    // Base stats from level scaling alone
    public BaseStats Stats => data.GetStatsAtLevel(level);

    // Full stats including equipped weapon bonuses — use this in combat
    public BaseStats TotalStats => equippedWeapon != null
        ? Stats + equippedWeapon.statBonus
        : Stats;

    public int MaxHp => TotalStats.hp;

    // Effective element — weapon overrides character element if it has one
    public Element EffectiveElement => equippedWeapon != null
        ? equippedWeapon.GetEffectiveElement(data.element)
        : data.element;

    // Weapon type data passthrough — null safe
    public WeaponTypeData WeaponTypeData => equippedWeapon?.weaponTypeData;

    // Convenience passthrough to CharacterData
    public string Name => data.characterName;
    public Element BaseElement => data.element;
    public WeaponType WeaponType => data.weaponType;
    public int Rarity => data.rarity;

    // ── Constructor ──────────────────────────────────────────────────────────

    public CharacterInstance(CharacterData data, int level = 1, WeaponData weapon = null)
    {
        this.data = data;
        this.level = Mathf.Clamp(level, 1, data.MaxLevel);
        this.equippedWeapon = weapon;
        FullHeal();
    }

    // ── Equipment ────────────────────────────────────────────────────────────

    public void EquipWeapon(WeaponData weapon)
    {
        equippedWeapon = weapon;
        // Re-clamp HP in case MaxHp changed due to weapon stat bonus
        currentHp = Mathf.Min(currentHp, MaxHp);
        OnWeaponChanged?.Invoke(equippedWeapon);
        OnHpChanged?.Invoke(currentHp, MaxHp);
    }

    public void UnequipWeapon()
    {
        equippedWeapon = null;
        currentHp = Mathf.Min(currentHp, MaxHp);
        OnWeaponChanged?.Invoke(null);
        OnHpChanged?.Invoke(currentHp, MaxHp);
    }

    // ── HP Methods ───────────────────────────────────────────────────────────

    public void FullHeal()
    {
        currentHp = MaxHp;
        isAlive = true;
        OnHpChanged?.Invoke(currentHp, MaxHp);
    }

    public void TakeDamage(int amount)
    {
        if (!isAlive) return;

        currentHp = Mathf.Max(0, currentHp - amount);
        OnHpChanged?.Invoke(currentHp, MaxHp);

        if (currentHp <= 0)
        {
            isAlive = false;
            OnDeath?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (!isAlive) return;

        currentHp = Mathf.Min(MaxHp, currentHp + amount);
        OnHpChanged?.Invoke(currentHp, MaxHp);
    }

    // ── Experience & Leveling ────────────────────────────────────────────────

    public void AddExperience(int amount)
    {
        if (level >= data.MaxLevel) return;

        currentExp += amount;

        while (currentExp >= ExpToNextLevel() && level < data.MaxLevel)
        {
            currentExp -= ExpToNextLevel();
            LevelUp();
        }
    }

    // Simple quadratic EXP curve — easy to tune later
    public int ExpToNextLevel()
    {
        return 100 + (level * level * 10);
    }

    private void LevelUp()
    {
        int oldMaxHp = MaxHp;
        level++;

        // Keep HP proportional after level up (same ratio as before)
        float hpRatio = (float)currentHp / oldMaxHp;
        currentHp = Mathf.RoundToInt(MaxHp * hpRatio);

        OnHpChanged?.Invoke(currentHp, MaxHp);
        Debug.Log($"{Name} leveled up to {level}!");
    }
}