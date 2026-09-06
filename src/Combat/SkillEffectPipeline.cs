// Sample driver around the actual phase dispatcher. See docs/combat-effects.md for extraction boundaries.
using System;
using System.Collections.Generic;
using Abyss.Core;

namespace Abyss.Portfolio.Combat
{
    public enum PrimaryOutcome { Support, Miss, Hit }

    // Tests supply the primary outcome; the full damage/healing resolver stays in the game.
    public interface IPrimaryResolver
    {
        PrimaryOutcome Resolve(CombatUnit actor, CombatUnit target, IRandomSource random);
    }

    public sealed class SkillEffectPipeline
    {
        private readonly IRandomSource random;
        private readonly IPrimaryResolver primary;
        private readonly IUnitNaming naming = new IdNaming();
        private readonly CombatEventLog log = new CombatEventLog();
        public CombatEventLog Log { get { return log; } }

        public SkillEffectPipeline(IRandomSource random, IPrimaryResolver primary)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.primary = primary ?? throw new ArgumentNullException(nameof(primary));
        }

        // This orchestrator is reduced for the public sample; it is not the complete BattleEngine.
        public void Execute(BattleState battle, CombatUnit actor, SkillDefinition skill,
            IReadOnlyList<string> targetIds)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            if (targetIds == null || targetIds.Count == 0) throw new ArgumentException("At least one target is required.", nameof(targetIds));
            if (!ReferenceEquals(battle.GetUnit(actor.Id), actor) || !actor.IsAlive || actor.IsCorpse)
                throw new ArgumentException("Actor must be active in this battle.", nameof(actor));
            // Resolve the full target list before any state or event changes.
            List<CombatUnit> targets = new List<CombatUnit>();
            HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in targetIds)
            {
                if (!unique.Add(id)) throw new ArgumentException("Duplicate target.", nameof(targetIds));
                CombatUnit target = battle.GetUnit(id);
                if (!target.IsAlive || target.IsCorpse) throw new ArgumentException("Target must be active.", nameof(targetIds));
                targets.Add(target);
            }

            log.Publish(new SkillUsedEvent(skill.Name));
            foreach (CombatUnit target in targets)
                ApplySkillEffects(battle, actor, target, skill, SkillEffectPhase.BeforePrimaryResolution);
            foreach (CombatUnit target in targets)
            {
                PrimaryOutcome outcome = primary.Resolve(actor, target, random);
                if (outcome != PrimaryOutcome.Support)
                    log.Publish(new PrimaryResolvedEvent(target.Id, outcome));
                if (outcome == PrimaryOutcome.Hit)
                    ApplySkillEffects(battle, actor, target, skill, SkillEffectPhase.AfterAttackHit);
                ApplySkillEffects(battle, actor, target, skill, SkillEffectPhase.AfterPrimaryResolution);
            }
            ApplySkillEffects(battle, actor, null, skill, SkillEffectPhase.AfterSkillResolution);
        }

        // Actual BattleEngine.ApplySkillEffects implementation; only the unused summons argument is omitted.
        private void ApplySkillEffects(
            BattleState battle,
            CombatUnit actor,
            CombatUnit target,
            SkillDefinition skill,
            SkillEffectPhase phase)
        {
            SkillEffectContext context = null;
            for (int index = 0; index < skill.Effects.Count; index++)
            {
                SkillEffect effect = skill.Effects[index];
                if (effect.Phase != phase) continue;

                if (context == null)
                    context = new SkillEffectContext(
                        battle,
                        actor,
                        target,
                        skill,
                        log,
                        naming,
                        random);
                effect.Apply(context);
            }
        }
    }
}
