# Closure / Merge 기준 초안 (PR #1: integrate/s19-ki022-ki024 → main)

> **초안 · Windows NOT TESTED · Soft≠Windows PASS · 신규 LOCK 아님**
> 작성: 수석 엔지니어 대행 초안 · 2026-10-05 (KST) · 문서만(코드 변경·push·GitHub 쓰기 없음) · CI GREEN 주장 없음
> 표의 "충족/미충족"은 **해당 항목의 증거 확보 여부**일 뿐, 프로젝트 전체 Windows PASS 선언이 아니다.

## 0. 기준점 (git 읽기 전용 확인, 2026-10-05)

| 항목 | 값 | 근거 |
|---|---|---|
| Canonical Soft tip | `96b9175129bd1b75bc2a75501178411960d6f3b4` (BLD-20261002-PHASEA-S97-001) | `git rev-parse origin/integrate/s19-ki022-ki024` |
| main | `8d9494d53ee246e32f6b34d6ffcd1f7d0631072d` | `git rev-parse origin/main` |
| 앞선 커밋 | 88개 (`origin/main..integrate`), main에만 있는 커밋 0개 | `git log`, `git rev-list --count integrate..main` = 0 |
| merge-base | `8d9494d` = main tip → **main이 integrate의 조상**(충돌 없이 병합 가능한 구조) | `git merge-base` |
| 변경 규모 | 64 files +2733/-113 (src·tests 15 files +2195/-113). 테스트 파일 변경 없음 | `git diff --stat origin/main origin/integrate/...` |
| docs 브랜치 | `origin/docs/records-20261005` = `97fc9ce`, 부모 = Soft tip, docs 71 files +5280, 코드 0 | `git diff --stat`, DOCS-RECORDS-BRANCH-20261005.md |
| PR #1 상태 | **확인 필요** (GitHub 커넥터 미연결). 이전 초안의 "OPEN"은 옛 값이며 재확인 안 됨 | README-UPDATE-DRAFT-S73-v2 §2 D-7 |
| CI | `.github/` 없음(main·integrate 파일 목록 확인). Actions 실행 0건은 투자자 응답 기록 기준(직접 재확인 못 함) | CI-INTRODUCTION-PLAN-20261003.md, INVESTOR-ADDENDUM2-RESPONSE-20261005.md |

## 1. (a) 필수 Windows 항목

원칙: 사용자에게 루틴 테스트를 요청하지 않는다. 요청 발행은 총괄만. 현재 **Lumen UI 마지막 Windows 점검 1회** 결과 대기(결과 없음). 아래 W1~W9는 그 1회 점검으로 가능한 한 함께 수집한다. 종류: L=`spike.log`, S=스크린샷, V=5~10초 화면 녹화.

| ID | 항목 | 충족 기준 | 미충족 기준 | 증거 (누가) | 근거 문서 |
|---|---|---|---|---|---|
| W1 | 시작·XAML 로드 | 창이 열리고 chrome 표시, `native_loaded` 후 시작 예외·XAML 오류 0건 | MessageBox, `ui_unhandled`, XAML 예외, chrome 없음 | L+S 1장 (사용자 제출 → 비서 기록 → 총괄 판정) | WINDOWS-RESULT-S87-S80, KNOWN_ISSUES KI-022 |
| W2 | 재생 | open→Playing→`first_frame ok`, 영상 표시, 크래시 0 | 첫 프레임 없음, 검은/빈 영상 | L (hwdec 값 기록) | WINDOWS-RESULT-S87-S80 |
| W3 | 시크 클릭 | 클릭 위치로 이동, 로그에 seek 기록, chrome 깜빡임 재발 없음 | 클릭 무시, 엉뚱한 위치 | L (`seek_latency`) | S87-ATOMIC-SOFT-CLOSED, VISUAL-GATE-GAP-TABLE U1 |
| W4 | 시크 드래그 (KI-024) | 드래그 중 썸이 포인터 추종, 놓은 위치로 이동, 멈춤 없음 | 썸 정지, 놓은 뒤 이동 안 됨 | L+V (로그만으론 추종 판정 불가) | KI-024, WINDOWS-RESULT-S87-S80(드래그 UNCONFIRMED) |
| W5 | 일시정지/재개 | Play/Pause 토글(S74)로 Paused↔Playing 전이, 화면 정지/재개 | 전이 없음, 소리만/화면만 재개 | L (`state` 전이)+V | S74-ATOMIC-SOFT-CLOSED |
| W6 | 볼륨·Mute | 트랙 누른 채 끌 때 바 추적(S96), Mute 아이콘 전환, 값 복원 | 바 미추종, 아이콘 미전환 | V (로그로 판정 불가) | S96-ATOMIC-SOFT-CLOSED |
| W7 | Theme 순환 3회 | 3회 클릭 후 처음 상태로 복귀, 아이콘 3종 구분, 예외 0 | 순환 불가, 색 깨짐, 예외 | S 3장 또는 V | S95-ATOMIC-SOFT-CLOSED |
| W8 | KI-022 빈 화면(HWND) 재현 여부 | 재생 중 영상 영역과 chrome이 **동시에** 보임(W1·W2 동시), 빈 회색 창 미재현 | 빈 회색 창 재현 | S+L | KI-022, HANDOFF-BLANK-UI-RCA |
| W9 | 아이콘·시간 라벨 | 전송 아이콘 표시, `--:--`→재생 시 라벨 색 복귀 | 아이콘 누락/글리프 대체, 색 미복귀 | S 2장 | S97-ATOMIC-SOFT-CLOSED |
| W10 | 프레임 드롭 | **별도 고위험 트랙**. 병합 게이트에 넣을지는 §5 결정 1. 사용자에게 테스트 요구 안 함 | - | 기존 S72/S87 로그(약 20~25/s, decoder/vo drop 0) | FRAME-DROP-OPTION-COMPARISON, KI-027 |

