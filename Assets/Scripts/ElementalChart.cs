/// <summary>
/// Defines elemental matchup multipliers.
/// Strong matchup = 1.5x, Weak matchup = 0.75x, Neutral = 1.0x.
/// None element always returns neutral.
/// </summary>
public static class ElementalChart
{
    // Strong matchups: attacker element -> defender element it hits hard
    // Fire > Wind > Earth > Water > Fire (cycle)
    // Light > Dark, Dark > Light (binary)
    private static readonly (Element attacker, Element defender)[] strongMatchups =
    {
        (Element.Fire,  Element.Wind),
        (Element.Wind,  Element.Earth),
        (Element.Earth, Element.Water),
        (Element.Water, Element.Fire),
        (Element.Light, Element.Dark),
        (Element.Dark,  Element.Light),
    };

    // Weak matchups are the reverse of strong ones
    private static readonly (Element attacker, Element defender)[] weakMatchups =
    {
        (Element.Wind,  Element.Fire),
        (Element.Earth, Element.Wind),
        (Element.Water, Element.Earth),
        (Element.Fire,  Element.Water),
        (Element.Dark,  Element.Light),
        (Element.Light, Element.Dark),
    };

    public const float StrongMultiplier  = 1.5f;
    public const float NeutralMultiplier = 1.0f;
    public const float WeakMultiplier    = 0.75f;

    /// <summary>
    /// Returns the damage multiplier for an attack of attackerElement
    /// landing on a target of defenderElement.
    /// </summary>
    public static float GetMultiplier(Element attackerElement, Element defenderElement)
    {
        // None element is always neutral
        if (attackerElement == Element.None || defenderElement == Element.None)
            return NeutralMultiplier;

        // Same element — neutral
        if (attackerElement == defenderElement)
            return NeutralMultiplier;

        foreach (var matchup in strongMatchups)
        {
            if (matchup.attacker == attackerElement && matchup.defender == defenderElement)
                return StrongMultiplier;
        }

        foreach (var matchup in weakMatchups)
        {
            if (matchup.attacker == attackerElement && matchup.defender == defenderElement)
                return WeakMultiplier;
        }

        return NeutralMultiplier;
    }

    /// <summary>
    /// Convenience method returning whether an element hits strongly
    /// against another.
    /// </summary>
    public static bool IsStrong(Element attacker, Element defender)
        => GetMultiplier(attacker, defender) == StrongMultiplier;
}