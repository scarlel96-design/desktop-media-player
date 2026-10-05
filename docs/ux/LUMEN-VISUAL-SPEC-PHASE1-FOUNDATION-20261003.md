# Lumen Visual Spec — 로드맵 + Phase 1 Design Foundation (문서 전용 · 코드 변경 없음)

| 항목 | 값 |
|---|---|
| Doc ID | UX-LUMEN-SPEC-P1-001 |
| 작성 | UI·UX 봇, 2026-10-03 KST (총괄 04:19 지시: LUMEN UI/UX IMPLEMENTATION PHASE 전환 준비) |
| 구현 기준(SoT) | `PHASE_A_PLAYER_SCREEN.md` + `LUMEN_DESIGN_DIRECTION.md`(둘 다 참고가 아니라 **구현 기준**) |
| 근거 | `97c0360` 코드 정적 읽기(E2) + `VISUAL-GATE-GAP-TABLE-20261003.md` + `USER-CAPTURE-REVIEW-20261003-0345.md`(사용자 캡처 픽셀 측정) |
| 개정 | Rev3 2026-10-03 04:4x KST(파일명·키 형식을 S78 LOCK에 맞춤) · Rev2 04:3x KST — 수석 조건부 승인 반영(F-1/F-2/F-3, Legacy.* 보존, 1D→Phase 4, 1E→Phase 7, Effect·OpacityMask·AllowsTransparency 금지). §4·§6·§8·부록 A 교체, §2.1은 부록 A가 우선 |
| 상태 | **UI PASS / Visual PASS = NOT EVALUATED. Soft ≠ PASS. Windows NOT TESTED.** 이 문서는 설계 스펙이며 구현·검증 결과가 아니다. |
| 정책 준수 | Blur 기본 OFF · show/hide는 Opacity만 · L1≤2 · 영상·성능 우선 · Facade/Architecture 변경 없음 · 사용자 루틴 테스트 요청 없음 |

## 0. 요약

1. 목표는 외형 복제가 아니라 **하나의 독립 디자인 언어(Lumen)**를 WPF 리소스(토큰) 체계로 만드는 것이다. 현재는 `App.xaml` 리소스가 비어 있고, `MainWindow.xaml`에 하드코딩 색 23곳, `MainWindow.xaml.cs`의 `ApplyTheme`가 색 4개만 코드로 덮어쓴다. 그래서 Light 테마를 골라도 버튼·플라이아웃·패널은 어두운 색 그대로일 수 있다(코드상 추정, 렌더 미확인).
2. Phase 1(Foundation)은 **눈에 보이는 변경을 거의 만들지 않는다**. 토큰 사전, 타입 스케일, 아이콘 규격, 모션 토큰, 상태 규칙을 먼저 만들어 이후 Phase 2~11이 같은 토큰만 쓰게 한다.
3. Phase 1은 Atomic 3개(F-1, F-2, F-3, §6). 각 Atomic은 Shell 한정·소형. 새 Lumen 값은 Phase 2 이후 컴포넌트 Atomic에서 하나씩 채택한다(한 번에 23곳 치환 금지).
4. 열린 결정 O1~O5는 총괄이 모두 확정했다(§8).

## 1. 구현 단계 지도 (총괄 순서)

