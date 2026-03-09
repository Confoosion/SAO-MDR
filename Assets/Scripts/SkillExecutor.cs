using UnityEngine;
using System.Collections;

/// <summary>
/// Executes a character's skill when the SP gauge is full.
/// Attach alongside PlayerCombat on the player GameObject.
/// </summary>
public class SkillExecutor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Debug")]
    [SerializeField] private bool showSkillGizmos = true;

    private CharacterInstance character;
    private bool isExecuting = false;

    // Gizmo data
    private Vector2 lastSkillHitboxSize;
    private Vector2 lastSkillHitboxPos;
    private float lastBurstRadius;
    private bool drawSkillGizmo = false;
    private bool drawBurstGizmo = false;

    // ── Setup ────────────────────────────────────────────────────────────────

    public void SetCharacter(CharacterInstance characterInstance)
    {
        character = characterInstance;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Call this when the player triggers their skill (button press, swipe, etc.)
    /// </summary>
    public void TryActivateSkill()
    {
        if (character == null || character.Skill == null) return;
        if (isExecuting) return;
        if (playerMovement.CurrentAction == Movement.Jumping) return;
        if (playerMovement.CurrentAction == Movement.Dashing) return;

        SkillData skill = character.Skill;

        if (!character.SPGauge.CanActivate(skill.spCost))
        {
            Debug.Log($"Not enough SP to activate {skill.skillName}.");
            return;
        }

        StartCoroutine(ExecuteSkill(skill));
    }

    // ── Execution ────────────────────────────────────────────────────────────

    private IEnumerator ExecuteSkill(SkillData skill)
    {
        isExecuting = true;
        character.SPGauge.TrySpend(skill.spCost);
        playerMovement.SetAction(Movement.Attacking);

        Debug.Log($"{character.Name} activates {skill.skillName}!");

        foreach (var effect in skill.effects)
        {
            ExecuteEffect(effect, skill.skillName);
            // Small stagger between multiple effects for feel
            yield return new WaitForSeconds(0.1f);
        }

        playerMovement.SetAction(Movement.Standing);
        isExecuting = false;
    }

    private void ExecuteEffect(SkillEffect effect, string sourceName)
    {
        switch (effect.effectType)
        {
            case SkillEffectType.DamageSingle:
                ExecuteDamageSingle(effect);
                break;

            case SkillEffectType.DamageAoE:
                ExecuteDamageAoE(effect);
                break;

            case SkillEffectType.Heal:
                ExecuteHeal(effect);
                break;

            case SkillEffectType.BuffStat:
                ExecuteStatModifier(effect, sourceName, positive: true);
                break;

            case SkillEffectType.DebuffStat:
                ExecuteStatModifier(effect, sourceName, positive: false);
                break;

            case SkillEffectType.ElementalBurst:
                ExecuteElementalBurst(effect);
                break;
        }
    }

    // ── Effect Implementations ───────────────────────────────────────────────

    private void ExecuteDamageSingle(SkillEffect effect)
    {
        float facing = transform.localScale.x >= 0 ? 1f : -1f;
        Vector2 offset = new Vector2(effect.hitboxOffset.x * facing, effect.hitboxOffset.y);
        Vector2 center = (Vector2)transform.position + offset;

        UpdateHitboxGizmo(center, effect.hitboxSize);

        Collider2D hit = Physics2D.OverlapBox(center, effect.hitboxSize, 0f, enemyLayer);
        if (hit == null) return;

        ApplyDamageToTarget(hit, effect);
    }

    private void ExecuteDamageAoE(SkillEffect effect)
    {
        float facing = transform.localScale.x >= 0 ? 1f : -1f;
        Vector2 offset = new Vector2(effect.hitboxOffset.x * facing, effect.hitboxOffset.y);
        Vector2 center = (Vector2)transform.position + offset;

        UpdateHitboxGizmo(center, effect.hitboxSize);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, effect.hitboxSize, 0f, enemyLayer);
        foreach (var hit in hits)
            ApplyDamageToTarget(hit, effect);
    }

    private void ExecuteHeal(SkillEffect effect)
    {
        int healAmount = Mathf.RoundToInt(character.TotalStats.hp * effect.powerMultiplier);
        character.Heal(healAmount);
        Debug.Log($"{character.Name} healed for {healAmount} HP.");
    }

    private void ExecuteStatModifier(SkillEffect effect, string sourceName, bool positive)
    {
        // Buff applies to self, debuff applies to nearby enemies
        if (positive)
        {
            var mod = new StatModifier(effect.targetStat, Mathf.Abs(effect.statModifierAmount), effect.duration, sourceName);
            character.AddModifier(mod);
            Debug.Log($"{character.Name} gained +{mod.amount} {mod.targetStat} for {mod.duration}s.");
        }
        else
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(
                (Vector2)transform.position + effect.hitboxOffset,
                effect.hitboxSize, 0f, enemyLayer);

            foreach (var hit in hits)
            {
                CharacterInstanceHolder holder = hit.GetComponent<CharacterInstanceHolder>();
                if (holder?.Character == null) continue;

                var mod = new StatModifier(effect.targetStat, -Mathf.Abs(effect.statModifierAmount), effect.duration, sourceName);
                holder.Character.AddModifier(mod);
                Debug.Log($"{hit.name} received -{Mathf.Abs(effect.statModifierAmount)} {effect.targetStat} for {effect.duration}s.");
            }
        }
    }

    private void ExecuteElementalBurst(SkillEffect effect)
    {
        // Burst hits everything in a radius using the character's effective element
        Vector2 center = transform.position;

        drawBurstGizmo  = true;
        lastBurstRadius = effect.burstRadius;
        lastSkillHitboxPos = center;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, effect.burstRadius, enemyLayer);
        foreach (var hit in hits)
            ApplyDamageToTarget(hit, effect);

        Debug.Log($"{character.Name} unleashes a {character.EffectiveElement} elemental burst!");
    }

    // ── Shared Helpers ───────────────────────────────────────────────────────

    private void ApplyDamageToTarget(Collider2D hit, SkillEffect effect)
    {
        IDamageable target = hit.GetComponent<IDamageable>();
        if (target == null || !target.IsAlive) return;

        BaseStats defenderStats   = new BaseStats(0, 0, 0, 0);
        Element defenderElement   = Element.None;

        CharacterInstanceHolder holder = hit.GetComponent<CharacterInstanceHolder>();
        if (holder?.Character != null)
        {
            defenderStats   = holder.Character.TotalStats;
            defenderElement = holder.Character.EffectiveElement;
        }

        // Reuse a throwaway ComboStep so we can use CombatCalculator unchanged
        ComboStep skillStep = new ComboStep { damageMultiplier = effect.powerMultiplier };
        int damage = CombatCalculator.CalculateDamage(
            character.TotalStats,
            character.EffectiveElement,
            skillStep,
            defenderStats,
            defenderElement
        );

        target.TakeDamage(damage, character.EffectiveElement);

        Vector2 knockDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
        float kbForce    = CombatCalculator.CalculateKnockback(character.TotalStats, skillStep);
        target.ApplyKnockback(knockDir, kbForce);

        Debug.Log($"{character.Name}'s skill hit {hit.name} for {damage} {character.EffectiveElement} damage.");
    }

    private void UpdateHitboxGizmo(Vector2 center, Vector2 size)
    {
        lastSkillHitboxPos  = center;
        lastSkillHitboxSize = size;
        drawSkillGizmo      = true;
        drawBurstGizmo      = false;
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!showSkillGizmos) return;

        if (drawSkillGizmo)
        {
            Gizmos.color = new Color(0.2f, 0.4f, 1f, 0.5f);
            Gizmos.DrawCube(lastSkillHitboxPos, lastSkillHitboxSize);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(lastSkillHitboxPos, lastSkillHitboxSize);
        }

        if (drawBurstGizmo)
        {
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f);
            Gizmos.DrawSphere(lastSkillHitboxPos, lastBurstRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastSkillHitboxPos, lastBurstRadius);
        }
    }
}