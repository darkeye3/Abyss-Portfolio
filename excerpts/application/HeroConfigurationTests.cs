// Abyss production source excerpt — portfolio review.
// Source commit: ca3674f1e782b1512db07636495fe83a923fc35a
// Source file: Core/Tests~/HeroConfigurationTests.cs
// Complete method bodies are preserved; unrelated members and dependencies are omitted.
// This excerpt is not compiled by Abyss.Portfolio.csproj. See docs/hero-configuration.md.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Abyss.Core.Application;
using Abyss.Core.Combat;
using Abyss.Core.Content;
using Abyss.Core.Estate;
using Abyss.Core.Heroes;
using Abyss.Core.Infrastructure;
using Abyss.Core.Save;
using Abyss.Core.Stats;
using Abyss.Core.World;

namespace Abyss.Core.Tests
{
    internal static class HeroConfigurationTests
    {
        // Fields, DTOs, helper methods and fixture setup remain in the original project.

        // Original lines 129–160.
        private static void H04_H21_S06_전투야영선택은_저장되고_아이콘순서는_고정된다()
        {
            WithSession((session, directory) =>
            {
                Hero hero = session.Game.Estate.Roster.Heroes[0];
                IEstateService service = session.CreateEstateService();
                EstateHeroDetailsState before = service.ReadHeroDetails(hero.RosterId.Value);
                string first = before.CombatSkills[0].Id;
                string second = before.CombatSkills[1].Id;
                string random = JsonSerializer.Serialize(session.Streams.Capture());
                Harness.True(service.SetCombatLoadout(hero.RosterId.Value, new[] { second, first }).Succeeded, "전투 기술 순서 설정");
                string state = Capture(session);
                Harness.False(service.SetCombatLoadout(hero.RosterId.Value, Array.Empty<string>()).Succeeded, "전투 최소 1개");
                Harness.False(service.SetCombatLoadout(hero.RosterId.Value, new[] { first, first }).Succeeded, "중복 전투 기술 거절");
                Harness.False(service.SetCombatLoadout(hero.RosterId.Value, new[] { "skill.unknown" }).Succeeded, "다른/미등록 전투 기술 거절");
                Harness.Equal(state, Capture(session), "거절 후 선택/저장 상태 불변");
                Harness.True(service.SetCombatLoadout(hero.RosterId.Value, new[] { first }).Succeeded, "하나만 선택 가능");
                EstateHeroDetailsState one = service.ReadHeroDetails(hero.RosterId.Value);
                Harness.False(Find(one.CombatSkills, first).CanUse, "마지막 기술 해제 불가 이유를 UI에서 제공");
                List<string> camp = new List<string>(hero.CampSkills.SelectedIds);
                camp.Reverse();
                Harness.True(service.SetCampLoadout(hero.RosterId.Value, camp).Succeeded, "기존 야영 명령 재사용 및 저장");
                EstateHeroDetailsState after = service.ReadHeroDetails(hero.RosterId.Value);
                for (int index = 0; index < before.CampSkills.Count; index++)
                    Harness.Equal(before.CampSkills[index].Id, after.CampSkills[index].Id, "야영 아이콘 고정 위치");
                Harness.Equal(camp[0], hero.CampSkills.SelectedIds[0], "선택 실행 순서는 따로 유지");
                CampaignSession resumed = Reopen(directory);
                Harness.Equal(Capture(session), Capture(resumed), "전투·야영 선택 순서 저장 왕복");
                Harness.Equal(random, JsonSerializer.Serialize(session.Streams.Capture()), "설정/저장/조회 난수 미소비");
                Harness.Equal(1, session.Content.CombatSkillsOf(hero).Count, "실제 전투 카탈로그도 설정한 1개만 제공");
            });
        }

        // Original lines 162–186.
        private static void S07_H09_설정저장실패는_장착수량과_선택을_복원한다()
        {
            ContentDatabase content = ContentLoader.LoadFromDirectory(DataDirectory());
            RandomStreams streams = new RandomStreams(1234UL);
            AutoPlay game = new AutoPlay(content, streams);
            CampaignSession session = new CampaignSession(); // 의도적으로 저장 경로 없음
            session.SelectSlot(1);
            session.AttachCampaign(content, game, streams, null);
            Hero hero = game.Estate.Roster.Heroes[0];
            game.Estate.TrinketStash.Add("trinket.iron_ring");
            IEstateService service = session.CreateEstateService();
            string before = Capture(session);
            Harness.False(service.EquipHeroTrinket(hero.RosterId.Value, 1, "trinket.iron_ring").Succeeded, "저장 실패 장착 거절");
            Harness.Equal(before, Capture(session), "장착 실패 슬롯과 창고 rollback");
            Harness.False(service.SetCampLoadout(hero.RosterId.Value, new[] { hero.CampSkills.SelectedIds[0] }).Succeeded, "저장 실패 야영 거절");
            Harness.Equal(before, Capture(session), "야영 저장 실패 선택 rollback");
            hero.CombatSkills.Restore(false, Array.Empty<string>());
            before = Capture(session);
            string skill = content.GetCharacter(hero.ClassId).Skills[0].Value;
            Harness.False(service.SetCombatLoadout(hero.RosterId.Value, new[] { skill }).Succeeded, "저장 실패 전투 거절");
            Harness.Equal(before, Capture(session), "미초기화 선택 플래그까지 rollback");
            Harness.False(hero.CombatSkills.IsInitialized, "표시용 조회로도 미초기화를 변경하지 않는다");
            service.ReadHeroDetails(hero.RosterId.Value);
            Harness.False(hero.CombatSkills.IsInitialized, "상세 조회도 미초기화 선택을 변경하지 않는다");
        }

        // Original lines 349–353.
        private static string Capture(CampaignSession session)
        {
            return SaveCodec.Encode(SaveMapper.Capture(session.Game.Campaign, session.Streams,
                contentRevision: session.Content.ContentRevision));
        }
    }
}
