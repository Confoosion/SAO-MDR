using UnityEngine;

// Create via: Right Click > Create > Weapons > Weapon Type Data
[CreateAssetMenu(fileName = "NewWeaponType", menuName = "Weapons/Weapon Type Data")]
public class WeaponTypeData : ScriptableObject
{
    [Header("Identity")]
    public WeaponType weaponType;

    [Header("Element")]
    // None means the weapon defers to the character's element
    public Element element = Element.None;

    [Header("Attack Speed")]
    // Time in seconds between the start of one full combo and the next
    public float attackSpeed = 1f;

    [Header("Combo")]
    // Each entry defines one hit in the combo chain
    // e.g. Rapier might have 5 steps, GreatSword might have 2
    public ComboStep[] comboSteps;

    // How long the player has after a combo step to input the next one
    // before the combo resets
    public float comboWindowTime = 0.4f;

    // ── Convenience Properties ───────────────────────────────────────────────

    public int ComboLength => comboSteps != null ? comboSteps.Length : 0;

    /// <summary>
    /// Returns the combo step at the given index, or null if out of range.
    /// </summary>
    public ComboStep GetComboStep(int index)
    {
        if (comboSteps == null || index < 0 || index >= comboSteps.Length)
            return null;

        return comboSteps[index];
    }

    /// <summary>
    /// Returns the effective element for this weapon — falls back to the
    /// character's element if the weapon has no element of its own.
    /// </summary>
    public Element GetEffectiveElement(Element characterElement)
    {
        return element != Element.None ? element : characterElement;
    }
}