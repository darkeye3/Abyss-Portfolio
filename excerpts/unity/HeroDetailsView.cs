// Abyss production source excerpt — portfolio review.
// Source commit: ca3674f1e782b1512db07636495fe83a923fc35a
// Source file: Assets/Abyss/Presentation/UI/EstateHeroDetailsWindowView.cs
// Complete method bodies are preserved; unrelated members and dependencies are omitted.
// This excerpt is not compiled by Abyss.Portfolio.csproj. See docs/hero-configuration.md.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Abyss.Core.Application;
using Abyss.Presentation.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Abyss.Presentation.UI
{
    public sealed class EstateHeroDetailsWindowView : MonoBehaviour
    {
        // Fields, DTOs, helper methods and fixture setup remain in the original project.

        // Original lines 236–244.
        public void SetActions(Action<string, bool> skill, Action<int, string> equip, Action<int> unequip)
        {
            WireSheetActions();
            skillAction = skill;
            equipAction = equip;
            unequipAction = unequip;
            if (unequipButton != null)
                unequipButton.SetAction(() => { if (unequipAction != null) unequipAction(selectedTrinketSlot); });
        }

        // Original lines 246–306.
        public void ApplyState(EstateHeroDetailsState value, bool preview = false, bool preserveScroll = false)
        {
            bool sameHero = State != null && value != null && State.Summary.RosterId == value.Summary.RosterId;
            State = value;
            previewMode = preview;
            if (value == null) { SetVisible(false); return; }
            if (!sameHero || !preserveScroll) selectedTrinketSlot = 0;
            if (!sameHero || !preserveScroll) HideDrawers();
            EstateHeroState hero = value.Summary;
            identityLabel.text = hero.Name + "\n" + hero.ClassName + "\n레벨 " + hero.ResolveLevel;
            progressLabel.text = hero.Status + "  ·  경험치 " + value.Experience.ToString(CultureInfo.InvariantCulture)
                + (value.NextLevelExperience > 0 ? " / " + value.NextLevelExperience : " · 현재 상한")
                + "\n" + value.PromotionDescription;
            guideLabel.text = preview ? "편집기 배치 표본 · 실제 캠페인 상태가 아님"
                : "기벽·기술·장비에 마우스를 올려 상세 확인   /   금색 테두리: 장착 중   /   Esc: 닫기";
            if (value.MentalState != null) guideLabel.text = value.MentalState.Label + "  ·  마우스를 올려 효과 확인";
            TooltipTarget mentalTooltip = guideLabel.GetComponent<TooltipTarget>();
            if (mentalTooltip == null) mentalTooltip = guideLabel.gameObject.AddComponent<TooltipTarget>();
            guideLabel.raycastTarget = true;
            mentalTooltip.SetMessage(value.MentalState == null ? string.Empty : value.MentalState.Description);
            healthBar.SetValue(hero.CurrentHealth, hero.MaximumHealth, StatBarTone.Health, "체력");
            stressBar.SetValue(hero.Stress, hero.MaximumStress, StatBarTone.Stress, "스트레스");
            portrait.Bind(hero.CharacterId);
            weaponArt.Bind(hero.CharacterId, EquipmentArtKind.Weapon);
            armorArt.Bind(hero.CharacterId, EquipmentArtKind.Armor);
            equipmentLabel.text = "무기 " + hero.WeaponTier + "단계  ·  방어구 " + hero.ArmorTier + "단계";
            string equipmentEffects = equipmentLabel.text + "\n" + value.WeaponDescription + "\n" + value.ArmorDescription;
            if (equipmentTooltip != null) equipmentTooltip.SetMessage(equipmentEffects);
            foreach (TooltipTarget target in weaponArt.GetComponentsInParent<TooltipTarget>()) target.SetMessage(equipmentEffects);
            foreach (TooltipTarget target in armorArt.GetComponentsInParent<TooltipTarget>()) target.SetMessage(equipmentEffects);
            combatLabel.richText = true;
            resistanceLabel.richText = true;
            combatLabel.text = TwoColumnEntries(SheetStatOrder(value.Stats), "표시할 능력치 없음");
            resistanceLabel.text = TwoColumnEntries(value.Resistances, "표시할 저항 없음");
            combatSkillsLabel.text = SelectionSummary(value.CombatSkills, value.CombatSkillLimit, false);
            campSkillsLabel.text = SelectionSummary(value.CampSkills, value.CampSkillLimit, true);
            positiveQuirksLabel.text = "긍정 기벽 없음";
            negativeQuirksLabel.text = "부정 기벽 없음";
            diseasesLabel.text = "질병 없음";
            positiveQuirksLabel.gameObject.SetActive(value.PositiveQuirks.Count == 0);
            negativeQuirksLabel.gameObject.SetActive(value.NegativeQuirks.Count == 0);
            diseasesLabel.gameObject.SetActive(value.Diseases.Count == 0);
            BindSkills(value.CombatSkills, combatChoices, combatChoicesRoot, false);
            List<EstateHeroDetailEntry> ownedCamp = new List<EstateHeroDetailEntry>();
            List<EstateHeroDetailEntry> lockedCamp = new List<EstateHeroDetailEntry>();
            foreach (EstateHeroDetailEntry entry in value.CampSkills)
                if (entry.IsUnlocked) ownedCamp.Add(entry); else lockedCamp.Add(entry);
            BindSkills(ownedCamp, campChoices, campChoicesRoot, true);
            BindSkills(lockedCamp, unlearnedCampChoices, unlearnedCampChoicesRoot, true);
            if (unlearnedOpenButton != null)
            {
                unlearnedOpenButton.SetContent(null, "미습득 " + lockedCamp.Count);
                unlearnedOpenButton.SetInteractable(lockedCamp.Count > 0, "모든 야영 기술을 습득했습니다.");
            }
            BindTraits(value.PositiveQuirks, positiveTraits, positiveTraitsRoot, theme.TextGold);
            BindTraits(value.NegativeQuirks, negativeTraits, negativeTraitsRoot, new Color(0.86f, 0.39f, 0.32f));
            BindTraits(value.Diseases, diseaseTraits, diseaseTraitsRoot, new Color(0.66f, 0.72f, 0.40f));
            BindTrinkets();
            if (!preserveScroll) ShowStatus(value.CanConfigure ? "" : value.ConfigurationReason, !value.CanConfigure);
            RefreshScrollLayout(!preserveScroll);
        }

        // Original lines 333–349.
        private void BindSkills(IReadOnlyList<EstateHeroDetailEntry> entries,
            List<HeroDetailChoiceView> views, RectTransform parent, bool camp)
        {
            ResizeChoices(views, parent, entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                EstateHeroDetailEntry entry = entries[index];
                string hint = !State.CanConfigure ? State.ConfigurationReason
                    : !entry.CanUse ? entry.UnavailableReason : entry.IsSelected ? "클릭하여 장착 해제" : "클릭하여 장착";
                string marker = !entry.IsUnlocked ? "미습득" : camp
                    ? entry.IsSelected ? (entry.SlotIndex + 1).ToString(CultureInfo.InvariantCulture) : "-"
                    : entry.Rank.ToString(CultureInfo.InvariantCulture);
                views[index].Bind(entry, () => { if (skillAction != null) skillAction(entry.Id, camp); },
                    marker, hint, !previewMode && State.CanConfigure && entry.CanUse,
                    entry.IsSelected, camping: camp);
            }
        }

        // Original lines 351–400.
        private void BindTrinkets()
        {
            if (State == null) return;
            selectedTrinketSlot = Mathf.Clamp(selectedTrinketSlot, 0, Mathf.Max(0, State.Trinkets.Count - 1));
            ResizeChoices(trinketSlots, trinketSlotsRoot, State.Trinkets.Count);
            for (int index = 0; index < State.Trinkets.Count; index++)
            {
                EstateHeroDetailEntry entry = State.Trinkets[index];
                trinketSlots[index].Bind(entry, () => SelectTrinketSlot(entry.SlotIndex),
                    "슬롯 " + (entry.SlotIndex + 1), "클릭해 장착할 슬롯 선택", true,
                    entry.SlotIndex == selectedTrinketSlot, trinket: true);
            }
            trinketsLabel.text = State.Trinkets.Count == 0 ? "장신구 슬롯 없음"
                : "장신구 칸을 눌러 교체";
            ResizeChoices(inventoryChoices, trinketInventoryRoot, State.TrinketInventory.Count);
            for (int index = 0; index < State.TrinketInventory.Count; index++)
            {
                EstateHeroDetailEntry entry = State.TrinketInventory[index];
                bool canEquip = entry.CanUse;
                string equipReason = entry.UnavailableReason;
                if (entry.EquipSlots.Count > 0)
                {
                    canEquip = false;
                    equipReason = "선택한 슬롯의 장착 정보를 찾을 수 없습니다.";
                    for (int slot = 0; slot < entry.EquipSlots.Count; slot++)
                    {
                        EstateTrinketSlotAvailability availability = entry.EquipSlots[slot];
                        if (availability.SlotIndex != selectedTrinketSlot) continue;
                        canEquip = availability.CanEquip;
                        equipReason = availability.Reason;
                        break;
                    }
                }
                string hint = !State.CanConfigure ? State.ConfigurationReason
                    : !canEquip ? equipReason : "클릭하면 선택한 슬롯에 장착 · 기존 장신구는 보관함으로 반환";
                inventoryChoices[index].Bind(entry,
                    () => { if (equipAction != null) equipAction(selectedTrinketSlot, entry.Id); },
                    "보유 " + entry.Quantity, hint, !previewMode && State.CanConfigure && canEquip,
                    false, trinket: true);
            }
            inventoryLabel.text = State.TrinketInventory.Count == 0
                ? "보관 중인 장신구가 없습니다.\n장착한 장신구를 해제하거나 원정에서 획득하세요."
                : "슬롯 " + (selectedTrinketSlot + 1) + "에 장착 · 보유한 장신구만 표시";
            bool canUnequip = !previewMode && State.CanConfigure && State.Trinkets.Count > 0
                && State.Trinkets[selectedTrinketSlot].CanUse;
            unequipButton.SetInteractable(canUnequip,
                !State.CanConfigure ? State.ConfigurationReason : State.Trinkets.Count == 0
                    ? "장신구 슬롯이 없습니다." : State.Trinkets[selectedTrinketSlot].UnavailableReason);
            unequipButton.SetTooltip("선택한 슬롯의 장신구를 보관함으로 돌려보냅니다.");
        }
    }
}
