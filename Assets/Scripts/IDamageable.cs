using UnityEngine;

/// <summary>
/// Implement this on any enemy or object that can be damaged.
/// The combat system targets this interface exclusively, so it works
/// with any enemy type you build later without modification.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int amount, Element damageElement);
    void ApplyKnockback(Vector2 direction, float force);
    void ApplyStagger();    // Called when the player lands a successful parry
    bool IsAlive { get; }
}