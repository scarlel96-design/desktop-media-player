# Known Issues — KI-2026-09-05-043

| Updated | 2026-10-20 02:30 KST (이전 헤더 2026-09-05 04:58 KST, 이력 보존) |

| ID | Sev | Issue | Status |
|----|-----|-------|--------|
| KI-031 | Low | 고해상도 영상에서 시크 후 약 1초 끊김 (Known Limitation) | **Open (Known Limitation)** — L1 사용자 관찰 · 정밀(hr) 시크 비용 의심, 원인 미검증(NOT VERIFIED) · PR #1 merge 차단 아님 |
| KI-030 | Low | 재생바 버퍼링 바 오표시 | **RESOLVED** — Windows PASS 2026-10-22 (`BLD-20261021-PHASEA-S132-001` / `e099c2e`) |
| KI-029 | Low | F2 전체화면 하단 레터박스 (사용자 피드백 2026-10-03) | **Open (등록)** — 수정 대상 S77(수석 확정 분석) · 원인 가설 미검증 · Windows 확정 없음(사용자 진술) |
| KI-028 | Med | F1 스페이스바가 키보드 포커스된 버튼을 동작시킴 | **Open (등록)** — 수정 대상 S76 · 사용자 진술, 로그/재현 정보 없음 · Windows 확정 없음 |
| KI-027 | **High** | frame-drop 초당 약 10~20 지속 (S69~S72 로그에서 반복, decoder/vo drop 0) | **Open · 고위험** — 원인 미확정(수석: VO 단계 누적 드롭, 가설 H1~H3 = 가설) · Windows 우선 항목이나 03:45 지시로 정기 테스트 없음, 필요 시 USER WINDOWS TEST REQUIRED |
| KI-026 | Med | EOF 후 Play 버튼이 재시작하지 않음 (S72 사용자 진술 + 로그: eof_reached state=Paused 이후 Playing/Paused 전이만) | **Open (등록)** — 수정 대상 S75 · 원인은 keep-open=yes 정황(가설) · 로그에서 영상 진행 증거 없음 |
| KI-025 | Info | EOF에서 `end_file` 로그 없음 (S68 기대 reason=eof와 불일치) | **문서 정정 항목 성격** — 수석 확정 분석: keep-open=yes에 의한 정상 동작, 로그 기대 오류. 단 keep-open 값은 로그로 확인되지 않음(S72 eof_reached state=Paused 정황 일치) · Windows 확정 없음 · 삭제 없이 보존 |
| KI-024 | Med | Seek/Timeline drag stuck | **Open** — Soft SUCCESS Soft (`04b90b0`/`HOTFIX-002`) · Windows Seek-drag 실기 대기 · Soft≠PASS · **[정정 2026-10-03: 사용자 S71 진술(03:07) "정상" = 진술만, 로그 증거 일부(seek id=20 ×9, 일시정지 seek 포함)이나 Windows PASS 아님, 상태는 WINDOWS PENDING 유지]** |
| KI-023 | Soft | Usage YELLOW→ORANGE ≈28% · Missed Deadline ~11m UNKNOWN | **Open Soft** — Safe Stop prep · hotfix-only window |
| KI-022 | High | Windows FAILED-UI-BLANK (`e86f8d8`→HOTFIX `564006c`) | **Closed (mitigated Soft)** — chrome visible on HOTFIX-001 retest · Soft≠PASS · **[정정 2026-10-03: 이 행의 Closed(mitigated)는 2026-09-18 Status refresh 이후 WINDOWS RE-VALIDATION PENDING으로 대체됨(원문 보존). 사용자 S71 진술(03:07) "정상" = 진술만, 로그 증거 없음, PASS 아님]** |
| KI-021 | Soft | Usage Missed Deadline + sharp burn (GREEN→YELLOW · BOT_INVOCATION_FAILED) | **Open Soft** — ops signal · not product defect |
| KI-020 | Blocker | USAGE_RED HALT (~3% · SAFE STOP) | **Closed (mitigated)** — RESUMED Preflight PASS · GREEN 99% · 2026-09-09 ~22:12 KST · USAGE-HALT-20260906-1338 |
| KI-019 | Soft | `_positionGate` FanOut 재진입 주의 (S13 `1d00642`) | **Open Soft** — Soft note only · Atomic SUCCESS 유지 · Gate FAIL 아님 |
| KI-017 | Med | S7 handlers CS0103 | **Closed** (`9b09e10`) |
| KI-018 | Med | S7 CS1039 MediaInfoText | **Closed** (`9b09e10` / `BLD-20260905-PHASEA-006`) |
| KI-013 | Med | UX debt D1–D5 | Open |
| KI-014 | Soft | frame-drop | Open Soft |
| KI-016 | Soft | CS0108 | Open Soft |
| KI-001 | Med | 벤치 없음 | Open |
| KI-002 | High | 법무 | Open |
| KI-004 | High | License Compliance Gate | Open |

