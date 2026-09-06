# 게임 규칙과 Unity 표현 계층 분리

Abyss는 Unity 기반 턴제 던전 RPG다. 전투·탐험·성장 규칙을 일반 C# 어셈블리에 두고, Unity는 상태 표시와 입력 연결을 맡도록 구성했다. 동일한 규칙을 콘솔 실행과 Unity 클라이언트에서 사용한다.

## 설계한 경계

| 계층 | 책임 | 직접 참조하는 Core 계층 |
|---|---|---|
| Rules | 전투 판정, 진형, 상태 효과, 탐험·성장 계산 | 없음 |
| Infrastructure | 저장 파일 입출력, 직렬화 | Rules |
| Content | JSON 검증, 콘텐츠 정의, 게임별 설정 조립 | Rules, Infrastructure |
| Application | 캠페인 세션, 원정·영지 명령, 화면용 조회 | Rules, Infrastructure, Content |
| Unity Presentation | 씬 전환, UI 표시, 플레이어 입력 | Rules, Content, Application |

위 표는 실제 어셈블리의 직접 참조 관계다. 화면의 명령·조회는 Application 서비스를 거친다. Presentation은 ID·enum을 위해 Rules를, 문자열 테이블을 위해 Content를 참조한다. 저장 파일 처리를 담당하는 Infrastructure는 직접 참조하지 않는다.

## 해결한 문제

콘솔과 Unity에 캠페인 진행 코드가 각각 있으면 경험치·전리품·귀환 처리가 서로 달라질 수 있다. 실행 조정을 Application에 모으고, Unity의 `GameSession`은 씬 사이에서 `CampaignSession`을 유지하는 호스트로 제한했다.

규칙의 수치와 콘텐츠도 분리했다. `CombatRules`, `HeroRules`, `ExpeditionRules` 같은 불변 설정을 주입하고, Abyss의 기본 수치는 Content에서 선택한다. 공통 전투 계산을 검증하면서도 게임별 설정을 바꿀 수 있는 구조다.

## 경계를 유지하는 방법

- Rules에서 Unity API, 파일 입출력, 벽시계와 직접 생성한 난수원을 사용하지 않는다. 난수는 인터페이스로 주입한다.
- `csproj`와 `asmdef`의 직접 참조 집합을 검증해 계층 역참조를 탐지한다.
- Core를 Unity 없이 빌드·실행하고, Unity에서는 서비스 연결과 UI 동작을 검사한다.

설계의 핵심은 변경 이유를 나누는 것이다. 전투 판정 수정은 Rules, 데이터 추가는 Content, 화면 구성 수정은 Presentation에서 다룰 수 있도록 책임을 구분했다.

근거 파일: `Rules.csproj`, `Infrastructure.csproj`, `Content.csproj`, `Application.csproj`, `Abyss.Presentation.asmdef`, `CampaignSession.cs`, `verify.ps1` — 비공개 원본에서 설계 근거를 선별했다.

[저장소 소개로 돌아가기](../README.md)
