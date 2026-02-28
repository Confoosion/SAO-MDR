using UnityEngine;
using System;

[Serializable]
public struct BaseStats
{
    public int hp;
    public int attack;
    public int defense;
    public int speed;

    public BaseStats(int hp, int attack, int defense, int speed)
    {
        this.hp = hp;
        this.attack = attack;
        this.defense = defense;
        this.speed = speed;
    }

    // Adds two BaseStats together — useful for applying equipment or buff offsets
    public static BaseStats operator +(BaseStats a, BaseStats b)
    {
        return new BaseStats(
            a.hp + b.hp,
            a.attack + b.attack,
            a.defense + b.defense,
            a.speed + b.speed
        );
    }

    // Scale all stats by a float multiplier — useful for rarity bonuses
    public BaseStats Scale(float multiplier)
    {
        return new BaseStats(
            Mathf.RoundToInt(hp * multiplier),
            Mathf.RoundToInt(attack * multiplier),
            Mathf.RoundToInt(defense * multiplier),
            Mathf.RoundToInt(speed * multiplier)
        );
    }
}