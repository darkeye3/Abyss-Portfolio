# 영웅 설정: UI 명령과 저장 실패 복구

전투 기술 선택과 장신구 교체는 플레이 화면, 캠페인 상태, 저장 파일에 함께 반영되어야 한다. 영웅 설정 기능을 Unity 입력, 게임 진행을 조정하는 Application 명령, 화면 표시용 데이터를 모은 조회 모델로 나누고, 저장에 실패하면 메모리의 설정도 이전 상태로 되돌리도록 구현했다.

![영웅 상태창](../images/hero-details.png)

## 실제 소스 읽기 순서

| 순서 | 공개 소스 | 확인할 구현 |
|---|---|---|
| 1 | [HeroDetailsView.cs](../excerpts/unity/HeroDetailsView.cs) | 버튼 콜백 연결, 사용 가능 여부 표시, 상세 상태를 화면에 반영 |
| 2 | [HeroDetailsCommandFlow.cs](../excerpts/unity/HeroDetailsCommandFlow.cs) | 선택 순서를 유지한 기술 토글, Application 호출, 명령 후 재조회 |
| 3 | [HeroConfigurationCommands.cs](../excerpts/application/HeroConfigurationCommands.cs) | 설정 조건 검사, 변경 전 상태 보관, 저장과 실패 복구 |
| 4 | [HeroConfigurationQueries.cs](../excerpts/application/HeroConfigurationQueries.cs) | 실제 명령과 같은 검사로 버튼 활성화 여부와 거절 이유 제공 |
| 5 | [HeroConfigurationTests.cs](../excerpts/application/HeroConfigurationTests.cs) | 선택 저장 왕복, 잘못된 명령의 상태 불변, 저장 실패 복구 검증 |

공개 파일은 Abyss 실제 소스의 **완전한 메서드 본문을 선별한 발췌본**이다. 메서드 내부 구현을 그대로 보존하고, 관계없는 필드·보조 메서드·도메인 타입·테스트 준비 코드는 생략했다. 따라서 `excerpts/`는 코드를 검토하는 자료이며, 저장소의 독립 실행 프로젝트에는 포함되지 않는다. 실행 가능한 샘플과 테스트는 `src/`, `tests/`에 있다.

## 입력과 규칙 검사를 연결한 방식

`BindSkills`는 기술 ID와 전투·야영 구분을 콜백에 전달한다. `ToggleHeroDetailSkill`은 조회 모델에서 현재 장착 목록을 가져와 슬롯 순서로 정렬하고, 클릭한 기술을 추가하거나 제거한 목록을 `SetCombatLoadout`에 전달한다. 영웅은 화면 배열 인덱스가 아닌 `RosterId`로 지정한다.

사용 가능 여부는 `ReadCombatSkills`에서 후보 목록을 만들고 `content.CanSetCombatLoadout`을 호출해 구한다. 실제 변경 경로의 `content.TrySetCombatLoadout`도 같은 검사를 사용한다. UI가 비활성화되어 있어도 명령 경계에서 다시 검사하므로, 마지막 기술 해제·중복 선택·다른 클래스의 기술은 상태를 바꾸기 전에 거절된다.

장신구 UI에는 슬롯별 `CanEquip`과 거절 이유를 제공한다. 선택한 슬롯에 따라 버튼 상태를 갱신하고, `EquipHeroTrinket`에서 실제 소유 수량과 장착 조건을 검사한다. 기술과 장신구 모두 진행 중인 원정이나 대기 상태가 아닌 영웅에 대한 변경을 거절한다.

## 저장 실패에 대응한 변경 경계

```text
SetCombatLoadout
  → 기존 SelectedIds와 IsInitialized 보관
  → 검증된 선택 적용
  → SaveHeroConfiguration
       저장 성공: 성공 결과 반환
       저장 실패: 기존 선택과 초기화 여부 복원 후 실패 결과 반환
  → ReadHeroDetails → ApplyState
```