## Soft note (2026-09-06 13:32 KST)

- KI-019: S13 통합 Soft OK 시 Soft note — `_positionGate` FanOut 재진입 주의
- SoT: **KI Soft만** · Atomic SUCCESS 재오픈 금지 · Next=S14

## USAGE HALT RED (2026-09-06 13:38 KST)

- KI-020: USAGE_RED (~3% remaining · user-confirmed)
- ACTION: SAFE STOP · Phase A wake/확대 금지
- HOLD: S14 `eba8326` · Atomic SUCCESS/S15/next Atomic **보류**
- Resume: Preflight + ≥25–30% + 총괄 · `USAGE-HALT-20260906-1338`

## USAGE RESUME (2026-09-09 22:13 KST)

- KI-020 **Closed (mitigated)**: `USAGE-HALT-20260906-1338` RESUMED · Preflight PASS · GREEN 99% · obs 2026-09-09 ~22:12 KST · reset Sep 16
- Continuity ACTIVE · S14 `eba8326` resume · next=빌드 smoke · S1–S13 재실행 없음


## Usage signal (2026-09-10 00:01 KST)

- KI-021 Soft: Missed Deadline +34m (`BOT_INVOCATION_FAILED`) · ΔR −38pp/~94m · band YELLOW 45%

## Usage Missed Deadline #2 (2026-09-10 01:17 KST)

- KI-021 Soft continued: 2nd Missed Deadline +23m vs ~00:53 (`BOT_INVOCATION_FAILED`) · R=42% YELLOW · Next ~01:46


## Windows FAILED-UI-BLANK (2026-09-10 02:29 KST)

- KI-022: Shell window opens · content empty light-gray · no player chrome
- tip `e86f8d8` / `BLD-20260906-PHASEA-018` · ZIP `08712d7329360cf24c68eeb256f12cd4dc65bf95d69e27c1ce4f72fb3b297134`
- Evidence: screenshot in chat · RCA pending
- Freeze: blank-UI RCA/fix만 허용 · S19 HOLD

## KI-022 Evidence+ (2026-09-10 02:33 KST)

LocalAppData prefs+spike.log present · no MessageBox · supports H1 airspace (not crash/missing runtime).

## KI-022 spike.log (2026-09-10 02:34 KST)

native_loaded + after_init OK · no ui_unhandled · supports H1 (not native.missing this run).


## Usage ORANGE (2026-09-10 02:39 KST)

- KI-023 Soft: YELLOW→ORANGE R≈28% · ΔR −10pp/~41m · Missed ~11m UNKNOWN · SAFE STOP not yet · KI-022 hotfix-only then hard freeze

## HOTFIX-001 Windows retest (2026-09-10 02:45 KST)

- KI-022: **mitigated Soft** (chrome visible) · Soft≠PASS
- KI-024: Seek/Timeline drag stuck · Open · fix after band recovery
- UI polish: Phase A Soft defer · Blur/P1 금지
- Evidence: chrome visible · timeline `--:--`

## Usage sharp burn (2026-09-10 02:51 KST)

ORANGE 18% · ΔR −10pp/~12m · Missed +2m · hard freeze · RED≤5% SAFE STOP prep (KI-023 Soft continued).

