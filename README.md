# ABYSS | Game Programming Portfolio

**Abyss는 Darkest Dungeon(다키스트 던전)의 전투·탐험·영지 운영 시스템을 분석하고,
Unity와 C#으로 재구현한 학습·포트폴리오 프로젝트입니다.**

원작은 Red Hook Studios의 [Darkest Dungeon](https://www.darkestdungeon.com/darkest-dungeon/)입니다.
원작의 게임 규칙과 콘텐츠 구성을 바탕으로, C# 규칙 계층·저장 복원·Unity UI를 구현한 과정과
문제 해결 사례를 소개합니다. 주요 코드와 독립 실행 테스트를 함께 제공합니다.
규칙 정리에는 기존 Unity 재현 프로젝트를 분석한 역기획 문서를 참고했습니다.

**[포트폴리오 PDF 보기](portfolio/Abyss_Game_Programmer_Portfolio.pdf)**

![Abyss 전투 화면](images/combat.png)

## 빠르게 살펴보기

| 확인할 역량 | 코드·자료 | 핵심 내용 |
|---|---|---|
| 시스템 설계 | [아키텍처](docs/architecture.md) | 순수 C# 규칙과 Unity 화면의 책임 분리 |
| 전투 콘텐츠 구현 | [효과 구현](src/Combat/SkillEffects.cs) · [실행기](src/Combat/SkillEffectPipeline.cs) · [읽기 안내](docs/combat-effects.md) | 효과 기반 클래스·구체 효과·단계 실행기·통합 테스트 |
| Unity와 게임 로직 연결 | [입력 처리](excerpts/unity/HeroDetailsCommandFlow.cs) · [설정 명령](excerpts/application/HeroConfigurationCommands.cs) | 버튼 입력 → 명령 검증 → 저장·실패 복구 → 화면 갱신 |
| 자료구조·알고리즘 | [그래프 탐색 샘플](src/GraphSearch.cs) | BFS 최단 거리와 안정적인 동률 처리 |
| 결정론·재현성 | [난수 스트림](src/RandomStreams.cs) | 7개 시스템의 난수 소비 격리와 상태 복원 |
| 저장 안정성 | [저장·복원 사례](docs/save-atomicity.md) | 사전 검증과 원자적 파일 교체 |
| 개발 도구 | [자동화 사례](docs/unity-tooling.md) | 씬 생성기와 JSON 콘텐츠 검증 |
| 테스트 | [검증 구성과 코드](docs/testing.md) | 효과 실행·난수 비간섭·저장 재개·그래프 경계 조건 검증 |

## 기술과 구현 범위

- **게임:** C#, Unity 6, URP, uGUI, Input System
- **설계:** 계층 분리, 불변 조회 모델, 명령 서비스, 타입 기반 스킬 효과
- **알고리즘:** BFS, Queue·Dictionary 기반 탐색, PCG32 기반 난수 스트림
- **도구:** Git, JSON, PowerShell, .NET, NUnit 및 자체 테스트 하네스
- **AI 활용:** Codex를 코드 작성·리팩터링·테스트 작성에 활용하고 규칙 명세와 실행 결과로 검증

Abyss에서는 원작의 영웅 편성·보급, 던전 탐험과 전투, 귀환·성장·영지 운영 흐름을 재구현했습니다.
이 저장소는 채용 검토에 필요한 **코드 샘플과 설명 자료**를 공개한 별도 저장소입니다.

## 실행

필요 환경: **.NET SDK 10.0**. `src/`의 독립 샘플과 `tests/`는 Unity나 외부 NuGet 패키지 없이 실행합니다.

```bash
dotnet run --project Abyss.Portfolio.csproj -c Release
```

성공하면 테스트별 결과와 전체 통과 수가 출력됩니다. 실패한 검사는 0이 아닌 종료 코드를 반환합니다.

## 공개 코드의 구성

기술 문서의 **'원본 코드'는 발췌 기준인 Abyss 전체 프로젝트 코드**를 의미합니다.

- 난수 관련 세 파일은 Abyss의 구현을 발췌하고 제출용으로 주석을 정리했습니다.
- `GraphSearch`는 원본 던전 생성기의 BFS·보스방 동률 선택 로직을 독립 인접 목록 API로 추출한 샘플입니다.
- 전투 효과는 원본의 핵심 효과 구현과, 실행 흐름을 확인할 수 있도록 주변 모델을 축소한 독립 샘플을 제공합니다. 원본과의 대응은 [전투 효과 문서](docs/combat-effects.md)에 정리했습니다.
- `excerpts/`는 Unity 입력 처리, Application 명령, 저장 실패 복구와 관련 테스트의 **원본 소스 발췌**입니다. 생략한 게임·Unity 타입을 참조하므로 위 실행 프로젝트에서는 제외합니다. [읽는 순서](docs/hero-configuration.md)를 따라 계층 간 흐름을 확인할 수 있습니다.
- 저장 파일 교체와 복원 검증은 [저장·복원 사례](docs/save-atomicity.md)에 설명했습니다.
- 독립 테스트는 이 공개 샘플에 맞게 구성했습니다. 전체 게임의 테스트 개수와 구분됩니다.
- 게임 전체 Core, 캠페인·콘텐츠 데이터, Unity 실행 프로젝트와 원본 개발 이력은 포함하지 않습니다.

## 프로젝트 화면

### 원정 지도

![던전 선택과 파티 편성](images/embark.png)

### 영웅 상태창

![능력치·기술·장비 조회](images/hero-details.png)

화면은 Unity UI 렌더 캡처입니다. 시각 자산에는 생성형 이미지 도구로 제작·후처리한 자산이 포함됩니다.

## 이용 범위

채용 평가와 코드 검토를 위한 자료입니다. 열람·로컬 실행 범위 및 제품 이용 제한은
[LICENSE.md](LICENSE.md)를 참고해 주세요. 원작 게임과 PCG 알고리즘 출처는
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)에 기재했습니다.
