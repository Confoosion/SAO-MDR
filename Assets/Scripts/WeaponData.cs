using UnityEngine;

// Create via: Right Click > Create > Weapons > Weapon Data
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponName;
    public Sprite icon;

    [Header("Type")]
    // Reference to the behavior profile for this weapon's type
    public WeaponTypeData weaponTypeData;

    [Header("Rarity")]
    [Range(4, 5)]
    public int rarity = 4;

    [Header("Stat Bonuses")]
    // Flat bonuses added on top of the character's own stats when equipped
    public BaseStats statBonus;

    // ── Convenience Passthrough ──────────────────────────────────────────────

    public WeaponType WeaponType => weaponTypeData != null ? weaponTypeData.weaponType : default;
    public Element Element => weaponTypeData != null ? weaponTypeData.element : Element.None;
    public int ComboLength => weaponTypeData != null ? weaponTypeData.ComboLength : 0;
    public float AttackSpeed => weaponTypeData != null ? weaponTypeData.attackSpeed : 1f;

    /// <summary>
    /// Returns the effective element, falling back to the character's element
    /// if neither the weapon nor its type has one assigned.
    /// </summary>
    public Element GetEffectiveElement(Element characterElement)
    {
        if (weaponTypeData == null) return characterElement;
        return weaponTypeData.GetEffectiveElement(characterElement);
    }
}