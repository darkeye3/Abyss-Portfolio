// Abyss production source excerpt — portfolio review.
// Source commit: ca3674f1e782b1512db07636495fe83a923fc35a
// Source file: Core/Application/FightView.cs
// Source file: Core/Application/AutoPlay.Queries.cs
// Complete selected method bodies and property declarations are preserved.
// Unrelated members and dependencies are omitted; this file is not compiled.
// See docs/corpse-display.md.

using System.Collections.Generic;
using Abyss.Core.Combat;
using Abyss.Core.Content;
using Abyss.Core.Heroes;
using Abyss.Core.Stats;

namespace Abyss.Core.Application
{
    public sealed class FightUnitView
    {
        // Selected properties: FightView.cs original lines 77–85 and 100–103.
        /// <summary>규칙의 원시 체력. 시체일 때는 파괴 가능한 내구도를 담는다.</summary>
        public double CurrentHealth { get; }
        /// <summary>생존 체력 표시용 값. 시체의 내구도를 HP로 표시하지 않는다.</summary>
        public double LivingHealth { get { return IsAlive ? CurrentHealth : 0d; } }
        /// <summary>시체 파괴에 필요한 남은 내구도. 살아 있는 유닛은 0이다.</summary>
        public double CorpseHealth { get { return IsCorpse ? CurrentHealth : 0d; } }
        /// <summary>전투 규칙이 정한 시체 초기 내구도. 생전 최대 HP와 별개다.</summary>
        public double CorpseMaxHealth { get; }
        public double MaxHealth { get; }
        public LifeState LifeState { get; }
        public bool IsAlive { get; }
        public bool IsCorpse { get; }
        public int CorpseDecayRounds { get; }
        // Other properties and the constructor are omitted.
        // In the original constructor (line 170), CorpseMaxHealth is assigned
        // as: isCorpse ? corpseMaxHealth : 0d.
    }

    public sealed partial class AutoPlay
    {
        // AutoPlay.Queries.cs original lines 511–571.
        private void AppendFightUnits(
            BattleState battle,
            Formation formation,
            IReadOnlyDictionary<UnitId, Hero> heroesByUnit,
            List<FightUnitView> units,
            IDictionary<UnitId, FightUnitView> unitsById)
        {
            IReadOnlyList<FormationSlot> slots = formation.Slots;
            for (int index = 0; index < slots.Count; index++)
            {
                FormationSlot slot = slots[index];
                CombatUnit unit = slot.Unit;
                Hero hero;
                bool hasHero = heroesByUnit.TryGetValue(unit.Id, out hero);
                CharacterDefinition definition = content.GetCharacter(unit.CharacterId);
                string name = hasHero ? hero.Name : content.GetName(unit);
                RosterId heroId = hasHero ? hero.RosterId : default(RosterId);
                int weaponTier = hasHero ? hero.WeaponTier : 0;
                int armorTier = hasHero ? hero.ArmorTier : 0;
                IReadOnlyList<string> trinketIds = hasHero
                    ? hero.Trinkets.EquippedIds
                    : new string[0];

                FightUnitView view = new FightUnitView(
                    unit.Id,
                    unit.CharacterId,
                    definition.Name,
                    heroId,
                    name,
                    unit.Side,
                    slot.Rank,
                    slot.Rank + unit.Size - 1,
                    unit.Size,
                    unit.CurrentHealth,
                    unit.MaxHealth,
                    unit.CurrentStress,
                    unit.MaxStress,
                    unit.Stats.Get(StatIds.Accuracy),
                    unit.Stats.Get(StatIds.Dodge),
                    unit.Stats.Get(StatIds.Protection),
                    unit.Stats.Get(StatIds.Speed),
                    unit.Stats.Get(StatIds.CriticalChance),
                    unit.Stats.Get(StatIds.DamageMin),
                    unit.Stats.Get(StatIds.DamageMax),
                    weaponTier,
                    armorTier,
                    trinketIds,
                    hasHero ? BuildHeroTrinketView(hero.Trinkets.Left) : null,
                    hasHero ? BuildHeroTrinketView(hero.Trinkets.Right) : null,
                    unit.LifeState,
                    unit.IsAlive,
                    unit.IsCorpse,
                    unit.CorpseDecayRounds,
                    BuildFightStatuses(battle.StatusesOf(unit.Id)),
                    MentalStateView.From(content.TraitCatalog, unit.MentalStateId),
                    unit.IsCorpse ? battle.Rules.CorpseHealth : 0d);

                units.Add(view);
                unitsById.Add(unit.Id, view);
            }
        }
    }
}