| Phase | 이름 | 핵심 산출 | 격차 항목(게이트표 ID) | Windows 확인 성격 |
|---|---|---|---|---|
| 1 | Design Foundation | 토큰·타입·간격·반경·아이콘 규격·모션 토큰·상태 규칙, Light/Dark 리소스 교체 구조 | 11, 12, D4(규격) | 없음(시각 변화 거의 0) |
| 2 | Player Chrome | 바 배경·구분·그룹핑·위계, 숨김 시 띠(#121212, 113px) 해소, 컨트롤 폭 | 1, 2, 3, 9, D1 | 관련 변경 묶음 1회(Visual) |
| 3 | Timeline | 트랙/썸 상태, 버퍼, hover 시간 툴팁, 어두운 트랙(현재 밝은 회색 기본 템플릿) | D2, AC-UI-1, 11 | Visual |
| 4 | Transport | 단일 Play/Pause(U2 마무리), 아이콘 24 교체, 버튼 템플릿(hover/pressed/focus/disabled/selected) | 1, 5, 7, D4, AC-UI-4, U2 | Visual |
| 5 | Volume / Time | 볼륨 비율·mute 상태, 시간 타이포 | 4, D3, D5, AC-UI-2/3 | Visual |
| 6 | Flyout / Playlist | 패널·플라이아웃 공통 표면, 열림/닫힘 전환, 리스트 항목 상태 | 9, AC-UI-5/6, 11 | Visual |
| 7 | Motion / State | chrome 페이드, OSD, 상태 전환 일괄 정렬 | 8, 10, U1(시각 부분) | Visual(모션은 영상 필요) |
| 8 | Fullscreen / Auto-hide | 전체화면 chrome, 포인터 위 유지 규칙 | AC-UI-7, U1 | 고위험 아님(UI), 영상 1건 |
| 9 | HiDPI / Theme | 100/125/150/175/200%, Light·System 테마 완결 | 11, 6 | 스크린샷 스냅샷 |
| 10 | Visual QA | 12 항목 + 7개 Visual Quality 항목 증거 채우기 | 전체 | 검증·검수 |
| 11 | Visual Gate | 총괄 Gate 판정 | 전체 | 사용자 확인 1회(최종) |

기존 진행: U1(S73)·U2(S74)는 Batch 1로 계속된다. U2는 현재 글리프 기반으로 단일 토글을 구현하고, 아이콘 모양은 Phase 4에서 교체한다(중복 작업 방지: S74는 아이콘 소스를 바꾸지 않는다). U1은 동작 규칙이므로 Phase 8에서 시각(페이드)만 통합한다.

## 2. 토큰 사전 (WPF `ResourceDictionary` 이름 규칙)

규칙: 키는 `Lumen.<Group>.<Name>`. 색은 `Color`와 `SolidColorBrush` 쌍(`...Color`, `...Brush`). 코드는 `DynamicResource`만 쓰고, 영상 영역 `#000000`만 예외로 허용한다.

### 2.1 색 (Dark 기본 / Light) — 대비는 계산값(E3), 렌더 미확인

| 토큰 | Dark | Light | 용도 / 대비 |
|---|---|---|---|
| `Surface.Window` | #121212 | #F4F4F6 | 창 바탕(영상 뒤). 영상 #000과 숨김 띠를 만들지 않도록 Phase 2에서 chrome 행 규칙 확정 |
| `Surface.Chrome` | #1A1A1C | #F4F4F6 | 하단 chrome 불투명 면(불투명 우선, 영상 위에 겹치지 않으므로 알파 불필요) |
| `Surface.Raised` | #222225 | #FFFFFF | 플라이아웃·패널(elevation 1) |
| `Border.Subtle` | #2E2E32 | #DADADF | 구분선·패널 경계 1px |
| `Text.Primary` | #F5F5F7 | #1A1A1C | 본문. Dark/Chrome 15.96:1, Light 15.82:1 |
| `Text.Secondary` | #B4B4BA | #55555C | 시간·보조. Dark 8.42:1, Light 6.73:1 |
| `Text.Tertiary` | #8E8E96 | #6C6C74 | 힌트·빈 상태. Dark 5.35:1, Light 4.74:1 (≥4.5) |
| `Icon.Default` | = Text.Secondary | = Text.Secondary | 평상시 아이콘 |
| `Icon.Active` | = Text.Primary | = Text.Primary | hover/pressed/selected |
| `Icon.Disabled` | #5A5A62 | #A0A0A8 | 비활성. 의도적으로 3:1 미만(비활성은 접근성 대비 면제), 단 **배경보다 확실히 어둡게**(현재 Next는 #F4F4F4로 오히려 가장 밝아 역전) |
| `Accent` (가칭, O2) | #7AA7FF | #2B5BD0 | 선택·포커스·진행. Dark 7.28:1, Light 5.45:1 |
| `Accent.OnAccent` | #0B0B0C | #FFFFFF | 강조 면 위 글자. 8.24:1 / 5.98:1 |
| `State.HoverOverlay` | #FFFFFF α0.08 → #2C2C2E | #000000 α0.06 → #E5E5E7 | hover 면(합성 결과를 Chrome 위 기준으로 기록) |
| `State.PressedOverlay` | #FFFFFF α0.14 → #3A3A3C | #000000 α0.12 → #D7D7D8 | pressed 면 |
| `Track.Rail` | #5C5C62 | #B8B8C0 | 슬라이더 레일(장식 요소, 채움은 Accent) |
| `Track.Buffer` | Accent α0.35 | Accent α0.30 | 버퍼 바. 현재 #4466AA 고정을 대체 |
| `Danger` | #FF6B6B | #C62828 | 오류 문구. 6.26:1 / 5.12:1 |
| `Focus.Ring` | = Accent | = Accent | 포커스 링 |

기존 값 처리: `#2A2A2A/#3A3A3A` 버튼 면·테두리는 Phase 4에서 `Surface.Chrome`+오버레이 규칙으로 대체(기본은 면 없음, hover에서만 오버레이). 사용자 캡처에서 확인된 WPF 기본 hover(#C2E7FB)·pressed(#C7E6F5)·disabled(#F4F4F4/#ADB2B5)는 Phase 4 템플릿 교체로 사라져야 한다.

### 2.2 타이포 (O3: 폰트 결정 필요)

폰트 폴백: `Segoe UI Variable Text, Segoe UI`(Win11 → Win10 폴백). 숫자는 `Typography.NumeralSpacing="Tabular"` 시도, 폰트가 지원하지 않으면 현재 Consolas를 유지(시각 증거로 확정, 가정 아님).

| 토큰 | 크기/행높이 | 두께 | 용도 |
|---|---|---|---|
| `Type.Caption` | 12 / 16 | Regular | 시간, 툴팁 |
| `Type.Body` | 14 / 20 | Regular | 리스트 항목, 안내 |
| `Type.Title` | 14 / 20 | SemiBold | 패널 제목 |
| `Type.Mono` | 12 / 16 | Regular(Consolas) | 미디어 정보 |
| `Type.Osd` | 28 / 36 | SemiBold | 자막/OSD 대형 텍스트(현재 28 유지) |

### 2.3 간격·크기·반경·그림자

| 토큰 | 값 | 비고 |
|---|---|---|
| `Space.1/2/3/4` | 4 / 8 / 16 / 24 | 4는 반단위(아이콘 보정 전용) |
| `Size.Hit` | 40 | 버튼 히트(AC-UI-4 유지) |
| `Size.Icon` | 24 | 아이콘 그리드(D4) |
| `Size.TimelineHit` | 24 | AC-UI-1 유지 |
| `Size.TrackRest/Hover` | 4 / 6 | 레일 두께(hover 시 확대는 Phase 3, 120ms) |
| `Size.Thumb` | 12(rest) / 14(hover·drag) | |
| `Size.VolumeWidth` | 96 (88–104) | 기존 유지 |
| `Radius.Control` | 6 | 버튼 hover 면, 슬라이더 썸은 원 |
| `Radius.Panel` | 10 | 플라이아웃·패널 |
| `Radius.Tooltip` | 6 | |
| `Elevation.1` | 그림자 효과 없음. `Surface.Raised` + `Border.Subtle` 1px | **DropShadowEffect 금지**(Premium ≠ Heavy, 영상 경로 비용 회피). 그림자는 측정 후 채택(Phase 10) |

곡률은 기준 부재가 격차가 아니라는 기존 원칙은 유지한다. 위 반경은 "쓸 때 이 값만 쓴다"는 통일 규칙이다.

### 2.4 아이콘 규격

- 방식: 폰트 글리프를 버리고 **자체 제작 벡터 `Geometry`**(`LumenIcons.xaml`, `DrawingImage` 또는 `Path.Data`). 폰트 대체·컬러 이모지(🔊 🖼) 혼재를 없앤다. 외부 아이콘 세트를 복사하지 않는다(독립성·라이선스 레벨 1 유지).
- 그리드: 24×24 viewBox, 안전 영역 20×20(상하좌우 2). 기본은 채움형(Fill) + 둥근 모서리, 선 아이콘은 두께 2·둥근 캡. **한 제품 안에서 채움/선 혼용 규칙**: 전송 컨트롤(play/pause/stop/prev/next/frame)은 채움, 유틸(subtitle/audio/list/info/theme/fullscreen/mute/screenshot/open)은 두께 2 선 + 필요 시 채움. 시각 무게(밀도)는 전 아이콘에서 같게 맞춘다.
- 광학 정렬: 재생 삼각형은 도형 중심이 아니라 **무게중심**을 24 그리드 중심에 맞춘다. 예시(출발점, 구현에서 렌더로 보정): Play `M8.5 5.6 L18.2 12 L8.5 18.4 Z`, Pause `M7 5 H10 V19 H7 Z M14 5 H17 V19 H14 Z`, Stop `M7 7 H17 V17 H7 Z`.
- 색: `Icon.Default/Active/Disabled` 토큰만. 아이콘 `Fill`은 `Foreground` 상속(`Path.Fill="{Binding Foreground, RelativeSource=...}"`).
- HiDPI: 125%에서 24 DIP = 30px(정수), 150% = 36px, 175% = 42px, 200% = 48px로 모두 정수. 선 두께 2 DIP도 125%에서 2.5px로 분수이므로 **Phase 9에서 100/125/150% 실측 후 보정 필요(위험으로 기록)**.

### 2.5 모션 토큰

| 토큰 | 값 | 사용 |
|---|---|---|
| `Motion.Fast` | 120 ms | hover/press/focus 면, 아이콘 전환 |
| `Motion.Base` | 150 ms | chrome 페이드 인, 플라이아웃 불투명도 |
| `Motion.Slow` | 180 ms | chrome 페이드 아웃(조용히 빠짐), 패널 내용 불투명도 |
| `Ease.Out` | `CubicEase EaseOut` | 나타남·hover 진입 |
| `Ease.In` | `CubicEase EaseIn` | 사라짐 |
| 금지 | scale/bounce/slide 장식, 영상 영역 크기 애니메이션 | 영상 HWND는 보간하지 않음(재배치 비용). 패널 열림은 영상 폭을 즉시 바꾸고 **패널 내용만** 불투명도로 150ms 페이드(Phase 6에서 측정 후 확정) |
| 감속 모션 | `SystemParameters.ClientAreaAnimation == false`이면 모두 0 ms | Phase 7에서 구현 |

원칙 연결: 같은 종류(버튼 면, 플라이아웃, 패널)는 같은 토큰. 입력 지연 0: 입력 처리 후 모션 시작, 모션은 `Opacity`/오버레이만, 레이아웃 속성 애니메이션 금지.

## 3. 상태 규칙 (모든 인터랙티브 컨트롤 공통)

| 상태 | 버튼 | 슬라이더 | 리스트 항목 | 비고 |
|---|---|---|---|---|
| Rest | 면 없음, 아이콘 `Icon.Default` | 레일 `Track.Rail`, 채움 Accent | 면 없음 | |
| Hover | 면 `State.HoverOverlay`(Radius.Control), 아이콘 Active, 120ms | 레일 6, 썸 14 | 호버 면 | |
| Pressed | 면 `State.PressedOverlay`, 즉시(0~60ms) | 썸 14 + 드래그 시 툴팁 유지 | Pressed 면 | 입력 응답 즉시 |
| Focus(키보드) | 2px `Focus.Ring`, 면과 2px 간격, 반경+2 | 썸 주위 링 | 항목 윤곽 링 | **현재 `FocusVisualStyle=null`로 포커스 표시가 없다 → 제거**(접근성 GAP) |
| Disabled | 아이콘 `Icon.Disabled`, 면 없음, 호버 무시 | 해당 없음 | 글자 Tertiary | Next/Prev의 #F4F4F4 반전 해소 |
| Selected/On | 아이콘 Accent, 면 Accent α0.14 | 해당 없음 | Accent 막대 2px + 글자 Primary | Mute On/Off, 재생목록 열림, 고정(Pin) 등 토글 표시 |

접근성: 툴팁·`AutomationProperties.Name`은 상태에 맞춰 변경(예: Play/Pause 토글). 히트 40×40 유지, 타임라인 히트 24.

## 4. 리소스 구조 (Shell 한정, 수석 확정안)

```
src/DesktopMediaPlayer.Shell/Themes/
  Lumen.Tokens.Shared.xaml   간격·크기·반경·타입·모션 (테마 무관)
  Lumen.Colors.Dark.xaml     색 SolidColorBrush (Dark)
  Lumen.Colors.Light.xaml    색 SolidColorBrush (Light) — 키 집합이 Dark와 완전히 동일
  Controls.xaml        스타일 3개 이동(F-3에서 생성, F-1에서는 병합하지 않음)
```

- 이름은 **S78(F-1) LOCK 기준**: `Themes/Lumen.Tokens.Shared.xaml`, `Themes/Lumen.Colors.Dark.xaml`, `Themes/Lumen.Colors.Light.xaml`, F-3의 `Themes/Controls.xaml`. 색 키는 `Lumen.<Group>.<Name>Color`와 `...Brush` 쌍이다(수석 회신 기준). 이 문서의 부록 A는 **설계 전체 집합**이고, F-1에 실제로 들어가는 키는 S78 LOCK 목록(수석 propose)이 우선한다.
- `App.xaml` MergedDictionaries: Shared → Dark(기본) 순(F-1). Controls는 F-3에서 추가. 테마 전환은 **Dark/Light 사전 한 개를 교체**한다. 모든 테마 의존 참조는 `DynamicResource`.
- 기존 값은 **`Legacy.*` 키**로 보존한다(Dark/Light 양쪽 사전에 같은 키, 현재 코드가 쓰는 값). 새 Lumen 값은 Phase 2 이후 컴포넌트 Atomic에서 채택한다. 따라서 Dark 동일 주장은 `Legacy.*` 경유 항목에 한해서만 한다(§6).
- csproj 변경 없음(WPF SDK가 `*.xaml`을 Page로 자동 포함).
- 위험: ① `MainWindow.xaml.cs`의 `ApplyTheme`가 `Background`/`Foreground`/`BottomChrome.Background`/`StatusText.Foreground`를 로컬 값으로 덮어쓰면 리소스 값이 가려진다 → F-2에서 코드 대입을 제거. ② `sys:Double`용 `xmlns:sys="clr-namespace:System;assembly=mscorlib"`가 .NET WPF에서 컴파일되는지는 **빌드로 확인**(이 박스는 WPF를 컴파일하지 못했고 XML 구문만 검사함). ③ 키 누락 시 `StaticResource`는 시작 크래시, `DynamicResource`는 값 없음 → F-2는 `DynamicResource`만 쓰고 키는 F-1에서 먼저 존재해야 한다.
- 영상 위 오버레이, Effect/DropShadow/Blur/OpacityMask, `AllowsTransparency`는 규칙으로 금지(§4.1). Facade/Architecture·엔진·`VideoHwndHost` 변경 없음.

### 4.1 구현 금지·허용 규칙 (총괄·수석 확정)

- 금지: 영상 위 오버레이, `Effect` 전반(DropShadow·Blur 포함), `OpacityMask`, `AllowsTransparency`, 레이아웃 속성 애니메이션(`Width/Height/Margin` 등).
- 허용 모션: `Opacity`와 `RenderTransform`만.
- 불변: 기존 `x:Name`과 이벤트 핸들러 이름.
- 현재 코드와의 충돌 1건: `HoverTimePopup`이 `AllowsTransparency="True"`를 쓴다. Phase 3(Timeline)에서 둥근 모서리 투명 Popup 대신 불투명 Popup(반경 0) 또는 다른 구현으로 교체한다고 Phase 2 스펙에 기록한다.

## 5. 격차 통합표 (게이트표 + 사용자 캡처 관찰 → Phase 배정)

| 출처 | 항목 | Phase |
|---|---|---|
| 게이트표 #1 Hierarchy | Play에 주 동작 강조 없음, 16개 동일 스타일 | 2(그룹), 4(Play 강조) + O1 |
| #2 Alignment / #3 Density | 폭 912, 16개 컨트롤 | 2 (+ O1) |
| #4 Typography | 타입 스케일 부재 | **1**(토큰), 5(적용) |
| #5 Icon consistency | 글리프 14px, 이모지 혼재 | **1**(규격·핵심 6), 4(전체) |
| #6 Spacing | 8/16 대체로 OK, 플라이아웃 하단 108 하드코딩 | 6 |
| #7 State feedback | 포커스 제거, WPF 기본 hover/pressed/disabled | **1**(상태 규칙), 4 |
| #8 Animation | 모션 없음 | **1**(토큰), 7 |
| #9 Transition | 숨김 시 #121212 띠 113px(측정 확정) | 2 |
| #10 Interaction continuity | U1/U2 | S73/S74 → 7, 8 |
| #11 Visual continuity | 토큰 부재, Light 테마 미완 | **1** |
| 캡처 관찰 | 시크 트랙이 밝은 회색 기본 템플릿 | 3 |
| 캡처 관찰 | 시크 클릭 후 영상 약 0.6초 정지(재생 엔진 쪽 가능성) | UI 범위 밖 → 수석 확인 |
| 캡처 관찰 | 재생목록 패널 폭 약 290 DIP(280–320 OK), 전환 즉시 | 6 |

## 6. Phase 1 Atomic 분할안 (수석 확정 · Shell 한정 소형)

공통: 변경 파일 ≤3, 빌드 0 error/0 warn, 기존 테스트 18/18, Facade/Contracts/Playback/Engine 수정 금지, 로그·진단 코드 추가 없음, 롤백=커밋 revert, Soft 증거만(렌더 PASS 주장 금지).

| ID | 내용 | 상한 | 시각 변화 | 완료 기준 |
|---|---|---|---|---|
| F-1 | `Themes/Lumen.Tokens.Shared.xaml`, `Lumen.Colors.Dark.xaml`, `Lumen.Colors.Light.xaml` 신규 + `App.xaml` MergedDictionaries 연결 | 신규 3 xaml 합계 ≤150줄(초안 98줄 측정), `App.xaml` ≤+4줄 | **없음**(Lumen 키는 아직 아무도 참조하지 않음) | 빌드 통과. Dark·Light 키 집합 완전 동일(초안에서 diff로 확인). 키·값이 S78 LOCK 목록과 일치(부록 A의 부분집합, 값 동일) |
| F-2 (= 구 1B+1C) | `MainWindow.xaml`의 `Window.Background/Foreground`, `BottomChrome.Background`, `StatusText.Foreground`를 `DynamicResource`(`Legacy.*`)로 교체, `ApplyTheme`를 사전 교체 헬퍼로 변경하고 로컬 색 대입 제거 | `MainWindow.xaml` ≤+8줄, `MainWindow.xaml.cs` ≤+20줄(ApplyTheme 영역), `App.xaml.cs` ≤+20줄(헬퍼) | **없음**: `Legacy.*`는 현재 값 그대로(Dark: #121212/#F0F0F0/#E6121212/#A0FFFFFF, Light: #F2F2F2/#1A1A1A/#E6F2F2F2/#A0000000). Light에서 chrome 외 영역이 바뀌는 항목은 이 Atomic에 없음 | 빌드 통과. Dark↔Light↔System 전환 시 예외 없음(내부 확인). `ApplyTheme`의 `SolidColorBrush`/`Color.From*` 대입 0. 시작 시 키 누락 크래시 없음(DynamicResource만 사용) |
| F-3 | `Window.Resources`의 스타일 3개(`TransportButton`, `ChromeCaption`, `SidePanel`)를 `Themes/Controls.xaml`로 **그대로 이동**하고 `App.xaml`에 병합 | `Controls.xaml` 신규 ≤60줄, `MainWindow.xaml` ≤-45줄, `App.xaml` ≤+2줄 | **없음**(값 변경 금지, 이동만) | 빌드 통과. 3개 스타일이 `StaticResource`로 해석됨(App 수준 병합이 먼저 로드됨). `x:Name`·핸들러 이름 불변 |

이후 순서(컴포넌트 Atomic은 Phase 2 이후, 한 Atomic당 컴포넌트 하나, 하드코딩 색은 그 컴포넌트 안에서만 `Lumen.*`로 교체):
- 구 1D(아이콘 Geometry 사전)는 **Phase 4 첫 Atomic(LUM-4A)**으로 이동한다. Phase 1에서는 만들지 않는다.
- 구 1E(모션 헬퍼, 감속 모션)는 **Phase 7**로 미룬다.
- 하드코딩 색 린트 목표(`MainWindow.xaml`에서 영상 `#000000` 외 0)는 Phase 1이 아니라 컴포넌트 Atomic이 모두 끝난 Phase 10 점검 항목이다.

Phase 1은 **고위험(재생·시크·충돌 등) 아님** → 사용자 Windows 요청 없음. Light 테마 완결(Phase 9)까지 Light는 `Legacy.*` 유지.

## 7. 검증 방법 (검증·검수용 초안, Phase 1 범위)

1. 정적: 토큰 키·값 대조(§2 표 ↔ xaml), 하드코딩 색 린트(1B 후 영상 `#000000` 외 0), `FocusVisualStyle=null` 잔존 여부(Phase 4 완료 시 0), 대비 계산표 재계산.
2. 빌드: 0 error / 0 warn, 18/18 테스트.
3. 렌더(없음): Phase 1에서는 렌더 증거를 요구하지 않는다. Phase 4 이후 박스 또는 기존 사용자 캡처 형식(%DPI 125%)을 기준 비교선으로 쓴다. 비교선(현재 측정): 버튼 50px(125%), 간격 10px, 숨김 띠 113px, 비활성 Next #F4F4F4.
4. 합격 판정은 Phase 11 Gate까지 **NOT EVALUATED**.

## 8. 결정 사항 (총괄 확정 2026-10-03)

| ID | 결정 |
|---|---|
| O1 | 보조 컨트롤 5개(FrameBack, FrameStep, Screenshot, Info, Theme)의 노출은 지금 바꾸지 않는다. Phase 2에서 구분선 그룹핑만 한다 |
| O2 | Accent 가칭(Lumen Blue) 유지 |
| O3 | `Segoe UI Variable Text, Segoe UI` 폴백 승인. 시간 숫자(탭형 vs Consolas)는 렌더로 확정 |
| O4 | Dark Gate 우선, Light는 Phase 9 완결(Light 사전은 F-1에서 만들어 둠) |
| O5 | 아이콘은 자체 제작 Geometry |

수석 검토 완료: 조건부 승인(F-1/F-2/F-3 분할, Legacy.* 보존, 1E→Phase 7).

## 9. 한계

- 모든 판정은 코드 정적 읽기(E2)와 사용자 캡처 3장·영상 1건의 픽셀 측정에 한정된다. 색·대비 값은 계산(E3)이며 렌더 검증 전이다.
- 아이콘 Path 예시는 출발점일 뿐이고 광학 정렬은 Phase 4 렌더로 확정한다.
- 이 문서는 구현 지시가 아니다. 착수는 수석 구조 검토 후 총괄 LOCK.
- UI PASS / Visual PASS = NOT EVALUATED. Soft ≠ PASS. Windows NOT TESTED. Blur/Acrylic/P1 HOLD.

## 부록 A. Lumen 키·값 표 — 설계 전체 집합 (Dark/Light 키 집합 동일)

**S78(F-1) 적용 범위 주의**: 수석이 줄 수 상한(합계 ≤150)과 소비처 0 원칙으로 아래 키 중 `Icon.Default`, `Icon.Active`, `Focus.Ring`, `State.SelectedOverlay`, `LineHeight.*`, `Weight.Title`을 F-1에서 제외했다. 이 키들은 **처음 쓰는 컴포넌트 Atomic에서 추가**한다(추가 이유 없음, 동의). 수석 목록에는 테마 무관 Legacy 브러시 14개(기존 하드코딩 색 보존, Shared)도 있다. 색은 `Color`+`Brush` 쌍으로 정의한다(아래 Brush 표기는 쌍의 대표 이름). 박스 초안(98줄)은 Brush 단일 형식이었으므로 쌍 형식의 줄 수는 수석 구현 기준을 따른다.

네임스페이스 접두 `Lumen.`은 키 앞에 항상 붙는다. 색은 `Color`+`SolidColorBrush`(`po:Freeze="True"`) 쌍. 초안 xaml은 박스에서 XML 구문 검사와 키 집합 diff를 통과했고, 총 98줄(Shared 38, Dark 30, Light 30)이다. WPF 컴파일은 하지 못했다.

### A.1 Lumen.Tokens.Shared.xaml (테마 무관)

| 키 | 형식 | 값 |
|---|---|---|
| `Space.1/2/3/4` | Double | 4 / 8 / 16 / 24 |
| `Size.Hit` | Double | 40 |
| `Size.Icon` | Double | 24 |
| `Size.TimelineHit` | Double | 24 |
| `Size.TrackRest` / `Size.TrackHover` | Double | 4 / 6 |
| `Size.ThumbRest` / `Size.ThumbHover` | Double | 12 / 14 |
| `Size.VolumeWidth` | Double | 96 |
| `Type.Caption` / `Type.Body` / `Type.Title` / `Type.Osd` | Double | 12 / 14 / 14 / 28 |
| `LineHeight.Caption` / `LineHeight.Body` / `LineHeight.Osd` | Double | 16 / 20 / 36 |
| `Radius.Control` / `Radius.Panel` / `Radius.Tooltip` | CornerRadius | 6 / 10 / 6 |
| `Font.Ui` | FontFamily | Segoe UI Variable Text, Segoe UI |
| `Font.Mono` | FontFamily | Consolas |
| `Weight.Title` | FontWeight | SemiBold |
| `Motion.Fast` / `Motion.Base` / `Motion.Slow` | Duration | 0:0:0.120 / 0.150 / 0.180 |
| `Ease.Out` / `Ease.In` | CubicEase | EaseOut / EaseIn |

### A.2 Lumen.Colors.Dark.xaml / Lumen.Colors.Light.xaml (색)

| 키 | Dark | Light | 메모 |
|---|---|---|---|
| `Surface.Window` | #121212 | #F4F4F6 | |
| `Surface.Chrome` | #1A1A1C | #F4F4F6 | |
| `Surface.Raised` | #222225 | #FFFFFF | |
| `Border.Subtle` | #2E2E32 | #DADADF | |
| `Text.Primary` | #F5F5F7 | #1A1A1C | 대비 15.96 / 15.82 |
| `Text.Secondary` | #B4B4BA | #55555C | 8.42 / 6.73 |
| `Text.Tertiary` | #8E8E96 | #6C6C74 | 5.35 / 4.74 |
| `Icon.Default` | #B4B4BA | #55555C | Text.Secondary와 같은 값 |
| `Icon.Active` | #F5F5F7 | #1A1A1C | |
| `Icon.Disabled` | #5A5A62 | #A0A0A8 | 비활성은 3:1 면제, 배경보다 확실히 어둡게 |
| `Accent` | #7AA7FF | #2B5BD0 | 7.28 / 5.45 (가칭) |
| `Accent.OnAccent` | #0B0B0C | #FFFFFF | 8.24 / 5.98 |
| `State.HoverOverlay` | #14FFFFFF | #0F000000 | α 8% / 6% |
| `State.PressedOverlay` | #24FFFFFF | #1F000000 | α 14% / 12% |
| `State.SelectedOverlay` | #247AA7FF | #242B5BD0 | Accent α14% |
| `Track.Rail` | #5C5C62 | #B8B8C0 | 장식 요소 |
| `Track.Buffer` | #597AA7FF | #4D2B5BD0 | Accent α35% / 30% |
| `Danger` | #FF6B6B | #C62828 | 6.26 / 5.12 |
| `Focus.Ring` | #7AA7FF | #2B5BD0 | |
| `Legacy.Window.Bg` | #121212 | #F2F2F2 | 현재 코드 값(F-2에서 사용) |
| `Legacy.Window.Fg` | #F0F0F0 | #1A1A1A | 현재 코드 값 |
| `Legacy.Chrome.Bg` | #E6121212 | #E6F2F2F2 | 현재 코드 값 |
| `Legacy.Status.Fg` | #A0FFFFFF | #A0000000 | 현재 코드 값 |

`Legacy.*` 값의 출처는 `MainWindow.xaml`(Window/BottomChrome/StatusText 초기값)과 `MainWindow.xaml.cs`의 `ApplyTheme`(Dark/Light 분기)이며 `97c0360` 기준이다. `Legacy.*`는 Phase 2 이후 컴포넌트가 Lumen 키로 옮겨가면 제거한다(제거 Atomic은 Phase 10 점검).
