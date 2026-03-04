using UnityEngine;

/// <summary>
/// Attach this to any GameObject that owns a CharacterInstance
/// (enemies, allies, etc.) so the combat system can read their stats
/// without needing a concrete class reference.
/// </summary>
public class CharacterInstanceHolder : MonoBehaviour
{
    public CharacterInstance Character { get; private set; }

    public void Init(CharacterInstance character)
    {
        Character = character;
    }
}