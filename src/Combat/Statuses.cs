// Actual implementation from Core/Rules/Combat/Statuses.cs; comments shortened and namespace changed.
using System;
using System.Collections.Generic;

namespace Abyss.Portfolio.Combat
{
    public static class StatusIds
    {
        public static readonly StatusId Bleed = new StatusId("status.bleed");
        public static readonly StatusId Blight = new StatusId("status.blight");
        public static readonly StatusId Stun = new StatusId("status.stun");
        public static readonly StatusId Mark = new StatusId("status.mark");
        public static readonly StatusId Riposte = new StatusId("status.riposte");
        public static readonly StatusId DeathRecovery = new StatusId("status.death_recovery");
    }
    public sealed class DamageOverTimeStack
    {
        public int DamagePerTurn { get; }
        public int TurnsRemaining { get; private set; }

        public bool IsActive { get { return TurnsRemaining > 0; } }

        public DamageOverTimeStack(int damagePerTurn, int turns)
        {
            if (damagePerTurn < 0)
                throw new ArgumentOutOfRangeException(nameof(damagePerTurn), "틱 피해는 음수일 수 없다.");
            if (turns < 1)
                throw new ArgumentOutOfRangeException(nameof(turns), "지속 턴은 1 이상이어야 한다.");
            DamagePerTurn = damagePerTurn;
            TurnsRemaining = turns;
        }

        internal void Advance()
        {
            if (TurnsRemaining > 0) TurnsRemaining--;
        }
    }
    public sealed class DamageOverTime
    {
        private readonly List<DamageOverTimeStack> stacks = new List<DamageOverTimeStack>();

        public StatusId Id { get; }

        public DamageOverTime(StatusId id)
        {
            if (id.IsEmpty) throw new ArgumentException("상태 ID는 비어 있을 수 없다.", nameof(id));
            Id = id;
        }

        public bool IsActive
        {
            get
            {
                for (int index = 0; index < stacks.Count; index++)
                    if (stacks[index].IsActive) return true;
                return false;
            }
        }

        public int StackCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < stacks.Count; index++)
                    if (stacks[index].IsActive) count++;
                return count;
            }
        }
        public int TotalDamagePerTurn
        {
            get
            {
                int total = 0;
                for (int index = 0; index < stacks.Count; index++)
                    if (stacks[index].IsActive) total += stacks[index].DamagePerTurn;
                return total;
            }
        }

        public void AddStack(int damagePerTurn, int turns)
        {
            stacks.Add(new DamageOverTimeStack(damagePerTurn, turns));
        }
        public IReadOnlyList<DamageOverTimeStack> Stacks { get { return stacks; } }
        public int PeekDamage() { return TotalDamagePerTurn; }
        public void AdvanceOwnerTurn()
        {
            for (int index = 0; index < stacks.Count; index++)
                stacks[index].Advance();
            stacks.RemoveAll(s => !s.IsActive);
        }
        public int Clear()
        {
            int removed = StackCount;
            stacks.Clear();
            return removed;
        }
    }
    public sealed class StatusCollection
    {
        private readonly Dictionary<StatusId, DamageOverTime> overTime;
        private readonly Dictionary<StatusId, int> timed;

        public StatusCollection()
        {
            overTime = new Dictionary<StatusId, DamageOverTime>
            {
                { StatusIds.Bleed, new DamageOverTime(StatusIds.Bleed) },
                { StatusIds.Blight, new DamageOverTime(StatusIds.Blight) }
            };
            timed = new Dictionary<StatusId, int>();
        }

        public DamageOverTime Bleed { get { return overTime[StatusIds.Bleed]; } }
        public DamageOverTime Blight { get { return overTime[StatusIds.Blight]; } }

        public DamageOverTime GetOverTime(StatusId id)
        {
            DamageOverTime found;
            if (overTime.TryGetValue(id, out found)) return found;
            throw new InvalidOperationException("지속 피해 상태가 아니다: " + id);
        }

        public bool IsOverTime(StatusId id) { return overTime.ContainsKey(id); }
        public void SetTimed(StatusId id, int turns)
        {
            if (id.IsEmpty) throw new ArgumentException("상태 ID는 비어 있을 수 없다.", nameof(id));
            if (turns < 0) throw new ArgumentOutOfRangeException(nameof(turns));
            if (turns == 0) { timed.Remove(id); return; }
            timed[id] = turns;
        }

        public int GetTimed(StatusId id)
        {
            int turns;
            return timed.TryGetValue(id, out turns) ? turns : 0;
        }

        public bool Has(StatusId id) { return GetTimed(id) > 0; }
        public IReadOnlyList<KeyValuePair<StatusId, int>> AllTimed()
        {
            List<KeyValuePair<StatusId, int>> all =
                new List<KeyValuePair<StatusId, int>>(timed);
            all.Sort((a, b) => string.CompareOrdinal(a.Key.Value, b.Key.Value));
            return all;
        }

        public void Clear(StatusId id) { timed.Remove(id); }
        public void AdvanceOwnerTurn()
        {
            foreach (KeyValuePair<StatusId, DamageOverTime> pair in overTime)
                pair.Value.AdvanceOwnerTurn();

            if (timed.Count == 0) return;

            List<StatusId> keys = new List<StatusId>(timed.Keys);
            for (int index = 0; index < keys.Count; index++)
            {
                StatusId key = keys[index];
                int remaining = timed[key] - 1;
                if (remaining <= 0) timed.Remove(key);
                else timed[key] = remaining;
            }
        }
    }
}
