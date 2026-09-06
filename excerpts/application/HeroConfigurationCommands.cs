// Abyss production source excerpt — portfolio review.
// Source commit: ca3674f1e782b1512db07636495fe83a923fc35a
// Source file: Core/Application/EstateApplicationService.HeroConfiguration.cs
// Complete method bodies are preserved; unrelated members and dependencies are omitted.
// This excerpt is not compiled by Abyss.Portfolio.csproj. See docs/hero-configuration.md.

using System;
using System.Collections.Generic;
using Abyss.Core.Estate;
using Abyss.Core.Heroes;

namespace Abyss.Core.Application
{
    internal sealed partial class EstateService
    {
        // Fields, DTOs, helper methods and fixture setup remain in the original project.

        // Original lines 10–25.
        public EstateActionResult SetCombatLoadout(int rosterId, IReadOnlyList<string> orderedSkillIds)
        {
            EnsureCurrentCampaign();
            Hero hero;
            string reason;
            if (!TryGetConfigurableHero(rosterId, out hero, out reason))
                return EstateActionResult.Failure(reason);
            List<string> previous = new List<string>(hero.CombatSkills.SelectedIds);
            bool wasInitialized = hero.CombatSkills.IsInitialized;
            if (!content.TrySetCombatLoadout(hero, orderedSkillIds, out reason))
                return EstateActionResult.Failure(reason);
            return SaveHeroConfiguration(hero.Name + "의 전투 기술을 설정했다.", () =>
            {
                hero.CombatSkills.Restore(wasInitialized, previous);
            });
        }

        // Original lines 27–43.
        public EstateActionResult EquipHeroTrinket(int rosterId, int slotIndex, string trinketId)
        {
            EnsureCurrentCampaign();
            Hero hero;
            string reason;
            if (!TryGetConfigurableHero(rosterId, out hero, out reason))
                return EstateActionResult.Failure(reason);
            TrinketDefinition definition = FindTrinketDefinition(trinketId);
            TrinketEquipmentQuote quote = game.Estate.QueryEquipTrinket(hero, slotIndex, definition);
            if (!quote.CanChange) return EstateActionResult.Failure(quote.Reason);
            IReadOnlyDictionary<string, int> previousStash = game.Estate.TrinketStash.Counts;
            string previous = hero.Trinkets.GetAt(slotIndex);
            quote = game.Estate.EquipTrinket(hero, slotIndex, definition);
            if (!quote.CanChange) return EstateActionResult.Failure(quote.Reason);
            return SaveHeroConfiguration(hero.Name + "에게 " + definition.Name + "을(를) 장착했다.", () =>
                RestoreTrinketConfiguration(hero, slotIndex, previous, previousStash));
        }

        // Original lines 62–69.
        private void RestoreTrinketConfiguration(Hero hero, int slotIndex, string previous,
            IReadOnlyDictionary<string, int> previousStash)
        {
            string ignored;
            if (!hero.Trinkets.TryReplaceAt(slotIndex, previous, out ignored))
                throw new InvalidOperationException("이전 장신구 칸을 복원하지 못했다.");
            game.Estate.TrinketStash.RestoreCounts(previousStash);
        }

        // Original lines 71–83.
        private EstateActionResult SaveHeroConfiguration(string message, Action rollback)
        {
            try
            {
                session.SaveCurrent();
                return EstateActionResult.Success(message);
            }
            catch (Exception exception)
            {
                rollback();
                return EstateActionResult.Failure("설정을 저장하지 못해 변경을 취소했다: " + exception.Message);
            }
        }

        // Original lines 85–95.
        private bool TryGetConfigurableHero(int rosterId, out Hero hero, out string reason)
        {
            hero = null;
            if (rosterId <= 0 || !game.Estate.Roster.TryGet(new RosterId(rosterId), out hero))
            {
                reason = "현재 영지에 없는 영웅이다.";
                return false;
            }
            reason = HeroConfigurationReason(hero);
            return reason.Length == 0;
        }

        // Original lines 97–104.
        private string HeroConfigurationReason(Hero hero)
        {
            if (session.HasActiveRaid || session.PendingRaidSnapshot != null)
                return "진행 중인 원정이 있어 영웅 설정을 바꿀 수 없다.";
            if (!hero.IsAvailable)
                return "영지에서 대기 중인 살아 있는 영웅만 설정할 수 있다.";
            return string.Empty;
        }

        // Original lines 106–112.
        private TrinketDefinition FindTrinketDefinition(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            foreach (TrinketDefinition definition in content.Trinkets)
                if (string.Equals(definition.Id, id, StringComparison.Ordinal)) return definition;
            return null;
        }
    }
}