### Missed Deadline log (2026-09-17 00:07 KST)
- 2026-09-16 ~23:36 KST: +7m · BOT_INVOCATION_FAILED · GREEN 97% same band · Next 2026-09-17 ~00:36 KST

### Missed Deadline log (2026-09-17 04:28 KST)
- 2026-09-17 ~04:27 KST: late ~4h · BOT_INVOCATION_FAILED cascade · GREEN 92% · Next 2026-09-17 ~05:27 KST

### Missed Deadline log (2026-09-17 05:42 KST)
- 2026-09-17 ~05:40 KST: late ~13m · BOT_INVOCATION_FAILED (routine ~04:57) · GREEN 88% · Next 2026-09-17 ~06:40 KST
- STOP-CAUSE 후보: Phase A Continuity Watch last fail 2026-09-16 ~17:43

## Status refresh (2026-09-18 02:04 KST)

- **KI-022**: FIX IMPLEMENTED · **WINDOWS RE-VALIDATION PENDING** (blank UI Soft tip `564006c` · not Windows PASS)
- **KI-024**: FIX IMPLEMENTED · **WINDOWS RE-VALIDATION PENDING** (Seek Soft tip `04b90b0` · not Windows PASS)

### Status refresh (2026-09-18 02:55 KST) — Canonical tip `2406143` / `BLD-20260918-INTEGRATE-001`

- **KI-022**: FIX IMPLEMENTED · WINDOWS RE-VALIDATION PENDING (still)
- **KI-024**: FIX IMPLEMENTED · WINDOWS RE-VALIDATION PENDING (still)

### OPEN Soft path (2026-09-18 03:03 KST)

- USER WINDOWS TEST REQUIRED: **path restore Soft** (실기 경로/한글·부재 파일) — S20 Soft Closure와 별도 OPEN
- KI-022 / KI-024: still FIX IMPLEMENTED · WINDOWS RE-VALIDATION PENDING

### OPEN Soft (2026-09-18 03:27 KST)

- USER WINDOWS TEST REQUIRED: **미디어키 실기** (S26 Soft Closure와 별도 OPEN)

### Correction note (2026-10-03 KST, 비서) — 총괄 요청 정정안, 삭제/축소 없음
- 표의 KI-022 'Closed (mitigated Soft)'(HOTFIX-001 retest)는 같은 문서 하단 2026-09-18 Status refresh의 'WINDOWS RE-VALIDATION PENDING'과 충돌 → **후자가 현행**. 전자는 이력으로 보존, 행에 정정 주석 추가.
- 사용자 진술(S71, 2026-10-03 03:07/03:08 및 S72 이후 로그)은 Windows PASS가 아님. 공식 상태 변경은 총괄 판정 전까지 없음.
- 신규 후보(번호 없음, 총괄 판단 대기): (1) EOF에서 `end_file` 로그 없음 — 가설 keep-open, S72 로그에서 `eof_reached state=Paused` 정황 일치(확정 아님). (2) EOF 후 Play 버튼이 재시작하지 않음(사용자 진술 + S72 로그 정황). (3) frame-drop 초당 약 10~20 지속(원인 미확정, decoder/vo drop 0). (4) F1 스페이스바가 포커스 버튼 동작. (5) F2 전체화면 하단 레터박스.

## KI-025~029 등록 (2026-10-03 ~03:58 KST, 총괄 지시: 다음 빈 번호 순서대로)
출처: 사용자 S72 Windows 로그(`S72-WINDOWS-LOG-20261003-0345.md`, sha 08ba6e80…) + 사용자 진술/피드백(총괄 relay). 원인 서술은 모두 **가설**, Windows 확정 아님, PASS 아님.
- **KI-025** EOF `end_file` 없음 — 수석 확정 분석: keep-open=yes의 정상 동작이며 S68 기대(EOF=reason=eof)가 분석 오류. KI라기보다 **문서/로그 기대 정정 항목**. S68 기록은 보존. 로그 근거: S72 `eof_reached pos=25.336397 state=Paused`, end_file 0회.
- **KI-026** EOF 후 Play 비동작 — 03:44:12 Playing → 03:44:13 Paused → 03:44:14 Playing 전이는 있으나 이후 frame 진행 증거 없음(frame-drop-delta=0 ×3). 사용자 진술: 재시작 안 됨. 대상 Atomic S75(미LOCK).
- **KI-027** frame-drop 10~20/s — decoder-drop 0, vo-drop 0, hwdec d3d11va, vo=gpu-next, container 60.0 / display 59.95 fps. 카운터는 pause→play·seek 시 0으로 초기화(delta 음수). 원인 미확정(H1~H3는 수석 가설, 원문 미수령). 고위험 Open.
- **KI-028** F1 스페이스바 → 포커스 버튼 동작 — 대상 S76(미LOCK).
- **KI-029** F2 전체화면 하단 레터박스 — 대상 S77(미LOCK).

