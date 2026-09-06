// Abyss production source excerpt — portfolio review.
// Source commit: ca3674f1e782b1512db07636495fe83a923fc35a
// Source file: Core/Application/EstateApplicationService.HeroDetails.cs
// Complete method bodies are preserved; unrelated members and dependencies are omitted.
// This excerpt is not compiled by Abyss.Portfolio.csproj. See docs/hero-configuration.md.

using System;
using System.Collections.Generic;
using System.Globalization;
using Abyss.Core.Combat;
using Abyss.Core.Content;
using Abyss.Core.Estate;
using Abyss.Core.Heroes;
using Abyss.Core.Stats;
using Abyss.Core.World;

namespace Abyss.Core.Application
{
    internal sealed partial class EstateService
    {
        // Fields, DTOs, helper methods and fixture setup remain in the original project.

        // Original lines 91–113.
        private IReadOnlyList<EstateHeroDetailEntry> ReadCombatSkills(Hero hero, CharacterDefinition character)
        {
            List<EstateHeroDetailEntry> entries = new List<EstateHeroDetailEntry>();
            List<string> selectedIds = new List<string>();
            foreach (SkillId id in content.CombatSkillsOf(hero)) selectedIds.Add(id.Value);
            for (int index = 0; index < character.Skills.Count; index++)
            {
                SkillDefinition skill;
                if (!content.TryGet(character.Skills[index], out skill))
                    throw new InvalidOperationException("영웅의 전투 스킬 정의가 없다.");
                int rank = hero.SkillRanks.Of(skill.Id);
                int selectedIndex = selectedIds.IndexOf(skill.Id.Value);
                bool selected = selectedIndex >= 0;
                List<string> candidate = ToggleSkill(selectedIds, skill.Id.Value, selected);
                string reason = HeroConfigurationReason(hero);
                if (reason.Length == 0) content.CanSetCombatLoadout(hero, candidate, out reason);
                entries.Add(new EstateHeroDetailEntry(skill.Id.Value, skill.Name,
                    DescribeCombatSkill(skill, rank), isSelected: selected,
                    slotIndex: selectedIndex, rank: rank,
                    canUse: reason.Length == 0, unavailableReason: reason));
            }
            return entries;
        }

        // Original lines 137–145.
        private static List<string> ToggleSkill(IReadOnlyList<string> selected, string id, bool remove)
        {
            List<string> candidate = new List<string>();
            for (int index = 0; index < selected.Count; index++)
                if (!remove || !string.Equals(selected[index], id, StringComparison.Ordinal))
                    candidate.Add(selected[index]);
            if (!remove) candidate.Add(id);
            return candidate;
        }
    }
}
