# README–구현 범위 일치 계획 초안

> **초안 · Windows NOT TESTED · Soft≠Windows PASS · 신규 LOCK 아님**
> 작성: 수석 엔지니어 대행 초안 · 2026-10-05 (KST) · 문서만(README 수정·commit·push·GitHub 쓰기 없음) · CI GREEN 주장 없음

## 0. 기준과 범위

| 항목 | 값 | 근거 |
|---|---|---|
| 구현 기준 | integrate tip `96b9175129bd1b75bc2a75501178411960d6f3b4` (S97, BLD-20261002-PHASEA-S97-001) | `git rev-parse origin/integrate/s19-ki022-ki024` |
| 대비 | `origin/main` = `8d9494d` (S19 시점). integrate의 README.md는 main과 **동일**(diff 없음) | `git show ...:README.md` 비교 |
| 개정 대상 | main 기준 README.md(54줄 상당, "Step 0 tech spike") | `git show origin/main:README.md` |
| 이전 초안 | `README-UPDATE-DRAFT-S73-v2-20261003.md` (S72 `963bdb9` 기준, 박스 `docs/continuity/`에서 확인). v1은 S71 | 파일 확인 |
| 근거 위치 | repo: `docs/SOFT_*.md`(35개), `docs/phase-a/*.md`(32개), `docs/spike/REPRO.md`, `src/`. 단 `docs/known-issues` 등은 integrate에 **없고** `origin/docs/records-20261005`(`97fc9ce`)에만 있음 | `git ls-tree` |
| 근거 한계 | S73~S97의 stage 기록(LOCK/Closed)은 박스 프로젝트 문서에만 있음(공개 repo 미게시) → README에서 링크하지 않음 | DOCS-RECORDS-BRANCH-20261005 |

## 1. 불일치 표 (현재 main README ↔ 구현 범위)

| # | main README | 구현 사실 (근거) | 조치 |
|---|---|---|---|
| 1 | 제목 "Step 0 tech spike", 본문 "playback spike" | Phase A Soft S1~S97 진행 (git 88 커밋, 박스 stage 기록) | 개정 |
| 2 | "Status: Step 0 PASS WITH KI · Phase A in progress (S0 Contracts/Facade)" | 상태가 S0 시점. "PASS WITH KI"는 Windows 증거로 뒷받침 안 됨(이전 초안 D-3) | 문구 교체: "Windows 검증 전" 명시 |
| 3 | Layers: Contracts = 4개 인터페이스 | Contracts 10개 파일: `INativeRuntimeProbe`, `IPlaybackEngine`, `IPlaybackObserver`, `IPlaylistService`, `IRenderHost`, `IResumeStore`, `MediaInfoBasics`, `MediaTrackInfo`, `MediaTrackKind`, `PlaybackState` | 갱신 |
| 4 | Platform = `SpikeLogger`, `HardwareAccelPolicy` | 5개: + `LoggingObserver`, `FileResumeStore`, `DelegatingNativeRuntimeProbe` | 갱신 |
| 5 | Playback = volume clamp, observer fan-out, path validation | + `MinimalPlaylistService` (`MinimalPlaylistService.cs`) | 갱신 |
| 6 | Shell = WPF UI, `VideoHwndHost`, DPI; "no mpv DllImport" | Shell DllImport는 `VideoHwndHost.cs` 2건(user32)뿐 — 확인. Prefs/Bounds store 8종(`*PrefsStore` 7 + `WindowBoundsStore`), `Themes/` 5개 xaml | 갱신 |
| 7 | Tests = Facade unit tests | xUnit 2.9.3, 12 속성(Fact 10 + Theory 2, InlineData 8행) = 18 case(정적 계수). main 대비 테스트 파일 변경 없음 | 갱신(실행 결과는 빌드 기록값 표기) |
| 8 | "Spike vs MVP ... Not in spike: playlist, resume, themes, subtitle UI" | playlist(`MinimalPlaylistService`, `PlaylistPrefsStore`), resume(`FileResumeStore`), themes(`AppThemeMode` Dark/Light/System, `Themes/*.xaml`), subtitle(`SOFT_DROP_SUBTITLE.md` 등) 코드·문서 존재 | 절 삭제 후 기능 표로 대체 |
| 9 | Native load order 3단계 | `MpvNative.EnumerateCandidates`: env → 상대 경로 → `BaseDirectory` → 상위 최대 8단계 탐색 (이전 초안은 5단계로 서술; 이번에 단계 수까지는 재계수 안 함) | 코드 기준으로 서술, 단계 수 **미확인** |
| 10 | 기능·단축키·저장 위치·Lumen UI·Known Issues·Windows 현황 절 없음 | 아래 §2 | 신설 |
| 11 | CI/Release 언급 없음 | `.github/` 없음 → "CI 없음, CI GREEN 주장 없음" 명시 | 신설 |