## 2026-10-20 ~02:30 KST 갱신 (총괄 판정)
- **KI-030** 재생바에 버퍼링 바처럼 보이는 표시가 있으나, 그 지점 너머를 눌러도 실제 버퍼링은 없음. 시각적 오표시 의심, 재생 영향 없음. 분류 Core UI·UX, 심각도 Low. 상태 OPEN, 수석이 원인 분석 중. 출처: 사용자 S97 Windows 점검 구두 보고(2026-10-20, 로그 없음). 번호 근거: KNOWN_ISSUES와 SoT 전체 검색 결과 사용 중인 최대 번호 KI-029, KI-030 중복 없음.
- **KI-022 / KI-024** 닫지 않음. 상태: OPEN (fix in Soft, Windows 미검증). S97 점검 5항목 PASS(사용자 관찰)는 이 KI의 재현 시나리오를 Windows에서 직접 확인한 증거가 아님(Soft≠Windows PASS). 수석에게 종료 근거 확인 요청.
- **KI-026 / KI-028 / KI-029** 현행 목록에 포함(위 "KI-025~029 등록" 절). Soft에서 고친 기록이 있더라도 Windows 미검증이라 OPEN.
- **KI-027 frame-drop 수치: 출처별 불일치.** 측정값은 frame-drop Atomic에서 다시 잡는다.

| 출처 | 기재 수치 |
|---|---|
| KNOWN_ISSUES KI-027 (S72 로그 기준) | 10~20/s |
| S71 Windows 로그 기록 (총괄 전달) | 약 14~20/s |
| README 초안 S71 | 약 14~26/s |
| CONTINUITY-STATUS-NOW | 18~26/s |
| WINDOWS-RESULT S87/S80 (총괄 기록) | 약 20~25/s |
| README 범위 정렬 초안 (수석) | 7.8~22.3/s |

불일치 — 어느 값도 확정 아님.

## 2026-10-20 ~02:25 KST — KI-022/024 근거 확인 (수석 회신)
- 수석 판단: 닫을 근거 부족, OPEN 유지.
- KI-024 관련 시크 동작: S97 사용자 관찰 정상(간접 증거, 시나리오 재현 아님). REPRO a~d 미재현, 로그·스크린샷 없음.
- KI-022: Windows 증거 없음.
- 종료 조건: 다음 Windows 점검에 REPRO 시나리오를 명시한 항목 포함.

## 2026-10-22 KST 갱신 (S133, 총괄 LOCK)
- **KI-030 RESOLVED** — Windows PASS 2026-10-22, 빌드 `BLD-20261021-PHASEA-S132-001` / commit `e099c2e`.
- **KI-031** (Low, Known Limitation) — 고해상도 영상에서 시크 후 약 1초 끊김. 출처: L1 사용자 관찰. 정밀(hr) 시크 비용 의심, **원인 미검증(NOT VERIFIED)**. PR #1 merge를 막지 않음.

### Pending decision (unclassified bare catches, code untouched)
분류 미정 bare `catch` — 코드 변경 없음, 총괄 판단 대기 (기준 `e099c2e`):
- `src/DesktopMediaPlayer.Platform/SpikeLogger.cs` L44, L54, L82
- `src/DesktopMediaPlayer.Shell/MainWindow.xaml.cs` L812, L823, L832, L843
