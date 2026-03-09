using UnityEngine;
using System;
using System.Collections.Generic;

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

    [Header("SP")]
    private SPGauge spGauge;

    // Active buffs and debuffs — tick down over time
    private List<StatModifier> activeModifiers = new List<StatModifier>();

    // Events other systems (UI, combat) can subscribe to
    public event Action<int, int> OnHpChanged;          // (currentHp, maxHp)
    public event Action OnDeath;
    public event Action<WeaponData> OnWeaponChanged;

    // ── Computed Properties ──────────────────────────────────────────────────

    public int Level => level;
    public int CurrentHp => currentHp;
    public bool IsAlive => isAlive;
    public WeaponData EquippedWeapon => equippedWeapon;
    public SPGauge SPGauge => spGauge;
    public IReadOnlyList<StatModifier> ActiveModifiers => activeModifiers;

    // Base stats from level scaling alone
    public BaseStats Stats => data.GetStatsAtLevel(level);

    // Stats including weapon bonuses
    public BaseStats WeaponStats => equippedWeapon != null
        ? Stats + equippedWeapon.statBonus
        : Stats;

    // Full stats including all active buffs/debuffs — use this in combat
    public BaseStats TotalStats
    {
        get
        {
            BaseStats total = WeaponStats;
            foreach (var mod in activeModifiers)
            {
                switch (mod.targetStat)
                {
                    case StatType.HP:       total.hp       += mod.amount; break;
                    case StatType.Attack:   total.attack   += mod.amount; break;
                    case StatType.Defense:  total.defense  += mod.amount; break;
                    case StatType.Speed:    total.speed    += mod.amount; break;
                }
            }
            // Clamp to prevent negatives from debuffs
            total.hp       = Mathf.Max(1,  total.hp);
            total.attack   = Mathf.Max(0,  total.attack);
            total.defense  = Mathf.Max(0,  total.defense);
            total.speed    = Mathf.Max(0,  total.speed);
            return total;
        }
    }

    public int MaxHp => TotalStats.hp;

    // Effective element — weapon overrides character element if it has one
    public Element EffectiveElement => equippedWeapon != null
        ? equippedWeapon.GetEffectiveElement(data.element)
        : data.element;

    // Weapon type data passthrough — null safe
    public WeaponTypeData WeaponTypeData => equippedWeapon?.weaponTypeData;

    // Convenience passthrough to CharacterData
    public string Name          => data.characterName;
    public Element BaseElement  => data.element;
    public WeaponType WeaponType => data.weaponType;
    public int Rarity           => data.rarity;
    public SkillData Skill      => data.skill;

    // ── Constructor ──────────────────────────────────────────────────────────

    public CharacterInstance(CharacterData data, int level = 1, WeaponData weapon = null)
    {
        this.data           = data;
        this.level          = Mathf.Clamp(level, 1, data.MaxLevel);
        this.equippedWeapon = weapon;
        this.spGauge        = new SPGauge();
        this.activeModifiers = new List<StatModifier>();
        FullHeal();
    }

    // ── Modifier Tick (call from a MonoBehaviour Update) ─────────────────────

    /// <summary>
    /// Ticks all active stat modifiers. Call this every frame from a
    /// MonoBehaviour that owns this CharacterInstance.
    /// </summary>
    public void TickModifiers(float deltaTime)
    {
        activeModifiers.RemoveAll(mod => mod.Tick(deltaTime));
    }

    // ── Modifier API ─────────────────────────────────────────────────────────

    public void AddModifier(StatModifier modifier)
    {
        activeModifiers.Add(modifier);
        // If an HP buff was added, reflect that in current HP immediately
        if (modifier.targetStat == StatType.HP && modifier.amount > 0)
        {
            currentHp = Mathf.Min(currentHp + modifier.amount, MaxHp);
            OnHpChanged?.Invoke(currentHp, MaxHp);
        }
    }

    public void ClearModifiers()
    {
        activeModifiers.Clear();
    }

    // ── Equipment ────────────────────────────────────────────────────────────

    public void EquipWeapon(WeaponData weapon)
    {
        equippedWeapon = weapon;
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

    public int ExpToNextLevel()
    {
        return 100 + (level * level * 10);
    }

    private void LevelUp()
    {
        int oldMaxHp = MaxHp;
        level++;

        float hpRatio = (float)currentHp / oldMaxHp;
        currentHp = Mathf.RoundToInt(MaxHp * hpRatio);

        OnHpChanged?.Invoke(currentHp, MaxHp);
        Debug.Log($"{Name} leveled up to {level}!");
    }
}