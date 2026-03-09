using UnityEngine;

// Create via: Right Click > Create > Skills > Skill Data
[CreateAssetMenu(fileName = "NewSkill", menuName = "Skills/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Identity")]
    public string skillName;
    public string description;
    public Sprite icon;

    [Header("SP Cost")]
    // How much SP is required to activate this skill
    [Range(1, 100)]
    public int spCost = 100;

    [Header("Effects")]
    // A skill can have multiple effects that all fire together
    // e.g. an elemental burst that also applies a debuff
    public SkillEffect[] effects;

    // ── Convenience ─────────────────────────────────────────────────────────

    public bool HasEffectOfType(SkillEffectType type)
    {
        if (effects == null) return false;
        foreach (var e in effects)
            if (e.effectType == type) return true;
        return false;
    }
}