- 판정은 총괄만. 사용자 진술은 로그/캡처가 없으면 **진술**로만 기록(PASS 아님). 로그 원본은 PUBLIC repo에 올리지 않는다.
- 미수행/미수신 항목은 "UNCONFIRMED"로 남기고 PASS로 올리지 않는다. 전체화면(KI-029) 확인은 이번 1회 점검 목록에 없음(별도).

## 2. (b) KI 처리 기준

| 분류 | 정의 | 전이 조건 | 승인 |
|---|---|---|---|
| Open | 등록됨, 수정 없음 또는 원인 미확정 | 등록 시 | 총괄 |
| Soft 수정 완료 · Windows 대기 | 코드 수정 + Soft 기록(빌드/구조 승인) 있음, Windows 증거 없음 | Atomic Soft Closed 기록 | 총괄 |
| 해결 | **Windows 증거**(로그/캡처)로 재현 안 됨을 확인 | 증거 + 총괄 판정 | 총괄 |
| 수용 (Accepted) | 고치지 않고 알고 출시/병합 | 영향·사유·재검토 시점 기재 | **사용자 승인 필수** |
| 이월 | 다음 단계로 이동(병합·Release 비차단) | 사유와 담당 단계 기재 | 총괄 (고위험은 사용자) |

- **원칙: Windows 확인 전에는 해결로 닫지 않는다.** Soft 성공·사용자 진술·CI GREEN 모두 해결 근거가 아니다.
- 번호: 다음 빈 번호 순서(현재 최고 KI-029 → 다음 KI-030). 부여는 총괄 지시 후. 후보는 "번호 미배정"으로 기록. 번호 재사용·삭제 금지, 정정은 주석 추가(원문 보존).

### 현황표 (KNOWN_ISSUES.md 및 S87/S91/S96 기록에서 읽음)

| ID | 내용 | 문서상 현황 | 분류(제안) |
|---|---|---|---|
| KI-022 | Windows 빈 UI(HWND airspace 가설) | 표는 "Closed (mitigated Soft)", 정정 주석으로 **WINDOWS RE-VALIDATION PENDING**이 현행 | Soft 수정 완료 · Windows 대기 |
| KI-024 | Seek/Timeline 드래그 멈춤 | Open · Soft SUCCESS(`04b90b0`) · Windows 대기 · S87 이후 드래그 UNCONFIRMED | Soft 수정 완료 · Windows 대기 |
| KI-029 | F2 전체화면 하단 레터박스 | Open (등록), 원인 미검증, S77 Soft 기록 있음(Open 유지) | Open (총괄이 Soft 수정 완료로 옮길지 판단) |
| KI-027 | frame-drop 지속 (문서 기재 약 10~20/s, S87 총괄 기록 약 20~25/s) | Open · 고위험 · HOLD | Open · 고위험 |
| KI-026 / KI-028 | EOF 후 Play / Space가 포커스 버튼 동작 | Open (등록), S75/S76 Soft 기록 있음 | 총괄 판단 (파일이 S75/S76 이후 갱신 안 됨) |
| 미번호 | PauseButton Margin 없음 (S91) | 현재 Collapsed라 영향 없음. 표시 코드가 생기면 간격 확인 | Open 후보 · 번호 미배정 |
| 미번호 | 볼륨 트랙 누른 채 끌기 바 미추종 | S96에서 Soft 수정 완료, Windows 확인 전 해결 아님 | Soft 수정 완료 · Windows 대기 (번호 미배정) |
| 미번호 | 클릭 후 Thumb 잔존 (S87 비차단) | Windows 미확인 | Open 후보 · 번호 미배정 |
| 미번호 | Value 0에서 채움 약 7px (S87, S96 동일) | 알려진 편차, 비차단 | 이월/수용 후보 (수용은 사용자 승인) |

