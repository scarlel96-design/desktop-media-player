# Bare catch 분류 메모 (7건 미분류 catch)

- 날짜: 2026-10-22
- 기준 커밋: ee6b76e (코드 기준선 e099c2e)
- 결정: 총괄 S134 LOCK 22:00 (수석 제안 기반)
- 코드 변경: 0줄 (문서 전용)
- Windows 실기 검증: NOT TESTED

## 배경

- Phase A Soft 로깅 라인(S118–S132)은 종료되었다.
- 의도적 무로그 catch 3건은 이미 결정됨:
  - `App.xaml.cs` L73
  - `MainWindow.xaml.cs` L274
  - `PlaybackFacade.cs` L332 (B-3 HOLD)
- 본 메모는 남은 bare catch 7건을 분류한다.

## 분류표

| 파일 | 라인 | 보호 대상 | 분류 | 근거 |
|---|---|---|---|---|
| SpikeLogger.cs | L44 | `Log()` 내 파일 writer `_writer.WriteLine(line)` I/O 실패 | 영구 유지(의도적 무로그) | 로거 자기 자신, 로그 추가 시 재귀/연쇄 실패 위험 |
| SpikeLogger.cs | L54 | `Log()` 내 `Console.WriteLine(line)` (콘솔 미가용 호스트) | 영구 유지(의도적 무로그) | 로거 자기 자신, 로그 추가 시 재귀/연쇄 실패 위험 |
| SpikeLogger.cs | L82 | `Dispose()` 내 `_writer.Dispose()` 실패 | 영구 유지(의도적 무로그) | 로거 자기 자신, 로그 추가 시 재귀/연쇄 실패 위험 |
| MainWindow.xaml.cs | L812 | `SoftCopyTimestamp()`의 `_facade.GetPosition()` 읽기 실패 | 유지 | 위치·길이 읽기 실패 시 return하는 무해한 no-op |
| MainWindow.xaml.cs | L823 | `SoftCopyTimestamp()`의 `Clipboard.SetText` + OSD 표시 | 향후 로그 대상(이번엔 기록만) | 다른 클립보드 catch 4건과 같은 key `clipboard_copy_failed` 적용 예정, 별도 LOCK 필요 |
| MainWindow.xaml.cs | L832 | `SoftCopyDuration()`의 `_facade.GetDuration()` 읽기 실패 | 유지 | 위치·길이 읽기 실패 시 return하는 무해한 no-op |
| MainWindow.xaml.cs | L843 | `SoftCopyDuration()`의 `Clipboard.SetText` + OSD 표시 | 향후 로그 대상(이번엔 기록만) | 다른 클립보드 catch 4건과 같은 key `clipboard_copy_failed` 적용 예정, 별도 LOCK 필요 |

## 요약

- bare catch 총 10건 = 기결정 3건 + 본 메모 7건
- 본 메모 7건: 유지 5건 (SpikeLogger 3 + MainWindow 2), 향후 로그 대상 2건
- L823/L843 로그 추가는 별도 LOCK 이후 진행하며, 본 메모로는 코드를 변경하지 않는다.
