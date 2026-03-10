using UnityEngine;

/// <summary>
/// Bootstraps the player's active character at the start of a scene.
/// Attach this to your Player GameObject alongside PlayerMovement,
/// PlayerCombat, and SkillExecutor.
/// 
/// In the Inspector, drag in the CharacterData and WeaponData assets
/// for whichever character is active in this scene.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [Header("Active Character")]
    [SerializeField] private CharacterData characterData;
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private int startingLevel = 1;

    [Header("References")]
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private SkillExecutor skillExecutor;

    public CharacterInstance ActiveCharacter { get; private set; }

    void Awake()
    {
        if (characterData == null)
        {
            Debug.LogError("PlayerManager: No CharacterData assigned!");
            return;
        }

        // Create the runtime instance from the ScriptableObject assets
        ActiveCharacter = new CharacterInstance(characterData, startingLevel, weaponData);

        // Hand it off to the combat systems
        playerCombat?.SetCharacter(ActiveCharacter);
        skillExecutor?.SetCharacter(ActiveCharacter);

        Debug.Log($"Player ready: {ActiveCharacter.Name} | " +
                  $"Lv.{ActiveCharacter.Level} | " +
                  $"{ActiveCharacter.EffectiveElement} | " +
                  $"HP: {ActiveCharacter.MaxHp} | " +
                  $"ATK: {ActiveCharacter.TotalStats.attack}");
    }

    void Update()
    {
        // Tick buff/debuff timers every frame
        ActiveCharacter?.TickModifiers(Time.deltaTime);
    }
}