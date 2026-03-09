using UnityEngine;
using System;

public enum SkillEffectType
{
    DamageSingle,
    DamageAoE,
    Heal,
    BuffStat,
    DebuffStat,
    ElementalBurst
}

// Which stat a buff or debuff targets
public enum StatType
{
    HP,
    Attack,
    Defense,
    Speed
}

[Serializable]
public class SkillEffect
{
    [Header("Type")]
    public SkillEffectType effectType;

    [Header("Damage / Heal")]
    // Multiplier against the caster's attack stat for damage,
    // or against max HP for heals
    public float powerMultiplier = 1f;

    [Header("Hitbox (Damage / Burst)")]
    public Vector2 hitboxSize = new Vector2(2f, 2f);
    public Vector2 hitboxOffset = new Vector2(1f, 0f);

    [Header("Buff / Debuff")]
    public StatType targetStat;
    // Flat value added (positive = buff, handled by sign on DebuffStat)
    public int statModifierAmount;
    // How long the buff/debuff lasts in seconds
    public float duration = 5f;

    [Header("Elemental Burst")]
    // Burst always uses the character's effective element,
    // but you can override the radius here
    public float burstRadius = 4f;
}