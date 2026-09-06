# Unity 개발 도구와 디버깅 사례

반복되는 씬 배선과 콘텐츠 오류를 도구로 확인하고, 화면 버그는 데이터가 표시되기까지의 경로를 추적해 수정했다.

## 씬·프리팹 생성 도구

`BattleSceneBuilder`가 실제 게임에 사용하는 Start·Loading·Estate·Dungeon 씬과 공통 UI 프리팹을 생성한다. 생성 절차를 C#으로 관리해 UI 계층과 참조 연결의 변경을 코드 차이로 검토할 수 있다.

씬별 구성을 partial 파일로 나누고 공통 컨트롤을 재사용한다. 생성 결과는 EditMode 테스트에서 컴포넌트·참조·표시 상태를 확인하며, 화면 캡처로 해상도별 배치를 검사한다. 콘텐츠 원본 JSON을 실행 위치로 복사하는 과정도 빌드 스크립트에 포함했다.

## 콘텐츠 오류의 위치까지 표시

콘텐츠 로더는 필수 필드, 타입·범위, 중복 ID와 교차 참조를 검사한다. 알 수 없는 필드나 명시적으로 잘못 입력한 값은 기본값으로 대체하지 않는다.

예를 들어 버프의 스탯 참조 오류는 `buffs.json`과 `$.buffs[0].stat`처럼 파일명·배열 인덱스·필드 경로를 함께 전달한다. 콘텐츠 작성자가 문제가 발생한 항목을 바로 찾을 수 있도록 진단 정보를 구성했다.

## 사례: 사망한 적의 HP가 1로 보이는 문제

| 단계 | 내용 |
|---|---|
| 증상 | 사망한 적이 `HP 1/28`로 표시됨 |
| 원인 | 시체 전환 뒤 `CurrentHealth`는 시체 내구도인데, UI가 생전 최대 HP와 조합함 |
| 수정 | 조회 모델에 `LivingHealth`, `CorpseHealth`, `CorpseMaxHealth`를 구분 |
| 결과 | 사망 HP는 `0`, 시체 내구도는 별도 행의 `1/1`로 표시 |

Rules의 시체 판정을 유지하면서 Application의 조회 의미와 UI 바인딩을 수정했다. 시체 그림은 Unity `Image`로 연결하고, 1칸·2칸 적과 살아 있는 유닛으로의 슬롯 재사용을 함께 처리했다.

관련 검증에서는 시체 그림의 표시·숨김, 슬롯 재사용, 실제 렌더 픽셀 차이와 1280×720·1920×1080 배치를 확인했다. 미래 턴 목록은 Editor·Development Build에서만 표시하도록 빌드 정책도 분리했다.

## AI 활용

생성형 AI(Codex)를 코드 작성·리팩터링·테스트 작성에 활용했다. 변경 내용은 규칙 문서와 실제 코드에 대조하고, Core 실행 검사와 Unity 테스트·화면 렌더 확인으로 검증하는 방식으로 개발했다.

근거 파일: `BattleSceneBuilder.cs`, `BattleSceneBuilder.Dungeon.cs`, `ContentSchemaValidator.cs`, `FightView.cs`, `DungeonCorpseGraphic.cs`, `CorpseArtImportTests.cs`, `build-unity.ps1`, `verify.ps1` — 비공개 원본에서 설명에 필요한 내용만 선별했다.

[저장소 소개로 돌아가기](../README.md)