- **갭**: 미번호 4건은 KNOWN_ISSUES.md 본문에 **없음**(S87/S91/S96 Closed 기록과 DECISION_LOG에만 있음). KNOWN_ISSUES.md 헤더는 "Updated 2026-09-05"로 낡았다. 병합 전 갱신 필요(삭제·축소 없이 추가).
- KI-025는 문서/로그 기대 정정 항목(keep-open=yes), 별도 처리 없음.

## 3. (c) Visual Gate 최소 증거 (병합 전 필수 스냅샷 최소 세트)

조건: Dark/Light 각 1, **100% DPI**. 150% DPI는 MinWidth 912 미만이라 제외(표기만; 계산 근거 미확인). 박스는 WPF를 렌더하지 못하므로 캡처는 사용자 PC 필요 → 마지막 점검 1회에 묶고 추가 요청 금지. Visual PASS는 NOT EVALUATED 유지(스냅샷은 "증거 첨부"이지 평가 아님).

| ID | 스냅샷 | 확인 항목 | 형식 | 근거 |
|---|---|---|---|---|
| V1 | 재생 중 기본 화면 Dark/Light | 아이콘 체계, Play/Pause 계층, 영상·chrome 동시 표시 | S 2 | GAP-TABLE §4 #1, Phase4-5 §2 |
| V2 | 일시정지 화면 Dark | Play/Pause 토글 아이콘 | S 1 | GAP-TABLE U2 |
| V3 | 볼륨 그룹 확대 Dark/Light | 레일 4px과 Mute 아이콘 중심선 정렬, 값 0 채움 약 7px 편차 기록 | S 2 | S96 Gate 항목, S87 |
| V4 | Thumb hover·drag (시크, 볼륨) | hover 시 썸 표시, 드래그 중 표시, 놓은 뒤 잔존 여부 | V 1 | S87, S96 |
| V5 | 트랙 누른 채 끌기 | 볼륨 바 추적(KI 후보), 시크 바 추적(KI-024) | V (W4·W6과 동일) | S96, KI-024 |
| V6 | 시간 라벨 | 로드 전 `--:--` Tertiary, 재생 후 Current=Primary·Total=Secondary 복귀 (Style 우선순위 렌더 확인 필요) | S 2 | S97 Gate 항목 |
| V7 | Theme 아이콘 3종 | ThemeDark/Light/System 구분, 3회 순환 | S 3 | Phase4-5 스펙 아이콘 절, S95 |
| V8 | 아이콘 선 두께 | 외곽선형·선 요소 시각 두께 약 2.0(코어 0.5 + Stroke 1.5)와의 편차를 확대 캡처로 기록 | S 1~2 (V1 크롭 가능) | LUMEN-ICON-GEOMETRY-P4-2-S89, Phase4-5 §2.1 |

선택(권장, 필수 아님): chrome 숨김 시 띠, 전체화면(KI-029 관련), 좁은 창(912 부근).

## 4. (d) PR #1 병합 조건

### 병합 전 체크리스트

