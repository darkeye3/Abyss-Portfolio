// Abyss production source excerpt — portfolio review.
// Source commit: ca3674f1e782b1512db07636495fe83a923fc35a
// Source file: Assets/Abyss/Presentation/UI/DungeonUnitSlotView.cs
// Complete selected method bodies and property declarations are preserved.
// Unrelated members and dependencies are omitted; this file is not compiled.
// See docs/corpse-display.md.

using System;
using Abyss.Core;
using Abyss.Core.Application;
using UnityEngine;

namespace Abyss.Presentation.UI
{
    public sealed class DungeonUnitSlotView : MonoBehaviour
    {
        // Serialized fields, rendering helpers and other members are omitted.

        // Original lines 156–228.
        public void SetFight(
            FightUnitView state,
            bool currentActor,
            bool selectionActive,
            bool targetable,
            bool selectedTarget,
            bool keyboardFocused,
            Action<UnitId> onSelected,
            StringTable strings)
        {
            if (state == null)
            {
                Clear();
                return;
            }

            gameObject.SetActive(true);
            unitId = state.UnitId;
            partyIndex = -1;
            unitAction = onSelected;
            partyAction = null;
            IsCorpse = state.IsCorpse;
            IsTargetable = targetable;
            IsCurrentActor = currentActor;
            IsSelectedTarget = selectedTarget;
            IsKeyboardFocused = keyboardFocused;

            SetIdentity(state.Name, state.ClassName, state.Rank);
            SetDisplaySize(state.Size);
            BindSprite(state.CharacterId.Value, "fight:" + state.UnitId.Value,
                state.IsAlive && !state.IsCorpse);
            SetHealth(state.LivingHealth, state.MaxHealth);
            if (stressLabel != null)
            {
                stressLabel.text = state.IsCorpse
                    ? "시체 내구도 " + CorpseDurability(state)
                    : state.IsHero
                    ? "스트레스 " + Mathf.RoundToInt((float)state.CurrentStress)
                        + "/" + Mathf.RoundToInt((float)state.MaxStress)
                    : string.Empty;
                if (theme != null) stressLabel.color = state.IsCorpse ? theme.TextDim : theme.Stress;
            }
            if (statusLabel != null) statusLabel.text = DescribeStatus(state, strings);
            if (corpseMark != null) corpseMark.SetActive(state.IsCorpse);
            SetCorpseArtwork(state.IsCorpse);
            if (frame != null)
            {
                // 밝기는 targetable 여부가 맡고, 금색 선택선은 실제로 고른 대상만 쓴다.
                // 키보드 커서는 hover 색으로 한 단계 더 구분한다.
                frame.SetSelected(currentActor || selectedTarget);
                frame.SetHovered(keyboardFocused);
            }

            float opacity = state.IsCorpse ? 0.48f : 1f;
            if (selectionActive && !targetable) opacity *= 0.34f;
            SetOpacity(opacity);

            if (button != null)
            {
                button.SetContent(null, state.IsCorpse ? "사망" : state.Rank.ToString());
                button.SetTooltip(
                    DescribeTooltip(state)
                    + (targetable && selectionActive
                        ? " · ←/→ 대상 이동 · Enter 선택"
                        : string.Empty)
                    + (keyboardFocused ? " · 현재 키보드 대상" : string.Empty));
                button.SetInteractable(
                    !selectionActive || targetable,
                    "선택한 스킬로 지목할 수 없다.");
                button.SetAction(InvokeUnit);
            }
            ApplyFloorPresentation();
        }

        // Original lines 518–526.
        private static string DescribeTooltip(FightUnitView state)
        {
            return state.Name + " · 랭크 " + state.Rank
                + " · HP " + Mathf.RoundToInt((float)state.LivingHealth)
                + "/" + Mathf.RoundToInt((float)state.MaxHealth)
                + (state.IsCorpse ? " · 사망 · 시체 내구도 " + CorpseDurability(state)
                    + " · " + state.CorpseDecayRounds + "라운드 뒤 소멸 · 직접 지목하여 제거 가능"
                    : string.Empty);
        }

        // Original lines 528–534.
        private static string CorpseDurability(FightUnitView state)
        {
            string current = state.CorpseHealth.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            return state.CorpseMaxHealth > 0d
                ? current + "/" + state.CorpseMaxHealth.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                : current;
        }
    }
}
