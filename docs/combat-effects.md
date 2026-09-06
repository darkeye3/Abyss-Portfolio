# 스킬 효과: 타입 → 실행 단계 → 상태 변경 → 이벤트

스킬마다 전투 엔진에 분기를 추가하면 효과의 상대 순서와 난수 소비가 달라지기 쉽다. Abyss는 기본 판정 주변의 실행 지점을 네 단계로 나누고, 각 효과가 자신의 상태 변경과 결과 이벤트를 담당하도록 구성했다.

이 저장소에서는 **원본의 효과 3종과 상태 스택 구현을 실제로 실행**할 수 있다. 다음 순서로 읽으면 정의에서 검증까지 연결된다.

1. [SkillEffects.cs](../src/Combat/SkillEffects.cs): `SkillEffect` 추상 클래스와 지속 피해(DoT)·시간제 상태·치료의 구체 구현
2. [SkillEffectPipeline.cs](../src/Combat/SkillEffectPipeline.cs): 단계 호출과 실제 효과 분배 메서드
3. [Statuses.cs](../src/Combat/Statuses.cs): 독립 스택·소유자 턴 만료·치료 처리
4. [CombatEffectTests.cs](../tests/CombatEffectTests.cs): 단계별 실행 결과·이벤트 순서·난수 상태를 검사하는 통합 테스트

## 실행 계약

| 단계 | 원 프로젝트의 실행 시점 | 공개한 효과 |
|---|---|---|
| BeforePrimaryResolution | 기본 판정 전 | 단계 실행을 테스트로 검증 |
| AfterAttackHit | 공격 명중 후, 반격·사망 처리 전 | DamageOverTimeSkillEffect, TimedStatusSkillEffect |
| AfterPrimaryResolution | 대상 하나의 기본 판정 이후 | CureSkillEffect |
| AfterSkillResolution | 모든 대상 처리 이후, 스킬당 한 번 | 다중 대상 테스트로 검증 |

각 효과의 생성자에서 실행 단계를 고정한다. `ApplySkillEffects`는 해당 단계와 일치하는 효과만 목록 순서대로 호출하고, 첫 효과가 있을 때 생성한 실행 문맥을 같은 단계의 효과끼리 공유한다. 효과의 실행 메서드와 기반 생성자는 `internal`이므로 원 프로젝트의 외부 계층은 정의를 생성·조회할 수 있고, 규칙 실행은 Rules 어셈블리가 담당한다.

예를 들어 효과 목록을 **치료 → 출혈 → 표식 → 중독** 순서로 선언해도, 적중 후 단계인 출혈·표식·중독이 먼저 실행되고 이후 단계의 치료가 방금 추가된 출혈을 제거한다. `C89_Order`는 최종 상태와 여섯 이벤트의 종류·전달 데이터·순번을 함께 검증한다. 빗나간 경우에는 적중 후 효과를 건너뛰지만, 대상 기본 판정 이후의 단계는 실행한다.

## 공개 구현의 핵심

C40·C41 등의 번호는 원본 규칙 문서와 테스트를 연결하는 항목 번호다. 각 항목에서 실제로 검사하는 조건은 다음과 같다.

- **C40 — 독립 DoT 스택:** 출혈·중독의 피해량과 남은 턴을 각각 보존하고, 활성 스택의 피해를 합산한다. 지속시간이 다른 스택을 합쳐 버리지 않는다.
- **C41 — 소유자 턴 수명:** 상태는 전체 라운드가 아니라 해당 유닛의 행동 종료에 맞춰 줄어든다. 시간제 상태 효과는 현재 행동 종료에 대한 `Turns + 1` 보정을 적용하며, 생성자에서 정수 표현 범위를 넘을 수 있는 기간을 거절한다.
- **C51 — 선택 치료:** 지정한 출혈 또는 중독의 모든 스택만 제거한다. 다른 상태는 유지하며, 제거한 스택이 없으면 치료 이벤트를 발행하지 않는다.
- **A04/C89 — 같은 입력의 결과 재현:** 위 효과들은 난수를 소비하지 않는다. 기본 판정이 같은 난수를 소비하도록 구성한 두 실행에서, 효과 추가 전후의 난수 생성기(RNG) 내부 상태와 다음 난수까지 비교한다.

## 원본 코드와 실행용 주변 코드

발췌 기준은 Abyss 원본 커밋 `ca3674f1e782b1512db07636495fe83a923fc35a`다.

| 공개 파일 | 출처와 정리 범위 |
|---|---|
| `src/Combat/SkillEffects.cs` | `Core/Rules/Combat/SkillEffects.cs`의 enum, 추상 클래스, `DamageOverTimeSkillEffect`, `TimedStatusSkillEffect`, `CureSkillEffect`를 발췌했다. 생성자와 `Apply` 로직은 유지하고 namespace·using·설명 주석만 정리했다. |
| `src/Combat/Statuses.cs` | `Core/Rules/Combat/Statuses.cs`의 상태 저장·만료·제거 구현이다. namespace와 설명 주석만 정리했다. |
| `SkillEffectPipeline.ApplySkillEffects` | `Core/Rules/Combat/BattleEngine.cs`의 실제 분배 메서드다. 이 표본에서 사용하지 않는 소환 포트 인자 하나를 제외했다. |
| `SkillEffectPipeline.Execute`, `SampleBattleModel.cs` | 선택한 효과를 Unity 없이 실행하기 위한 축소 드라이버·주변 모델이다. 기본 공격·치유 판정은 `IPrimaryResolver`로 주입하며, 게임 전체의 반격·생명 상태 전이·가드·공유 체력 처리는 포함하지 않는다. |
| `tests/CombatEffectTests.cs` | 원본 `SkillEffectExecutionTests.cs`, `AdvancedSkillEffectTests.cs`의 규칙 계약을 바탕으로 작성한 공개 샘플용 테스트다. 다중 대상 단계 확인에는 테스트 전용 효과를 사용한다. |

## 실행과 검증

저장소 루트에서 .NET 10 SDK로 실행한다. 외부 NuGet 패키지는 필요하지 않다.

```powershell
dotnet run --project Abyss.Portfolio.csproj -c Release
```

전투 표본의 8개 테스트는 효과·단계·목록 순서, 명중 실패, 선택 치료와 재호출, 서로 다른 스택의 만료, 다중 대상 후 1회 실행, 난수 생성기 상태 보존, 입력 검증 전 상태 변경 방지를 다룬다. 전체 공개 테스트 구성은 [검증 문서](testing.md)에서 확인할 수 있다.

[저장소 소개로 돌아가기](../README.md)