## 2. S1~S97 구현 범위 요약 (README 기능 표의 근거)

"Soft 구현"은 코드·stage 기록 확인이며 Windows 검증이 아니다. 아래 Windows 열은 모두 **NOT TESTED**이거나 사용자 진술/로그 일부(PASS 아님).

| 범위 | 내용 (근거 유형: C=코드, D=repo docs, R=박스 stage 기록) | Windows 상태 |
|---|---|---|
| S0~S9 | Contracts/Facade, 하단 control bar, timeline, transport, volume·mute, fullscreen, Sub/Audio flyout, Playlist 패널, theme, chrome auto-hide, resume, screenshot, Media Info, frame step (D `docs/phase-a`, C) | NOT TESTED (일부 사용자 진술) |
| S10~S50 | subtitle offset·OSD, drag-and-drop, 휠/키 볼륨, Always-on-top, 창·볼륨·theme·playlist 저장, 탐색 키(PageUp/Down, Home/End, 숫자), 미디어 키, 경로·파일명 등 복사, 트랙 순환, 마지막 폴더 저장 (D `SOFT_*`, `phase-a`, C) | NOT TESTED. 미디어 키·경로 복원은 과거 "USER WINDOWS TEST REQUIRED" 미해결 |
| S51~S66 | 길이·남은 시간 복사, 자막 offset 초기화, 미세 볼륨, 자막 drop, 창 제목, 각종 OSD, playlist Enter·선택 동기화 (D `SOFT_*`, R) | NOT TESTED |
| S67~S72 | 진단 로그만(seek_latency, end_file, mpv_event, eof_reached, render_path), Seek 클릭·드래그 개선(S70/S71) (C `MpvPlaybackEngine.cs`, D `REPRO.md`) | 사용자 로그 일부 수신. 드래그 등 UNCONFIRMED |
| S73~S77 | chrome hover 중 auto-hide 유지(S73), Play/Pause 단일 토글(S74), EOF 후 Play 재시작 시도(S75, KI-026), Space 포커스 버튼 문제(S76, KI-028), 전체화면 하단 레터박스(S77, KI-029) (R) | NOT TESTED. KI-026/028/029 Open(Windows 미확인) |
| S78~S97 | Lumen UI: 토큰·Dark/Light 색(S78), 테마 리소스 교체(S79), `Controls.xaml`(S80), chrome 색·구분선·MinWidth 912·시간 라벨·툴팁·버퍼바(S81~S86), SeekSlider 템플릿(S87), 아이콘 Geometry 19개(S88~S89), `LumenIconButton`·버튼 교체(S90~S95), 볼륨 슬라이더(S96), 시간 라벨 Style(S97) (C `Themes/*.xaml`, R) | WPF 렌더 NOT TESTED. Visual PASS NOT EVALUATED. 마지막 점검 1회 결과 대기 |

