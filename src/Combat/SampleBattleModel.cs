// Portfolio-only support types. These are deliberately smaller than the game's models.
// They make the selected effect implementations executable without publishing the full engine.
using System;
using System.Collections.Generic;
using Abyss.Core;

namespace Abyss.Portfolio.Combat
{
    public readonly record struct StatusId(string Value)
    {
        public bool IsEmpty { get { return string.IsNullOrWhiteSpace(Value); } }
        public override string ToString() { return Value; }
    }

    public sealed class CombatUnit
    {
        public string Id { get; }
        public bool IsAlive { get; }
        public bool IsCorpse { get; }
        public CombatUnit(string id, bool isAlive = true, bool isCorpse = false)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Unit ID is required.", nameof(id));
            Id = id; IsAlive = isAlive; IsCorpse = isCorpse;
        }
    }

    public sealed class BattleState
    {
        private readonly Dictionary<string, CombatUnit> units = new Dictionary<string, CombatUnit>(StringComparer.Ordinal);
        private readonly Dictionary<string, StatusCollection> statuses = new Dictionary<string, StatusCollection>(StringComparer.Ordinal);
        public BattleState(params CombatUnit[] initialUnits)
        {
            if (initialUnits == null) throw new ArgumentNullException(nameof(initialUnits));
            foreach (CombatUnit unit in initialUnits)
            {
                if (unit == null) throw new ArgumentException("Null unit.", nameof(initialUnits));
                units.Add(unit.Id, unit);
                statuses.Add(unit.Id, new StatusCollection());
            }
        }
        public CombatUnit GetUnit(string id) { return units[id]; }
        public StatusCollection StatusesOf(string id) { return statuses[id]; }
    }

    public sealed class SkillDefinition
    {
        public string Name { get; }
        public IReadOnlyList<SkillEffect> Effects { get; }
        public SkillDefinition(string name, params SkillEffect[] effects)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Skill name is required.", nameof(name));
            if (effects == null) throw new ArgumentNullException(nameof(effects));
            foreach (SkillEffect effect in effects)
                if (effect == null) throw new ArgumentException("Null effect.", nameof(effects));
            Name = name;
            Effects = Array.AsReadOnly((SkillEffect[])effects.Clone());
        }
    }

    public interface IUnitNaming { string GetName(CombatUnit unit); }
    public sealed class IdNaming : IUnitNaming
    {
        public string GetName(CombatUnit unit) { return unit.Id; }
    }

    internal sealed class SkillEffectContext
    {
        internal BattleState Battle { get; }
        internal CombatUnit Actor { get; }
        internal CombatUnit Target { get; }
        internal SkillDefinition Skill { get; }
        internal CombatEventLog Log { get; }
        internal IUnitNaming Naming { get; }
        internal IRandomSource Random { get; }
        internal SkillEffectContext(BattleState battle, CombatUnit actor, CombatUnit target,
            SkillDefinition skill, CombatEventLog log, IUnitNaming naming, IRandomSource random)
        {
            Battle = battle ?? throw new ArgumentNullException(nameof(battle));
            Actor = actor ?? throw new ArgumentNullException(nameof(actor));
            Target = target; // The once-per-skill phase has no target.
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            Log = log ?? throw new ArgumentNullException(nameof(log));
            Naming = naming ?? throw new ArgumentNullException(nameof(naming));
            Random = random ?? throw new ArgumentNullException(nameof(random));
        }
    }

    public abstract record CombatEvent
    {
        public int Sequence { get; internal set; }
    }
    public sealed record SkillUsedEvent(string Name) : CombatEvent;
    public sealed record PrimaryResolvedEvent(string TargetId, PrimaryOutcome Outcome) : CombatEvent;
    public sealed record StatusAppliedEvent(string TargetId, string TargetName,
        StatusId Status, int Amount, int Turns) : CombatEvent;
    public sealed record StatusCuredEvent(string TargetId, string TargetName,
        StatusId Status, int RemovedStacks) : CombatEvent;

    public sealed class CombatEventLog
    {
        private readonly List<CombatEvent> events = new List<CombatEvent>();
        private readonly IReadOnlyList<CombatEvent> view;
        public CombatEventLog() { view = events.AsReadOnly(); }
        public IReadOnlyList<CombatEvent> Events { get { return view; } }
        public void Publish(CombatEvent entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            entry.Sequence = events.Count;
            events.Add(entry);
        }
    }
}
