using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages all player combat: tap-to-attack, combo window timing,
/// hitbox detection, damage/knockback application, and parry counter window.
/// Attach alongside PlayerMovement on the player GameObject.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Parry Counter")]
    // How long the counter-attack window lasts after a successful parry
    [SerializeField] private float parryCounterWindow = 1.5f;
    // Damage multiplier applied to the first attack during a counter window
    [SerializeField] private float parryCounterMultiplier = 2.0f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 5f;

    [Header("Debug")]
    [SerializeField] private bool showHitboxGizmos = true;

    // ── Runtime State ────────────────────────────────────────────────────────

    private CharacterInstance character;

    private int currentComboStep = 0;
    private bool isAttacking = false;
    private bool inComboWindow = false;
    private bool inParryCounter = false;

    private Coroutine comboWindowCoroutine;
    private Coroutine attackCoroutine;

    // Gizmo data for debug drawing
    private Vector2 lastHitboxSize;
    private Vector2 lastHitboxPos;
    private bool drawHitbox = false;

    // ── Setup ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this to bind a CharacterInstance to this combat component.
    /// </summary>
    public void SetCharacter(CharacterInstance characterInstance)
    {
        character = characterInstance;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Called by PlayerMovement (or input system) when a tap is detected
    /// and movement isn't consuming it.
    /// </summary>
    public void OnTapAttack()
    {
        if (character == null || character.WeaponTypeData == null) return;
        if (playerMovement.CurrentAction == Movement.Jumping) return;
        if (playerMovement.CurrentAction == Movement.Dashing) return;
        if (isAttacking) return;

        // If in parry counter window, next attack gets a bonus
        bool isCounterAttack = inParryCounter;
        if (inParryCounter)
        {
            inParryCounter = false;
            StopCoroutine(nameof(ParryCounterWindowCoroutine));
        }

        attackCoroutine = StartCoroutine(ExecuteComboStep(currentComboStep, isCounterAttack));
    }

    /// <summary>
    /// Called by PlayerMovement when a successful parry is detected.
    /// Staggers all nearby enemies and opens the counter window.
    /// </summary>
    public void OnParrySuccess()
    {
        // Stagger all enemies in a generous radius around the player
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 3f, enemyLayer);
        foreach (var hit in hits)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            target?.ApplyStagger();
        }

        // Open counter window
        inParryCounter = true;
        currentComboStep = 0;
        StartCoroutine(nameof(ParryCounterWindowCoroutine));

        Debug.Log("Parry success! Counter window open.");
    }

    // ── Combo Execution ──────────────────────────────────────────────────────

    private IEnumerator ExecuteComboStep(int stepIndex, bool isCounter)
    {
        WeaponTypeData weaponType = character.WeaponTypeData;
        ComboStep step = weaponType.GetComboStep(stepIndex);
        if (step == null) yield break;

        isAttacking = true;

        // Stop any running combo window — we're mid-attack now
        if (comboWindowCoroutine != null)
        {
            StopCoroutine(comboWindowCoroutine);
            comboWindowCoroutine = null;
            inComboWindow = false;
        }

        playerMovement.SetAction(Movement.Attacking);

        // Wait for the hitbox active window, then fire it
        yield return new WaitForSeconds(step.activeTime * 0.5f);
        FireHitbox(step, isCounter);
        yield return new WaitForSeconds(step.activeTime * 0.5f);

        // Recovery before player can act again
        yield return new WaitForSeconds(step.recoveryTime);

        isAttacking = false;

        // Advance combo step
        bool isLastStep = stepIndex >= weaponType.ComboLength - 1;
        if (isLastStep)
        {
            // Full combo finished — reset
            currentComboStep = 0;
            playerMovement.SetAction(Movement.Standing);
        }
        else
        {
            // Open the combo window for the next step
            currentComboStep = stepIndex + 1;
            inComboWindow = true;
            playerMovement.SetAction(Movement.Standing);
            comboWindowCoroutine = StartCoroutine(ComboWindowCoroutine(weaponType.comboWindowTime));
        }

        attackCoroutine = null;
    }

    private IEnumerator ComboWindowCoroutine(float windowTime)
    {
        yield return new WaitForSeconds(windowTime);
        // Window expired — reset combo
        inComboWindow = false;
        currentComboStep = 0;
        comboWindowCoroutine = null;
    }

    private IEnumerator ParryCounterWindowCoroutine()
    {
        yield return new WaitForSeconds(parryCounterWindow);
        inParryCounter = false;
        Debug.Log("Parry counter window expired.");
    }

    // ── Hitbox & Damage ──────────────────────────────────────────────────────

    private void FireHitbox(ComboStep step, bool isCounter)
    {
        // Flip offset based on facing direction
        float facing = transform.localScale.x >= 0 ? 1f : -1f;
        Vector2 offset = new Vector2(step.hitboxOffset.x * facing, step.hitboxOffset.y);
        Vector2 hitboxCenter = (Vector2)transform.position + offset;

        // Store for gizmo drawing
        lastHitboxSize = step.hitboxSize;
        lastHitboxPos  = hitboxCenter;
        drawHitbox     = true;

        Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxCenter, step.hitboxSize, 0f, enemyLayer);

        // Track enemies we've already hit this step (no double-hitting)
        HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();

        foreach (var hit in hits)
        {
            if (alreadyHit.Contains(hit)) continue;
            alreadyHit.Add(hit);

            IDamageable target = hit.GetComponent<IDamageable>();
            if (target == null || !target.IsAlive) continue;

            // We need defender stats — try to get them from a CharacterInstance
            // if available, otherwise use zeroed stats (enemy system will handle this later)
            BaseStats defenderStats = new BaseStats(0, 0, 0, 0);
            Element defenderElement = Element.None;

            CharacterInstance defenderCharacter = hit.GetComponent<CharacterInstanceHolder>()?.Character;
            if (defenderCharacter != null)
            {
                defenderStats   = defenderCharacter.TotalStats;
                defenderElement = defenderCharacter.EffectiveElement;
            }

            // Calculate and apply damage
            float counterMult = isCounter ? parryCounterMultiplier : 1f;
            int rawDamage = CombatCalculator.CalculateDamage(
                character.TotalStats,
                character.EffectiveElement,
                step,
                defenderStats,
                defenderElement
            );
            int finalDamage = Mathf.RoundToInt(rawDamage * counterMult);

            target.TakeDamage(finalDamage, character.EffectiveElement);

            // Apply knockback away from player
            Vector2 knockDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
            float kbForce = CombatCalculator.CalculateKnockback(character.TotalStats, step) * knockbackForce;
            target.ApplyKnockback(knockDir, kbForce);

            // Every landed hit charges the SP gauge
            character.SPGauge.OnHitLanded();

            Debug.Log($"{character.Name} hit {hit.name} for {finalDamage} {character.EffectiveElement} damage" +
                      (isCounter ? " [COUNTER]" : ""));
        }
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!showHitboxGizmos || !drawHitbox) return;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawCube(lastHitboxPos, lastHitboxSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(lastHitboxPos, lastHitboxSize);
    }
}