// Actual implementation selected from Core/Rules/Combat/SkillEffects.cs.
// Only namespace/imports and explanatory comments are changed. See docs/combat-effects.md.
using System;

namespace Abyss.Portfolio.Combat
{
    public enum SkillEffectPhase
    {
        BeforePrimaryResolution = 1,
        AfterAttackHit = 2,
        AfterPrimaryResolution = 3,
        AfterSkillResolution = 4
    }

    public abstract class SkillEffect
    {
        public SkillEffectPhase Phase { get; }

        internal SkillEffect(SkillEffectPhase phase)
        {
            if (!Enum.IsDefined(typeof(SkillEffectPhase), phase))
                throw new ArgumentOutOfRangeException(nameof(phase));
            Phase = phase;
        }

        internal abstract void Apply(SkillEffectContext context);
    }

    public sealed class DamageOverTimeSkillEffect : SkillEffect
    {
        public StatusId Status { get; }
        public int DamagePerTurn { get; }
        public int Turns { get; }

        public DamageOverTimeSkillEffect(
            StatusId status,
            int damagePerTurn,
            int turns)
            : base(SkillEffectPhase.AfterAttackHit)
        {
            if (status != StatusIds.Bleed && status != StatusIds.Blight)
                throw new ArgumentException(
                    "지속 피해 효과는 출혈 또는 중독이어야 한다.",
                    nameof(status));
            if (damagePerTurn < 1)
                throw new ArgumentOutOfRangeException(nameof(damagePerTurn));
            if (turns < 1)
                throw new ArgumentOutOfRangeException(nameof(turns));
            Status = status;
            DamagePerTurn = damagePerTurn;
            Turns = turns;
        }

        internal override void Apply(SkillEffectContext context)
        {
            context.Battle.StatusesOf(context.Target.Id)
                .GetOverTime(Status)
                .AddStack(DamagePerTurn, Turns);
            context.Log.Publish(new StatusAppliedEvent(
                context.Target.Id,
                context.Naming.GetName(context.Target),
                Status,
                DamagePerTurn,
                Turns));
        }
    }

    public sealed class TimedStatusSkillEffect : SkillEffect
    {
        public StatusId Status { get; }
        public int Turns { get; }

        public TimedStatusSkillEffect(StatusId status, int turns)
            : base(SkillEffectPhase.AfterAttackHit)
        {
            if (status.IsEmpty) throw new ArgumentException("상태 ID가 비었다.", nameof(status));
            if (turns < 1 || turns == int.MaxValue)
                throw new ArgumentOutOfRangeException(
                    nameof(turns),
                    "시간제 상태의 턴은 1 이상 Int32 최댓값 미만이어야 한다.");
            Status = status;
            Turns = turns;
        }

        internal override void Apply(SkillEffectContext context)
        {
            context.Battle.StatusesOf(context.Target.Id).SetTimed(Status, Turns + 1);
            context.Log.Publish(new StatusAppliedEvent(
                context.Target.Id,
                context.Naming.GetName(context.Target),
                Status,
                0,
                Turns));
        }
    }

    public sealed class CureSkillEffect : SkillEffect
    {
        public StatusId Status { get; }

        public CureSkillEffect(StatusId status)
            : base(SkillEffectPhase.AfterPrimaryResolution)
        {
            if (status != StatusIds.Bleed && status != StatusIds.Blight)
                throw new ArgumentException(
                    "Cure는 출혈 또는 중독만 제거할 수 있다.",
                    nameof(status));
            Status = status;
        }

        internal override void Apply(SkillEffectContext context)
        {
            if (context.Target == null
                || !context.Target.IsAlive
                || context.Target.IsCorpse)
                return;
            int removed = context.Battle.StatusesOf(context.Target.Id)
                .GetOverTime(Status)
                .Clear();
            if (removed <= 0) return;
            context.Log.Publish(new StatusCuredEvent(
                context.Target.Id,
                context.Naming.GetName(context.Target),
                Status,
                removed));
        }
    }
}
