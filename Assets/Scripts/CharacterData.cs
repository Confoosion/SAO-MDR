using UnityEngine;

// Create character assets via: Right Click > Create > Characters > Character Data
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Characters/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string characterName;
    public Sprite portrait;                 // Full card art
    public Sprite icon;                     // Small icon for party UI

    [Header("Classification")]
    public Element element;
    public WeaponType weaponType;

    [Header("Rarity")]
    [Range(4, 5)]
    public int rarity = 4;                  // 4 or 5 star, matching early MD

    [Header("Base Stats (at Level 1)")]
    public BaseStats baseStats;

    [Header("Stat Growth")]
    // How much each stat grows per level.
    // A value of 1.05 means stats grow by 5% per level.
    [Range(1f, 1.2f)]
    public float statGrowthRate = 1.05f;

    [Header("Skill")]
    public SkillData skill;

    // Max level scales with rarity, matching MD's system
    public int MaxLevel => rarity == 5 ? 80 : 60;

    /// <summary>
    /// Returns the stats for this character at a given level.
    /// </summary>
    public BaseStats GetStatsAtLevel(int level)
    {
        level = Mathf.Clamp(level, 1, MaxLevel);
        float multiplier = Mathf.Pow(statGrowthRate, level - 1);
        return baseStats.Scale(multiplier);
    }
}