// Abyss production source excerpt — portfolio review.
// Source commit: ca3674f1e782b1512db07636495fe83a923fc35a
// Source file: Assets/Abyss/Presentation/UI/EstateScreen.HeroDetails.cs
// Complete method bodies are preserved; unrelated members and dependencies are omitted.
// This excerpt is not compiled by Abyss.Portfolio.csproj. See docs/hero-configuration.md.

using System;
using System.Collections.Generic;
using Abyss.Core.Application;
using UnityEngine;

namespace Abyss.Presentation.UI
{
    public sealed partial class EstateScreen
    {
        // Fields, DTOs, helper methods and fixture setup remain in the original project.

        // Original lines 51–73.
        public void ToggleHeroDetailSkill(string skillId, bool camping)
        {
            if (heroDetailsWindow == null || !heroDetailsWindow.IsVisible || service == null) return;
            EstateHeroDetailsState details = heroDetailsWindow.State;
            if (details == null) return;
            IReadOnlyList<EstateHeroDetailEntry> entries = camping ? details.CampSkills : details.CombatSkills;
            EstateHeroDetailEntry target = null;
            List<EstateHeroDetailEntry> selected = new List<EstateHeroDetailEntry>();
            foreach (EstateHeroDetailEntry entry in entries)
            {
                if (entry.Id == skillId) target = entry;
                if (entry.IsSelected) selected.Add(entry);
            }
            if (target == null) return;
            selected.Sort((left, right) => left.SlotIndex.CompareTo(right.SlotIndex));
            List<string> ids = new List<string>();
            foreach (EstateHeroDetailEntry entry in selected)
                if (entry.Id != skillId) ids.Add(entry.Id);
            if (!target.IsSelected) ids.Add(skillId);
            ApplyHeroDetailCommand(() => camping
                ? service.SetCampLoadout(details.Summary.RosterId, ids)
                : service.SetCombatLoadout(details.Summary.RosterId, ids));
        }

        // Original lines 75–79.
        public void EquipHeroDetailTrinket(int slot, string id)
        {
            if (heroDetailsWindow == null || heroDetailsWindow.State == null) return;
            ApplyHeroDetailCommand(() => service.EquipHeroTrinket(heroDetailsWindow.State.Summary.RosterId, slot, id));
        }

        // Original lines 81–85.
        public void UnequipHeroDetailTrinket(int slot)
        {
            if (heroDetailsWindow == null || heroDetailsWindow.State == null) return;
            ApplyHeroDetailCommand(() => service.UnequipHeroTrinket(heroDetailsWindow.State.Summary.RosterId, slot));
        }

        // Original lines 87–106.
        private void ApplyHeroDetailCommand(Func<EstateActionResult> command)
        {
            if (previewMode || service == null || heroDetailsWindow == null || !heroDetailsWindow.IsVisible) return;
            int rosterId = heroDetailsWindow.State.Summary.RosterId;
            try
            {
                EstateActionResult result = command();
                EstateHeroDetailsState refreshed = service.ReadHeroDetails(rosterId);
                heroDetailsWindow.ApplyState(refreshed, previewMode, preserveScroll: true);
                heroDetailsWindow.ShowStatus(result.Message, !result.Succeeded);
                // Update this portrait in place: recreating the roster would destroy modal return focus.
                if (refreshed != null && panel != null)
                    foreach (HeroSlotView slot in panel.HeroSlots)
                        if (slot.State != null && slot.State.RosterId == rosterId) slot.SetData(refreshed.Summary);
            }
            catch (Exception exception)
            {
                heroDetailsWindow.ShowStatus("설정을 변경하지 못했습니다: " + exception.Message, true);
            }
        }
    }
}
