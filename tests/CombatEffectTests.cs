using System;
using System.Collections.Generic;
using Abyss.Core;
using Abyss.Portfolio.Combat;

namespace Abyss.Portfolio.Tests
{
    // Public-sample integration tests, adapted from the original C40/C41/C51/C89 contracts.
    internal static class CombatEffectTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("C89_phase_and_list_order_produce_typed_events", () => C89_Order(runner));
            runner.Run("C89_miss_skips_hit_effects_but_preserves_after_primary", () => C89_Miss(runner));
            runner.Run("C51_cure_removes_only_requested_stacks_and_repeated_cure_is_noop", () => C51_Cure(runner));
            runner.Run("C40_C41_independent_stacks_expire_on_owner_turn", () => C41_Duration(runner));
            runner.Run("C89_all_targets_finish_before_once_per_skill_effect", () => C89_MultipleTargets(runner));
            runner.Run("A04_deterministic_effects_preserve_primary_rng_state", () => A04_Random(runner));
            runner.Run("C89_sample_rejects_late_invalid_target_before_mutation", () => C89_Validation(runner));
            runner.Run("C40_C41_C51_invalid_effect_definitions_are_rejected", () => C51_InvalidDefinitions(runner));
        }

        private static SkillEffectPipeline Engine(PrimaryOutcome outcome, IRandomSource random = null, int draws = 0)
        {
            return new SkillEffectPipeline(random ?? new Pcg32RandomSource(37UL, 11UL), new FixedPrimary(outcome, draws));
        }

        private static void C89_Order(TestRunner r)
        {
            CombatUnit actor = new CombatUnit("actor"), target = new CombatUnit("target");
            BattleState battle = new BattleState(actor, target);
            SkillDefinition skill = new SkillDefinition("combined",
                new CureSkillEffect(StatusIds.Bleed), // Listed first, but its phase runs after all hit effects.
                new DamageOverTimeSkillEffect(StatusIds.Bleed, 2, 3),
                new TimedStatusSkillEffect(StatusIds.Mark, 3),
                new DamageOverTimeSkillEffect(StatusIds.Blight, 4, 5));
            SkillEffectPipeline engine = Engine(PrimaryOutcome.Hit);
            engine.Execute(battle, actor, skill, new[] { target.Id });
            IReadOnlyList<CombatEvent> events = engine.Log.Events;
            r.Equal(6, events.Count, "Skill, primary, three hit effects, cure.");
            r.True(events[0] is SkillUsedEvent && events[1] is PrimaryResolvedEvent, "Primary result precedes extra effects.");
            r.Equal(StatusIds.Bleed, ((StatusAppliedEvent)events[2]).Status, "First hit effect.");
            r.Equal(StatusIds.Mark, ((StatusAppliedEvent)events[3]).Status, "Second hit effect.");
            r.Equal(StatusIds.Blight, ((StatusAppliedEvent)events[4]).Status, "Third hit effect.");
            r.Equal(1, ((StatusCuredEvent)events[5]).RemovedStacks, "Cure sees the preceding hit effect's state.");
            r.Equal(0, battle.StatusesOf(target.Id).Bleed.StackCount, "Cured bleed is absent.");
            r.Equal(4, battle.StatusesOf(target.Id).Blight.TotalDamagePerTurn, "Other status survives.");
            r.Equal(4, battle.StatusesOf(target.Id).GetTimed(StatusIds.Mark), "Timed status keeps current-turn adjustment.");
            for (int i = 0; i < events.Count; i++) r.Equal(i, events[i].Sequence, "Event sequence is stable.");
        }

        private static void C89_Miss(TestRunner r)
        {
            CombatUnit actor = new CombatUnit("actor"), target = new CombatUnit("target");
            BattleState battle = new BattleState(actor, target);
            battle.StatusesOf(target.Id).Bleed.AddStack(3, 2);
            SkillEffectPipeline engine = Engine(PrimaryOutcome.Miss);
            engine.Execute(battle, actor, new SkillDefinition("miss",
                new DamageOverTimeSkillEffect(StatusIds.Blight, 4, 5),
                new TimedStatusSkillEffect(StatusIds.Mark, 2), new CureSkillEffect(StatusIds.Bleed)), new[] { target.Id });
            r.Equal(0, battle.StatusesOf(target.Id).Blight.StackCount, "A missed attack cannot add DoT.");
            r.Equal(0, battle.StatusesOf(target.Id).GetTimed(StatusIds.Mark), "A missed attack cannot add mark.");
            r.Equal(0, battle.StatusesOf(target.Id).Bleed.StackCount, "AfterPrimary still runs after miss.");
            r.Equal(3, engine.Log.Events.Count, "No StatusApplied event is emitted on miss.");
            r.True(engine.Log.Events[2] is StatusCuredEvent, "AfterPrimary event follows miss.");
        }

        private static void C51_Cure(TestRunner r)
        {
            CombatUnit actor = new CombatUnit("actor"), target = new CombatUnit("target");
            BattleState battle = new BattleState(actor, target);
            StatusCollection statuses = battle.StatusesOf(target.Id);
            statuses.Bleed.AddStack(2, 2); statuses.Bleed.AddStack(4, 4);
            statuses.Blight.AddStack(1, 3); statuses.SetTimed(StatusIds.Mark, 2);
            Pcg32RandomSource random = new Pcg32RandomSource(37UL, 11UL);
            RandomSnapshot before = random.Capture();
            SkillEffectPipeline engine = Engine(PrimaryOutcome.Support, random);
            SkillDefinition cure = new SkillDefinition("cure bleed", new CureSkillEffect(StatusIds.Bleed));
            engine.Execute(battle, actor, cure, new[] { target.Id });
            r.Equal(2, ((StatusCuredEvent)engine.Log.Events[1]).RemovedStacks, "All requested stacks are counted.");
            r.Equal(0, statuses.Bleed.StackCount, "All bleed stacks removed.");
            r.Equal(1, statuses.Blight.StackCount, "Blight preserved.");
            r.Equal(2, statuses.GetTimed(StatusIds.Mark), "Mark preserved.");
            engine.Execute(battle, actor, cure, new[] { target.Id });
            r.Equal(3, engine.Log.Events.Count, "Repeating cure adds only SkillUsed.");
            r.True(engine.Log.Events[2] is SkillUsedEvent, "No false cure event.");
            r.Equal(before, random.Capture(), "Cure and repeated no-op consume no randomness.");
        }

        private static void C41_Duration(TestRunner r)
        {
            CombatUnit unit = new CombatUnit("self");
            BattleState battle = new BattleState(unit);
            SkillEffectPipeline engine = Engine(PrimaryOutcome.Hit);
            engine.Execute(battle, unit, new SkillDefinition("two durations",
                new DamageOverTimeSkillEffect(StatusIds.Bleed, 2, 1),
                new DamageOverTimeSkillEffect(StatusIds.Bleed, 3, 3),
                new TimedStatusSkillEffect(StatusIds.Mark, 2)), new[] { unit.Id });
            StatusCollection statuses = battle.StatusesOf(unit.Id);
            r.Equal(5, statuses.Bleed.TotalDamagePerTurn, "Independent damage is summed.");
            statuses.AdvanceOwnerTurn();
            r.Equal(1, statuses.Bleed.StackCount, "Only the short stack expires.");
            r.Equal(3, statuses.Bleed.TotalDamagePerTurn, "Long stack damage remains.");
            r.Equal(2, statuses.Bleed.Stacks[0].TurnsRemaining, "Long stack duration advances once.");
            r.Equal(2, statuses.GetTimed(StatusIds.Mark), "Current-action adjustment preserves intended mark duration.");
            statuses.AdvanceOwnerTurn(); statuses.AdvanceOwnerTurn();
            r.Equal(0, statuses.Bleed.StackCount, "Long stack expires independently.");
            r.Equal(0, statuses.GetTimed(StatusIds.Mark), "Timed status expires on owner turns.");
        }

        private static void C89_MultipleTargets(TestRunner r)
        {
            CombatUnit actor = new CombatUnit("actor"), a = new CombatUnit("a"), b = new CombatUnit("b");
            BattleState battle = new BattleState(actor, a, b);
            List<string> trace = new List<string>();
            SkillEffectPipeline engine = Engine(PrimaryOutcome.Hit);
            engine.Execute(battle, actor, new SkillDefinition("multi",
                new PhaseProbe(SkillEffectPhase.AfterSkillResolution, trace),
                new PhaseProbe(SkillEffectPhase.AfterPrimaryResolution, trace),
                new PhaseProbe(SkillEffectPhase.BeforePrimaryResolution, trace),
                new PhaseProbe(SkillEffectPhase.AfterAttackHit, trace)), new[] { a.Id, b.Id });
            r.Equal("BeforePrimaryResolution:a|BeforePrimaryResolution:b|AfterAttackHit:a|AfterPrimaryResolution:a|AfterAttackHit:b|AfterPrimaryResolution:b|AfterSkillResolution:all",
                string.Join("|", trace), "Before effects prepare all targets; after-skill runs once with no target.");
        }

        private static void A04_Random(TestRunner r)
        {
            CombatUnit actor = new CombatUnit("actor"), target = new CombatUnit("target");
            Pcg32RandomSource plain = new Pcg32RandomSource(91UL, 5UL), extra = new Pcg32RandomSource(91UL, 5UL);
            Engine(PrimaryOutcome.Hit, plain, 2).Execute(new BattleState(actor, target), actor,
                new SkillDefinition("plain"), new[] { target.Id });
            Engine(PrimaryOutcome.Hit, extra, 2).Execute(new BattleState(actor, target), actor,
                new SkillDefinition("effects", new DamageOverTimeSkillEffect(StatusIds.Bleed, 2, 3),
                    new TimedStatusSkillEffect(StatusIds.Mark, 1), new CureSkillEffect(StatusIds.Bleed)), new[] { target.Id });
            r.Equal(plain.Capture(), extra.Capture(), "Adding deterministic effects leaves the primary resolver's RNG state unchanged.");
            r.Equal(plain.NextDouble(), extra.NextDouble(), "The next gameplay draw also stays aligned.");
        }

        private static void C89_Validation(TestRunner r)
        {
            CombatUnit actor = new CombatUnit("actor"), target = new CombatUnit("target");
            BattleState battle = new BattleState(actor, target);
            Pcg32RandomSource random = new Pcg32RandomSource(37UL, 11UL);
            RandomSnapshot before = random.Capture();
            SkillEffectPipeline engine = Engine(PrimaryOutcome.Hit, random, 2);
            SkillDefinition skill = new SkillDefinition("validate first", new DamageOverTimeSkillEffect(StatusIds.Bleed, 2, 3));
            r.Throws<KeyNotFoundException>(() => engine.Execute(battle, actor, skill, new[] { target.Id, "unknown" }), "Later invalid target rejects the whole request.");
            r.Equal(0, battle.StatusesOf(target.Id).Bleed.StackCount, "Earlier valid target remains unchanged.");
            r.Equal(0, engine.Log.Events.Count, "Rejected request emits no partial log.");
            r.Equal(before, random.Capture(), "Rejected request consumes no random values.");
        }

        private static void C51_InvalidDefinitions(TestRunner r)
        {
            r.Throws<ArgumentException>(() => new CureSkillEffect(StatusIds.Mark), "Cure cannot remove mark.");
            r.Throws<ArgumentException>(() => new DamageOverTimeSkillEffect(StatusIds.Mark, 1, 1), "Only registered DoT types are valid.");
            r.Throws<ArgumentOutOfRangeException>(() => new DamageOverTimeSkillEffect(StatusIds.Bleed, 0, 1), "DoT damage must be positive.");
            r.Throws<ArgumentOutOfRangeException>(() => new DamageOverTimeSkillEffect(StatusIds.Bleed, 1, 0), "DoT duration must be positive.");
            r.Throws<ArgumentOutOfRangeException>(() => new TimedStatusSkillEffect(StatusIds.Mark, int.MaxValue), "Current-turn adjustment must not overflow.");
        }

        private sealed class FixedPrimary : IPrimaryResolver
        {
            private readonly PrimaryOutcome outcome;
            private readonly int draws;
            public FixedPrimary(PrimaryOutcome outcome, int draws) { this.outcome = outcome; this.draws = draws; }
            public PrimaryOutcome Resolve(CombatUnit actor, CombatUnit target, IRandomSource random)
            {
                for (int i = 0; i < draws; i++) random.NextDouble();
                return outcome;
            }
        }

        private sealed class PhaseProbe : SkillEffect
        {
            private readonly List<string> trace;
            public PhaseProbe(SkillEffectPhase phase, List<string> trace) : base(phase) { this.trace = trace; }
            internal override void Apply(SkillEffectContext context)
            {
                trace.Add(Phase + ":" + (context.Target == null ? "all" : context.Target.Id));
            }
        }
    }
}