장신구 교체는 기존 슬롯의 ID와 보관함 수량을 별도 복사본(스냅샷)으로 보관한다. 저장 실패 시 `RestoreTrinketConfiguration`이 두 상태를 복구해, 슬롯만 바뀌거나 보관함 수량만 차감된 상태가 남지 않게 한다. `TrinketStash.Counts`는 원본과 분리해 복사한 읽기 전용 사전이므로 이후 장착 처리에 의해 이전 수량이 바뀌지 않는다.

이 Application 경계는 메모리 변경의 실패 복구를 맡는다. 저장 파일 교체와 손상 복구는 별도의 [저장 설계](save-atomicity.md)에서 다룬다.

## 결과를 화면에 반영하는 방식

`ApplyHeroDetailCommand`는 명령 결과를 받은 뒤 `ReadHeroDetails`로 최신 상태를 다시 읽는다. 성공과 실패 모두 이 상태를 `ApplyState(..., preserveScroll: true)`에 적용하고, 결과 메시지를 표시한다. 실패했다면 복원된 상태가 화면에 반영된다.

상세창의 스크롤과 선택한 장신구 슬롯은 유지한다. 보유 영웅 목록에서는 해당 영웅의 `HeroSlotView.SetData`만 갱신한다. 초상 오브젝트를 유지하기 때문에 상세창을 닫을 때 돌아갈 입력 포커스가 사라지지 않는다.

## 회귀 테스트에서 확인하는 조건

공개한 테스트는 원본 프로젝트에서 사용하는 다음 두 사례의 본문이다.

- `H04_H21_S06_전투야영선택은_저장되고_아이콘순서는_고정된다`: 전투·야영 선택 순서를 저장 후 재개해 비교하고, 빈 선택·중복·미등록 기술 거절 후 전체 상태가 유지되는지 검사한다. 조회와 설정이 난수 상태를 소비하지 않는지도 비교한다.
- `S07_H09_설정저장실패는_장착수량과_선택을_복원한다`: 저장 경로가 없는 세션으로 실패를 유도하고, 설정 전후 캠페인 직렬화 결과를 비교한다. 장신구 슬롯·보관함 수량, 전투·야영 선택과 미초기화 플래그까지 복구되는지 검사한다.

`Capture`는 `SaveMapper.Capture`와 `SaveCodec.Encode`로 비교할 상태를 만든다. 공개 테스트의 준비 도우미와 전체 콘텐츠는 원본 프로젝트에 있으므로, 독립 실행 테스트 수에는 이 발췌본을 합산하지 않는다.

## 원본과의 대응

기준 커밋: `ca3674f1e782b1512db07636495fe83a923fc35a`. 아래 번호는 비공개 원본의 줄 번호이며, 공개 파일의 각 메서드 위에도 기록했다. 선별한 20개 메서드 본문은 원본과 대조했다.

| 원본 파일 | 발췌 범위 |
|---|---|
| `Assets/Abyss/Presentation/UI/EstateHeroDetailsWindowView.cs` | `SetActions` 236–244, `ApplyState` 246–306, `BindSkills` 333–349, `BindTrinkets` 351–400 |
| `Assets/Abyss/Presentation/UI/EstateScreen.HeroDetails.cs` | 기술 토글·장신구 명령·명령 결과 반영 51–106 |
| `Core/Application/EstateApplicationService.HeroConfiguration.cs` | `SetCombatLoadout`·`EquipHeroTrinket` 10–43, 복구·저장·상태 검사 62–112 |
| `Core/Application/EstateApplicationService.HeroDetails.cs` | `ReadCombatSkills` 91–113, `ToggleSkill` 137–145 |
| `Core/Tests~/HeroConfigurationTests.cs` | 선택 저장 테스트 129–160, 저장 실패 테스트 162–186, `Capture` 349–353 |

버튼 입력과 처리 함수의 실제 연결은 원본 `EstateScreen.cs` 1103줄의 `heroDetailsWindow.SetActions(ToggleHeroDetailSkill, EquipHeroDetailTrinket, UnequipHeroDetailTrinket)`에서 이뤄진다.

[Unity 연동과 개발 도구](unity-tooling.md) · [저장소 소개](../README.md)
