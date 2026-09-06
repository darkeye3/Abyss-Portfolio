# 실행 순서를 명시한 스킬 효과 구조

스킬마다 효과 분기를 전투 엔진에 추가하면 실행 순서와 난수 소비가 쉽게 달라진다. Abyss에서는 기본 피해·치유 판정을 유지하면서, 부가 효과를 타입과 실행 단계로 분리했다.

## 실행 계약

| 단계 | 실행 시점 |
|---|---|
| BeforePrimaryResolution | 기본 판정 전 |
| AfterAttackHit | 공격 명중 후, 반격·사망 처리 전 |
| AfterPrimaryResolution | 대상 하나의 기본 판정 이후 |
| AfterSkillResolution | 모든 대상 처리 이후, 스킬당 한 번 |

같은 단계의 효과는 콘텐츠 목록 순서대로 실행한다. 전투 종료나 시전자 사망 시 남은 처리를 중단하는 조건은 엔진에서 관리하고, 각 효과는 자신의 규칙을 구현한다. 이 구조로 버프·진형 이동·치료·소환 등의 효과를 같은 실행 경로에 연결했다.

## 핵심 코드 발췌

다음은 실제 enum과 효과 분배 메서드다. 설명을 위해 주석을 생략하고 인자 줄바꿈을 정리했다. 원본 타입과 전투 문맥에 의존하는 발췌이며, 이 코드만으로 실행되는 독립 샘플은 아니다.

```csharp
public enum SkillEffectPhase
{
    BeforePrimaryResolution = 1,
    AfterAttackHit = 2,
    AfterPrimaryResolution = 3,
    AfterSkillResolution = 4
}

private void ApplySkillEffects(
    BattleState battle, CombatUnit actor, CombatUnit target,
    SkillDefinition skill, SkillEffectPhase phase)
{
    SkillEffectContext context = null;
    for (int index = 0; index < skill.Effects.Count; index++)
    {
        SkillEffect effect = skill.Effects[index];
        if (effect.Phase != phase) continue;

        if (context == null)
            context = new SkillEffectContext(
                battle, actor, target, skill,
                log, naming, random, summons);
        effect.Apply(context);
    }
}
```

일치하는 효과가 있을 때만 실행 문맥을 만들고, 해당 단계의 효과끼리 공유한다. 효과의 생성·조회는 외부 계층에 열어 두되 실행은 Rules 내부로 제한해 상태 변경 경계를 유지한다.

## 검증한 동작

- 명중·실패와 단계별 효과 실행 순서, 효과 적용 결과를 확인한다.
- 효과 추가 전후의 이벤트 순서와 난수 소비를 회귀 검사한다.
- 콘텐츠를 읽을 때 알 수 없는 효과·필드·참조와 잘못된 단계 조합을 거절한다.

근거 파일: `SkillEffects.cs`, `BattleEngine.cs`, `SkillEffectExecutionTests.cs`, `SkillEffectContentTests.cs`, `AdvancedSkillEffectTests.cs` — 비공개 원본에서 선별했다.

[저장소 소개로 돌아가기](../README.md)