- S73~S97 중 stage별 세부 동작은 박스 기록 기준이며 repo의 `docs/SOFT_*.md`에는 없다(미게시). README에는 "Lumen UI 템플릿 적용(Soft)" 수준으로만 쓴다.
- 이전 초안 이후 달라진 점: §4.

## 3. 코드 레이아웃 표 재확인 (integrate tip `96b9175`, `git ls-tree`)

| 프로젝트 | 확인 내용 |
|---|---|
| Contracts | 10개 `.cs` (위 §1-3) |
| Platform | `SpikeLogger`, `LoggingObserver`, `HardwareAccelPolicy`, `FileResumeStore`, `DelegatingNativeRuntimeProbe` |
| Playback | `PlaybackFacade`, `MinimalPlaylistService` |
| Engine.Mpv | `MpvPlaybackEngine`, `MpvRenderHost`, `Native/MpvNative`, `Native/MpvEventIds`. DllImport: `MpvNative.cs` 2건, `MpvPlaybackEngine.cs` 1건 → **libmpv P/Invoke는 이 프로젝트에만** |
| Shell | `App`, `AppTheme`(Dark/Light/System), `MainWindow`, `VideoHwndHost`, prefs/bounds store 8종, `Themes/`(Tokens.Shared, Colors.Dark, Colors.Light, Icons, Controls), `app.manifest` |
| Tests | `tests/DesktopMediaPlayer.Playback.Tests` — `PlaybackFacadeTests`, `FakePlaybackEngine`, `RecordingObserver`. Contracts·Playback만 참조 |

## 4. 이전 초안(S73-v2, S72 기준)과의 차이

| 항목 | 이전 초안 | 이번 계획 |
|---|---|---|
| 기준 tip | `963bdb9` (S72), S73은 진행 중 | `96b9175` (S97). S73~S97 모두 Soft Closed 기록 존재 |
| 기능 | S1~S72, Lumen UI 없음 | + S73~S77 사용자 피드백 대응, S78~S97 Lumen UI |
| KI | KI-022/024/014 + 번호 없는 후보 3건 | KI-025~029 번호 부여됨(KNOWN_ISSUES). 단 Lumen 후보 4건은 아직 번호 없음 |
| 근거 문서 | repo에 `docs/known-issues` 없음 → 링크 불가 | `docs/records-20261005`에 존재. 병합 전에는 링크 금지, 병합 후 링크 여부 총괄 판단 |
| frame-drop | 7.8~22.3/s | 총괄 기록 약 20~25/s HOLD (고위험, 사용자 테스트 미요구) |
| 유지 | 한국어, 저장소 밖 링크 금지, "PASS WITH KI" 미사용, 18/18은 빌드 기록값, CI 없음 | 동일 |
| 폐기 | - | 이전 초안 §4 전문을 그대로 적용하지 않는다(tip·기능 범위가 달라 재작성 필요) |

## 5. README 개정 목차안

| 절 | 내용 | 상태 문구 규칙 |
|---|---|---|
| 1. 제목·한 줄 설명 | WPF + libmpv, win-x64, .NET 10 | "Step 0 PASS WITH KI" 삭제 |
| 2. 현재 상태 | 단계(Phase A, Soft), 기준 tip·빌드 ID, main과의 관계, 빌드·테스트(빌드 기록값, 재실행 여부 명시) | **"Windows 검증 전"** 명시. Soft≠Windows PASS. CI 없음 |
| 3. 구성(Layers) | §3 표 | - |
| 4. 구현 기능 | 2열: **Soft 구현 / Windows 검증 상태** | Windows 열은 NOT TESTED, 진술은 "진술", UNCONFIRMED. PASS 단어 사용은 "PASS 아님" 부정문 외 금지 |
| 5. 단축키·저장 위치 | 코드(`PreviewKeyDown`, prefs 파일)에서 재추출 | 이전 초안 표는 S72 기준 → 재확인 필요 |
| 6. Lumen UI | 템플릿 적용 범위(Soft), Visual NOT EVALUATED | "Visual PASS" 금지 |
| 7. Known Issues | KI-022/024 WINDOWS PENDING, KI-027(고위험 HOLD), KI-026/028/029 Open, 번호 미배정 후보 | 해결로 쓰지 않음 |
| 8. 하지 않는 것 | Blur/P1 HOLD, Release·installer·auto-update 없음, 외부 텔레메트리 없음, 엔진 교체 없음 | - |
| 9. LGPL·Native DLL | 현행 유지(로드 순서는 코드 기준) | CI/agent DLL 다운로드 금지 규칙 유지 |
| 10. 빌드·증거 | build 명령, `docs/spike/REPRO.md` 링크 | PASS 주장 금지 |

