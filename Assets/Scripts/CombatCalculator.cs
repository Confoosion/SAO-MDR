using UnityEngine;

/// <summary>
/// Handles all damage calculation. Kept static so any system can call it
/// without needing a component reference.
/// </summary>
public static class CombatCalculator
{
    // Flat defense reduction — how much of the defender's DEF stat
    // reduces incoming damage. Tune this to feel right for your game.
    private const float DefenseScaling = 0.5f;

    /// <summary>
    /// Calculates final damage dealt by an attacker's combo step
    /// against a defender's stats and element.
    /// 
    /// Formula: (ATK * comboMultiplier - DEF * defenseScaling) * elementalMultiplier
    /// Minimum 1 damage always guaranteed.
    /// </summary>
    public static int CalculateDamage(
        BaseStats attackerStats,
        Element attackerElement,
        ComboStep comboStep,
        BaseStats defenderStats,
        Element defenderElement)
    {
        float baseDamage = attackerStats.attack * comboStep.damageMultiplier;
        float reduction  = defenderStats.defense * DefenseScaling;
        float elemental  = ElementalChart.GetMultiplier(attackerElement, defenderElement);

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt((baseDamage - reduction) * elemental));
        return finalDamage;
    }

    /// <summary>
    /// Calculates knockback force for a hit.
    /// Heavier weapons (higher base attack) knock back further.
    /// </summary>
    public static float CalculateKnockback(BaseStats attackerStats, ComboStep comboStep)
    {
        return (attackerStats.attack * comboStep.damageMultiplier) * 0.05f;
    }
}