// Abyss production source excerpt — portfolio review.
// Source commit: ca3674f1e782b1512db07636495fe83a923fc35a
// Source file: Assets/Abyss/Editor/Tests/DungeonSpriteViewTests.cs
// Complete selected method bodies and property declarations are preserved.
// Unrelated members and dependencies are omitted; this file is not compiled.
// See docs/corpse-display.md.

using System.Collections.Generic;
using Abyss.Core;
using Abyss.Core.Application;
using Abyss.Core.Combat;
using Abyss.Presentation.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Abyss.EditorTools.Tests
{
    public sealed class DungeonSpriteViewTests
    {
        // Shared fixture setup, Construct<T> and assertion helpers are omitted.

        // Original lines 141–230.
        [TestCase(1)]
        [TestCase(2)]
        public void C84_DeadHealthAndCorpseDurabilityStaySeparateAcrossSlotReuse(int size)
        {
            Scene scene = BattleSceneBuilder.CreateDungeonScene();
            try
            {
                DungeonStageView stage = FindStage(scene);
                DungeonUnitSlotView slot = stage.MonsterSlots[0];
                FightUnitView alive = Unit("same", "monster.charnel_maw", 1, true, false,
                    28, true, size, 28);
                slot.SetFight(alive, false, false, false, false, false, null, null);
                StatBar healthBar = slot.transform.Find("HealthBar").GetComponent<StatBar>();
                TMP_Text health = slot.transform.Find("Health").GetComponent<TMP_Text>();
                TMP_Text durability = slot.transform.Find("Stress").GetComponent<TMP_Text>();
                TMP_Text status = slot.transform.Find("Status").GetComponent<TMP_Text>();
                Assert.That(health.text, Is.EqualTo("HP 28/28"));
                Assert.That(healthBar.FillAmount, Is.EqualTo(1f));
                Assert.That(slot.CorpseGraphic.gameObject.activeSelf, Is.False);

                FightUnitView corpse = Unit("same", "monster.charnel_maw", 1, false, true,
                    1, true, size, 28);
                slot.SetFight(corpse, false, false, false, false, false, null, null);
                Assert.That(corpse.CurrentHealth, Is.EqualTo(1d),
                    "The renderer must not destroy the Rules snapshot's targetable corpse durability.");
                Assert.That(corpse.LivingHealth, Is.Zero);
                Assert.That(health.text, Is.EqualTo("HP 0/28"));
                Assert.That(healthBar.CurrentValue, Is.Zero);
                Assert.That(healthBar.MaximumValue, Is.EqualTo(28f));
                Assert.That(healthBar.FillAmount, Is.Zero);
                Assert.That(durability.text, Is.EqualTo("시체 내구도 1/1"));
                Assert.That(status.text, Is.EqualTo("시체 · 3라운드"));
                Assert.That(slot.GetComponent<TooltipTarget>().Message,
                    Does.Contain("HP 0/28").And.Contain("시체 내구도 1/1"));
                Assert.That(slot.SpriteAnimator.HasSprite, Is.False);
                Assert.That(slot.SpriteAnimator.Image.enabled, Is.False);
                Assert.That(slot.transform.Find("Portrait").gameObject.activeSelf, Is.False);
                Assert.That(slot.CorpseGraphic.gameObject.activeSelf, Is.True);
                Assert.That(slot.CorpseGraphic.rectTransform.pivot, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(slot.CorpseGraphic.rectTransform.anchoredPosition.y, Is.EqualTo(-68f));
                Assert.That(slot.CorpseGraphic.rectTransform.rect.size,
                    Is.EqualTo(new Vector2(144f, 64f) * (size > 1 ? 1.5f : 1f)));
                Assert.That(slot.CorpseGraphic.raycastTarget, Is.False);
                Assert.That(slot.CorpseGraphic.GetComponent<CanvasRenderer>(), Is.Not.Null,
                    "Corpse artwork must own a serialized CanvasRenderer before rendering begins.");
                AssertCorpseArtwork(slot.CorpseGraphic);
                AssertFloorUnit(slot);

                slot.CorpseGraphic.gameObject.SetActive(false);
                slot.CorpseGraphic.gameObject.SetActive(true);
                AssertCorpseArtwork(slot.CorpseGraphic);
                Assert.That(slot.transform.Find("Portrait").gameObject.activeSelf, Is.False);
                Assert.That(health.text, Is.EqualTo("HP 0/28"));

                slot.Clear();
                Assert.That(slot.gameObject.activeSelf, Is.False);
                Assert.That(slot.CorpseGraphic.gameObject.activeSelf, Is.False);
                Assert.That(slot.IsCorpse, Is.False);
                slot.SetFight(alive, false, false, false, false, false, null, null);
                Assert.That(slot.gameObject.activeSelf, Is.True);
                Assert.That(slot.CorpseGraphic.gameObject.activeSelf, Is.False);
                Assert.That(slot.transform.Find("CorpseMark").gameObject.activeSelf, Is.False);
                Assert.That(health.text, Is.EqualTo("HP 28/28"));
                Assert.That(healthBar.FillAmount, Is.EqualTo(1f));
                Assert.That(durability.text, Is.Empty);
                Assert.That(slot.GetComponent<TooltipTarget>().Message, Does.Not.Contain("시체 내구도"));

                // Imported corpse PNGs may persist as asset references; runtime character frames may not.
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Abyss/UI/Slots/DungeonUnitSlot.prefab");
                Assert.That(prefab, Is.Not.Null);
                DungeonUnitSlotView stored = prefab.GetComponent<DungeonUnitSlotView>();
                Assert.That(stored.CorpseGraphic, Is.Not.Null);
                Assert.That(stored.CorpseGraphic.GetComponent<CanvasRenderer>(), Is.Not.Null,
                    "Do not rely on Graphic.canvasRenderer lazily adding a renderer at runtime.");
                Assert.That(stored.CorpseGraphic.raycastTarget, Is.False);
                Assert.That(stored.SpriteAnimator.Image.sprite, Is.Null);
                if (stored.CorpseGraphic.sprite != null)
                    Assert.That(EditorUtility.IsPersistent(stored.CorpseGraphic.sprite), Is.True,
                        "Only the imported corpse Sprite asset may be serialized, never a generated frame.");
                GameObject reloaded = UnityEngine.Object.Instantiate(prefab);
                objects.Add(reloaded);
                DungeonUnitSlotView reopened = reloaded.GetComponent<DungeonUnitSlotView>();
                reopened.SetFight(corpse, false, false, false, false, false, null, null);
                Assert.That(reopened.CorpseGraphic.gameObject.activeSelf, Is.True);
                Assert.That(reopened.transform.Find("Health").GetComponent<TMP_Text>().text, Is.EqualTo("HP 0/28"));
                Assert.That(reopened.transform.Find("Stress").GetComponent<TMP_Text>().text, Is.EqualTo("시체 내구도 1/1"));
                AssertCorpseArtwork(reopened.CorpseGraphic);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        // Original lines 919–933.
        private static FightUnitView Unit(string id, string character, int rank, bool alive, bool corpse,
            double health, bool monster = false, int size = 1, double maximumHealth = 33d,
            string name = "localized name")
        {
            return Construct<FightUnitView>(new Dictionary<string, object>
            {
                { "unitId", new UnitId(id) }, { "characterId", new CharacterId(character) },
                { "side", monster ? Sides.Monsters : Sides.Heroes },
                { "rank", rank }, { "lastRank", rank + size - 1 }, { "size", size },
                { "name", name }, { "currentHealth", health }, { "maxHealth", maximumHealth },
                { "lifeState", alive ? LifeState.DeathsDoor : LifeState.Defeated },
                { "isAlive", alive }, { "isCorpse", corpse }, { "corpseDecayRounds", corpse ? 3 : 0 },
                { "corpseMaxHealth", corpse ? 1d : 0d }
            });
        }
    }
}