| # | 항목 | 기준 | 현재 상태 | 승인 |
|---|---|---|---|---|
| 1 | 빌드·테스트 | 병합 직전 Canonical에서 error 0 / warning 0, 테스트 수 일치 | S97 기록: 0/0/0, 18/18, Dark/Light 36/36, Shared 45, Icons 19. 정적 계수 18(10 Fact + 2 Theory 8행) 확인. 이 박스엔 `dotnet` 없어 **재실행 안 함**. 기록의 세 번째 0의 의미는 미확인 | 총괄 |
| 2 | 문서 일치 | PROJECT_STATUS(낡음 배너), KNOWN_ISSUES(갱신일 2026-09-05), CHANGELOG, DECISION_LOG가 Canonical과 일치 | 불일치 있음(§2 갭). docs/records 브랜치는 같은 내용 | 총괄 |
| 3 | README 일치 | README-SCOPE-ALIGNMENT-PLAN 기준 반영 | main README는 Step 0 문구, 불일치 | 총괄 |
| 4 | 필수 Windows 증거 | §1 W1~W9 증거 확보 또는 "UNCONFIRMED"로 명시 | 결과 대기 | 총괄 판정 |
| 5 | KI 분류 | §2 현황표 확정, 미번호 4건 처리 | 미완 | 총괄 (수용은 사용자) |
| 6 | 충돌 | main이 integrate의 조상 → 충돌 없음 예상. GitHub mergeable 상태·병합 직전 main SHA 재확인 | PR 상태 **확인 필요** | 총괄 |
| 7 | 병합 방식 | **merge commit(`--no-ff`) 권장**: Canonical SHA와 88개 S 단계 커밋이 main에 보존(기록 문서가 SHA를 다수 인용), 롤백이 merge 1건 revert(`-m 1`)로 가능. 대안: fast-forward(이력 보존, 롤백 단위가 88 커밋이라 불리), squash/rebase(SHA 소멸·변경, 기록 파손) → 비권장 | 미결 | 총괄 + 사용자(실행) |
| 8 | 롤백 | 병합 전 main SHA(현재 `8d9494d`, 직전 재확인)에 태그 (예: `pre-merge-main-20261005`). 복구는 merge revert. force-push 금지 | 태그 미생성 | 태그 push는 사용자 승인 |
| 9 | CI | 도입 계획 문서 기준 `.github/` 없음 → **N/A (도입 전)**. 근거: CI-INTRODUCTION-PLAN §1·§5. 도입 후에도 build+unit test only이며 Windows PASS 아님 | N/A | 총괄 |
| 10 | 금지 변경 | Release·Portable·비밀·Blur/P1·외부 텔레메트리(Sentry) 없음, force-push/대량 삭제 없음 | docs 브랜치는 토큰 패턴 점검 기록 있음. integrate 전체 diff 점검은 **미확인** | 총괄 |

### 병합 금지 조건

- 빌드 error/warning 발생 또는 테스트 수 감소. W1·W8에서 빈 화면/시작 실패 재현. 비밀 값 포함 발견. force-push·이력 재작성 필요 시.
- 프레임 드롭(KI-027)은 **Release 금지 조건**이다. 병합 자체의 금지 여부는 §5 결정 1.

### 승인 주체

| 구분 | 주체 |
|---|---|
| 기준 충족 판정, KI 분류, 병합 가능 선언 | 총괄 |
| 구조·diff 검토 | 수석 |
| 기록 | 비서 |
| **사용자 승인 필요** | 프레임 드롭 상태 병합 허용, KI 수용(Accepted), PR 병합·태그 push 실행(GitHub 쓰기), Release, 로그 공개 |

## 5. 결정 필요 (사용자)

**결정 1. 프레임 드롭 미해결(KI-027, HIGH-RISK HOLD) 상태에서 main 병합을 허용할 것인가**

| 선택지 | 내용 | 장단 |
|---|---|---|
| A. 허용 | 병합 ≠ Release. 문서에 고위험 Open 명시, Release 금지 유지 | 빠름. main이 Windows 미검증 코드를 가짐 |
| B. 불허 | 프레임 드롭 해결 후 병합 | 안전하나 해결에 고위험 Atomic + Windows 확인 필요, 테스트 비요구 방침과 충돌해 지연 가능 |
| C. 조건부 허용 | W1~W9 증거(또는 UNCONFIRMED 명시) + KI 분류 완료 + §4 체크리스트 + 롤백 태그 후 병합, main은 Release 후보 아님 | 균형 |

수석 권고: **C**.

**결정 2. 마지막 Windows 점검 결과가 오지 않거나 일부만 올 때**: A) 대기 계속 B) UNCONFIRMED 명시 후 C안 진행 C) 병합 보류. 수석 권고: B (사용자 부담 최소 방침, 단 PASS 표기 금지).

**결정 3. README/docs 반영 방식**: A) integrate에 docs 전용 커밋 추가 후 PR #1 병합(Canonical 코드 tip은 `96b9175` 유지, 코드 diff 0 증거 필요) B) PR #1 병합 후 별도 docs PR(그 사이 main README 낡음) C) docs/records-20261005를 별도 PR로 분리. 수석 권고: A (README 일치를 병합 전 조건으로 두었으므로).

## 6. 미확인 사항

- PR #1의 현재 상태(OPEN/CLOSED/mergeable)와 리뷰·체크 상태.
- 마지막 Windows 점검 1회 결과(미수신), 그리고 점검 요청이 실제 발행됐는지(S97 기록상 발행은 총괄만).
- 병합 직전 빌드 재실행 결과(박스에 dotnet 없음), S97 빌드 기록 "0/0/0"의 세 번째 항목 의미.
- 프레임 드롭 수치: KNOWN_ISSUES 약 10~20/s vs 총괄 기록 약 20~25/s — 원인 미확정.
- 150% DPI 제외의 계산 근거, PC 사양, 이전 초안의 "main=8d9494d / PR #2 merged" 현재 유효성.
- integrate 전체 diff의 비밀·금지 변경 점검, KI-026/028/029의 Soft 수정 후 문서 분류 확정.
