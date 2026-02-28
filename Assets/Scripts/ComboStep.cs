using UnityEngine;
using System;

[Serializable]
public class ComboStep
{
    [Header("Damage")]
    // Multiplier applied to the character's attack stat for this hit
    // e.g. 0.8 for a weak opener, 1.5 for a heavy finisher
    public float damageMultiplier = 1f;

    [Header("Hitbox")]
    public Vector2 hitboxSize = new Vector2(1f, 1f);
    // Offset from the character's position — positive X = in front
    public Vector2 hitboxOffset = new Vector2(1f, 0f);

    [Header("Timing")]
    // How long the hitbox is active (in seconds)
    public float activeTime = 0.15f;
    // Delay before the next combo step can trigger
    public float recoveryTime = 0.2f;
}