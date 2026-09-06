# Unity 연동과 개발 도구

Unity 화면은 게임 진행을 조정하는 Application 계층이 제공한 상태와 명령을 사용한다. 반복되는 씬 구성은 생성 도구로 관리하고, 콘텐츠 오류는 파일과 필드 경로를 포함해 전달한다.

## 영웅 설정: 입력에서 저장까지

영웅 상태창에서 전투 기술을 선택하거나 장신구를 교체하면 다음 흐름으로 처리한다.

```text
Unity 버튼 입력
  → 선택한 영웅 ID·기술 ID·장신구 슬롯을 Application 명령에 전달
  → 현재 상태와 설정 조건 검사
  → 설정 변경 후 저장, 저장 실패 시 이전 상태 복원
  → 최신 상태 재조회 후 상세창·로스터 초상 갱신
```

UI는 기술 개수·클래스 제한을 별도로 계산하지 않는다. 화면 표시용 데이터를 모은 **조회 모델**의 `CanUse`와 거절 이유로 버튼을 표시하고, 실제 명령도 같은 규칙 검사를 거친다. 명령 뒤에는 선택한 장신구 슬롯과 스크롤을 유지하면서 최신 상태를 적용한다. 보유 영웅 목록 전체를 다시 생성하지 않고 해당 영웅 초상만 갱신해 상세창을 닫은 뒤 돌아갈 입력 포커스도 보존한다.

**[영웅 설정 상세 설계와 실제 소스 흐름](hero-configuration.md)**에서 Unity View, 입력 처리, Application 명령, 실패 복구와 회귀 테스트를 연결해 볼 수 있다.

## 씬·프리팹 생성 도구

**게임 씬·프리팹 생성 도구(production builder)**는 실제 게임에 사용하는 Start·Loading·Estate·Dungeon 씬과 공통 UI 프리팹의 계층·참조 연결을 C# 코드로 만드는 Unity 에디터 도구다. `BattleSceneBuilder`가 이 역할을 맡는다. 생성 절차를 코드로 관리하므로 UI 계층과 참조 연결의 변경을 코드 차이로 검토할 수 있다.

하나의 클래스를 여러 파일에 나누는 C# `partial` 기능으로 씬별 구성을 분리하고 공통 컨트롤을 재사용한다. 생성 결과는 Unity 에디터 안에서 실행하는 EditMode 테스트로 컴포넌트·참조·표시 상태를 확인하며, 화면 캡처로 해상도별 배치를 검사한다. 콘텐츠 원본 JSON을 실행 위치로 복사하는 과정도 빌드 스크립트에 포함했다.

## 콘텐츠 오류의 위치까지 표시

`ContentLoader`와 `ContentJsonReader`는 필수 필드, 타입·범위, 중복 ID와 교차 참조를 검사한다. 알 수 없는 필드나 명시적으로 잘못 입력한 값은 기본값으로 대체하지 않는다.

예를 들어 버프의 스탯 참조 오류는 `buffs.json`과 `$.buffs[0].stat`처럼 파일명·배열 인덱스·필드 경로를 함께 전달한다. 콘텐츠 작성자가 문제가 발생한 항목을 바로 찾을 수 있도록 진단 정보를 구성했다.

## AI 활용

생성형 AI(Codex)를 코드 작성·리팩터링·테스트 작성에 활용했다. 변경 내용은 규칙 문서와 실제 코드에 대조하고, Unity 없이 실행하는 게임 코드(Core)의 테스트와 Unity 테스트·화면 렌더 확인으로 검증하는 방식으로 개발했다.

근거 파일: `EstateHeroDetailsWindowView.cs`, `EstateScreen.HeroDetails.cs`, `EstateApplicationService.HeroConfiguration.cs`, `EstateHeroDetailsSceneTests.cs`, `BattleSceneBuilder.cs` 및 씬별 partial 파일, `ContentLoader.cs`, `ContentJsonReader.cs`, `build-unity.ps1`, `verify.ps1`.

[저장소 소개로 돌아가기](../README.md)