## 6. 섹션별 변경 계획

| 섹션 | 변경 | 위험/주의 |
|---|---|---|
| 상태 | 재작성 | tip·빌드 ID는 병합 직전 재확인(이후 커밋 시 어긋남) |
| Layers | 증보(§3) | 파일 목록은 `git ls-tree`로 재생성 |
| Spike vs MVP | 삭제 → 기능 표 | "Not in spike" 항목을 구현됨으로 이동하되 Windows 열은 NOT TESTED |
| 기능 | 신설. 각 행에 근거 파일(`docs/SOFT_*.md`, `docs/phase-a`, 소스) 표기. 근거 없는 항목 제외 | 과장 금지. S73~S97은 박스 기록만 근거 → 요약 수준 |
| Known Issues | 신설 | KNOWN_ISSUES.md 갱신(문서 1 §2) 이후 반영 |
| 사용자 진술/로그 | 최소화, 개인정보·로그 원문 제외 | 공개 여부는 사용자·총괄 |
| 빌드/테스트 | 18 case 정적 계수 + "빌드 기록값, README 작성 시 재실행하지 않음" | 박스에 dotnet 없음 |

## 7. 순서·책임·시점

| 순서 | 작업 | 담당 |
|---|---|---|
| 1 | 마지막 Windows 점검 1회 결과 수신·기록 (결과 전 PASS 문구 작성 금지) | 비서 기록, 총괄 판정 |
| 2 | KNOWN_ISSUES 갱신(후보 4건 반영, 낡은 헤더) | 비서 기록, 총괄 승인 |
| 3 | README 본문 초안 작성(이 계획 기준, §2 표 재검증) | 수석 구조 검토 대행 |
| 4 | 수석 구조 검토: 코드 사실(§3), 근거 파일 존재, PASS 표현 점검 | 수석 |
| 5 | 총괄 승인 → 적용 방식 결정(문서 1 결정 3) | 총괄 (공개 반영·push는 사용자 승인) |
| 6 | **PR #1 병합 전**에 README 일치 확인 (문서 1 체크리스트 3번) | 총괄 |
| 7 | Canonical tip이 바뀌면 상태 절·기능 표 재갱신 | 비서 |

- 적용 방식: integrate에 docs 전용 커밋을 얹으면 head SHA가 바뀐다. "Canonical Soft tip = 코드 기준 `96b9175`, 이후 docs 전용(코드 diff 0)" 규칙이 필요하다(총괄 결정).
- 병합 후 별도 docs PR로 하면 그 사이 main README가 낡은 구간이 생긴다.

## 8. 미확인 사항

- PR #1의 현재 상태, GitHub 쪽 README 변경 여부.
- 마지막 Windows 점검 1회 결과(미수신).
- S73~S97 개별 동작의 코드 수준 재검증 범위(이번엔 Lumen 파일 목록·stage 기록 위주, 동작 실행은 못 함). Native 로드 후보의 정확한 단계 수.
- 빌드·테스트 재실행(박스에 dotnet 없음). 단축키 표(이전 초안 것)가 S97 코드와 일치하는지.
- 사용자 진술·로그를 README에 공개할지, `docs/records-20261005`를 README에서 링크할지.
