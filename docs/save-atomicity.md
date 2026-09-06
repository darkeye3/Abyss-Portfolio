# 저장 실패와 복원 실패를 다루는 방법

저장 시스템은 파일을 안전하게 쓰는 문제와, 읽은 데이터를 게임 상태에 안전하게 적용하는 문제를 함께 다룬다. Abyss에서는 임시 파일 교체와 복원 전 검증을 별도 단계로 구성했다.

## 파일 교체

정식 파일을 먼저 삭제하면 새 파일을 쓰는 도중 종료될 때 저장을 잃을 수 있다. 임시 파일에 기록하고 디스크 반영을 요청한 다음, 기존 파일이 있으면 `File.Replace`로 정식 파일과 백업을 교체한다. 최초 저장에는 `File.Move`를 사용한다.

아래는 `SaveStore.Save`의 핵심 발췌다. 직렬화, 경로 준비, 보조 메서드 구현과 교체 후 백업 정리는 생략했다. 원본 메서드의 일부이므로 독립 실행 예제는 아니다.

```csharp
using (FileStream stream = new FileStream(
    temporary, FileMode.Create, FileAccess.Write, FileShare.None))
using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
{
    writer.Write(text);
    writer.Flush();
    stream.Flush(true);
}

if (File.Exists(target))
{
    ClearReadOnly(target);
    ClearReadOnly(backup);
    File.Replace(temporary, target, backup, true);
}
else
    File.Move(temporary, target);
```

읽을 때 정식 파일이 없거나 저장 형식 오류로 읽을 수 없으면 백업을 시도한다. 정식 파일 교체가 끝난 뒤 백업 정리에 실패한 경우에는 저장 실패로 되돌리지 않는다. 이미 새 파일이 반영된 뒤 메모리 상태만 롤백되는 불일치를 방지하기 위한 구분이다.

## 복원 전 검증

콘텐츠의 개정 번호(revision), 규칙 설정 식별값, 진형 크기와 예약·장착 정보의 정합성을 먼저 검사한다. 규칙 설정 식별값은 저장 당시의 수치·설정이 현재 게임과 호환되는지 비교하는 값이다. 구버전 저장을 변환할 때도 당시 값을 고정해 보관한다. 프로젝트에서 **‘동결 서명’**이라고 부르는 이 값은 보안용 전자서명이 아니라 저장 호환성을 판정하는 기준이다.

진행 전투는 유닛 ID, 진형, 남은 행동과 상호 참조를 검증한 뒤 상태를 적용한다. 전투·전리품 등 용도별로 분리한 난수 수열을 **난수 스트림**이라고 한다. 스트림별 내부 상태와 행동 순서를 그대로 복원해 재추첨을 방지한다.

| 검증 사례 | 확인하는 결과 |
|---|---|
| 정식 파일 손상, 유효한 백업 존재 | 백업으로 읽기 |
| 백업 파일만 존재 | 저장 슬롯으로 인식 |
| 콘텐츠·규칙 설정 불일치 | 적용 전 거절 |
| 잘못된 예약·장착·전투 참조 | 검증 대상 상태를 변경하지 않고 거절 |
| 저장 후 이어 하기 | 난수 상태와 진행 순서 유지 |

근거 파일: `SaveStore.cs`, `SaveMapper.cs`, `BattleSnapshotMapper.cs`, `SaveTests.cs`, `EngineRulesSaveTests.cs`, `BattleSaveTests.cs` — 비공개 원본에서 선별했다.

[저장소 소개로 돌아가기](../README.md)
