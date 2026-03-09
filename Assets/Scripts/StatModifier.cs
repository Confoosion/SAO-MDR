using System;

/// <summary>
/// Represents one active buff or debuff on a character.
/// Multiple modifiers on the same stat stack additively.
/// </summary>
[Serializable]
public class StatModifier
{
    public StatType targetStat;
    public int amount;          // Positive = buff, negative = debuff
    public float duration;      // Remaining time in seconds
    public string sourceName;   // Which skill applied this (for UI / debug)

    public StatModifier(StatType stat, int amount, float duration, string sourceName = "")
    {
        this.targetStat = stat;
        this.amount     = amount;
        this.duration   = duration;
        this.sourceName = sourceName;
    }

    /// <summary>
    /// Ticks the timer down. Returns true when the modifier has expired.
    /// </summary>
    public bool Tick(float deltaTime)
    {
        duration -= deltaTime;
        return duration <= 0f;
    }
}