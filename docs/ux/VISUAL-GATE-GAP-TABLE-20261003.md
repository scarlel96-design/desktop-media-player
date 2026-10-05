# Visual Gate 격차 표 — Lumen / D1–D5 / AC-UI-1~9 / Apple-level Visual Quality (문서 전용 · 코드 변경 없음)

| 항목 | 값 |
|---|---|
| 작성 | UI·UX 봇, 2026-10-03 KST (총괄 요청, 사용량 GREEN 03:30 확정 후) |
| 대상 빌드 | Canonical `97c0360` (S71) · 사용자 피드백 빌드 `7d92c06` (S70) |
| 상태 | **Soft ≠ PASS. Windows NOT TESTED. Visual PASS 아님. UI/UX IMPLEMENTATION + VISUAL QUALITY는 Diagnostic과 별개 독립 Gate.** 현 프로토타입 외형을 Release/Visual baseline으로 삼지 않는다. 구현 LOCK은 별도 총괄 결정. |
| 기준 우선순위 | 사용자 요구 > Lumen > Visual Quality Gate > 타 플레이어 참고 (복제·혼합 금지) |
| 격차 판단 제외 | Blur·투명도·모서리 곡률의 부재는 격차로 쓰지 않는다(Blur 기본 OFF, 측정 전 HOLD). |

## 0. 증거 등급 (이 표의 모든 판정에 붙는다)

| 등급 | 뜻 |
|---|---|
| E1 사용자 Windows 직접 관찰 | 사용자 진술(로그·캡처 없음). 원문은 총괄 전달 요지. |
| E2 코드 정적 확인 | `src/DesktopMediaPlayer.Shell/MainWindow.xaml` / `MainWindow.xaml.cs` @ `97c0360`를 읽어 확인한 사실. **렌더 결과가 아님.** |
| E3 계산 | E2 수치로 계산한 값. 렌더 미확인. |
| E4 진단 스크린샷 | S69 정지 화면 1장(투자자 봇 제공, 미디어 미로드). 진단 빌드 증거. **UI 구현 완료 근거로 쓰지 않음.** 이 문서 작성 시 박스에서 파일을 찾지 못해 이미지 자체는 재검토하지 못했다. |
| EVIDENCE INSUFFICIENT | 렌더·동작 증거가 없어 판정 불가. 아래 §4 증거 목록으로 채운다. |

판정 표기: **GAP**(기준 미달 확인) / **GAP 후보**(E2·E3 근거, 렌더 확인 필요) / **OK(E2)**(코드상 기준 충족, 렌더 미확인) / **EVIDENCE INSUFFICIENT**.

## 1. 사용자 직접 피드백 2건 (빌드 7d92c06, E1)

| # | 피드백 | 분류 | 코드 관찰(E2) | 원인 | 제안 규칙(Lumen) |
|---|---|---|---|---|---|
| U1 | 재생바를 클릭해 이동할 때마다 컨트롤 오버레이가 다시 나타나 거슬린다. | Interaction continuity GAP (UI/UX Gate, Known Issue 아님) | `Root_MouseMove`가 모든 마우스 이동마다 `ShowChrome()` + `ArmAutoHide()`. `TryAutoHideChrome`은 2.5초 타이머로 `BottomChrome.Opacity=0, IsHitTestVisible=false`이며, `ShouldPauseAutoHide()`에 **포인터가 chrome 위에 있는지** 조건이 없다. | **미확정(가설)**: 포인터가 바 위에서 가만히 있으면 2.5초 뒤 chrome이 포인터 밑에서 사라지고, 다시 움직이면 즉시 재등장하는 흐름으로 보임. 렌더·재현 로그 없음. | 포인터가 chrome 위에 있는 동안 숨기지 않기. 숨김은 포인터가 chrome을 떠난 뒤 2.5초. 재표시는 의미 있는 이동(작은 떨림 무시)에서만, Opacity 120–180ms 페이드. 바 클릭 seek는 chrome 상태를 바꾸지 않기. |
| U2 | 재생·일시정지 버튼이 하나로 통합되지 않았다(상태에 따라 토글되는 단일 버튼 기대). | State feedback / hierarchy GAP (UI/UX Gate) | `MainWindow.xaml`에 `PlayButton`(▶)과 `PauseButton`(⏸)이 **별개 버튼**으로 나란히 있고, `StopButton`(⏹)도 이웃. 상태에 따른 표시 전환 코드 없음(Play/Pause 버튼 참조는 클릭 핸들러뿐). | 설계 부채(기능 변경 아님, Shell 표시 규칙). | 단일 Play/Pause 토글(재생 중 ⏸, 그 외 ▶). 기본 전환 120ms 이내. 접근성 이름·툴팁도 상태에 맞춰 변경. Stop은 보조 위계로 분리 검토. Facade 호출·Architecture 변경 없음. |

## 2. 12개 평가 항목 표

| # | 평가 항목 | Lumen 기준 | 현재 근거 | 등급 | 판정 |
|---|---|---|---|---|---|
| 1 | Hierarchy | 재생이 주 동작, 보조는 조용히. L1≤2. | 16개 버튼(Open·Prev·Play·Pause·Stop·Next·FrameBack·FrameStep·Mute·Sub·Audio·Playlist·Screenshot·Info·Theme·Fullscreen)이 모두 같은 `TransportButton` 스타일(40×40, #2A2A2A, 테두리 #3A3A3A). Play에 주 동작 강조 없음. | E2 | **GAP 후보** |
| 2 | Alignment | 정렬 기준선 명확(D1). 좌/우 그룹 균형. | 시간 행(28)·간격(8)·컨트롤 행(48), 좌우 패딩 16로 기준선은 있음. 그러나 컨트롤 합계폭이 크다(§3 E3): 좌측 392 + 볼륨 160 + 우측 328 = 880, 패딩 32 포함 912. | E2/E3 | 기준선 **OK(E2)**, 폭 초과는 **GAP 후보**(창 폭 640~944에서 잘림·겹침 가능, 렌더 미확인) |
| 3 | Density | 한 줄에 필요한 것만, 일정한 밀도(D1). | 한 행에 16개 컨트롤 + 볼륨 슬라이더. 프레임 단위 이동 2개, 스크린샷, 정보, 테마가 상시 노출. | E2 | **GAP 후보**(D1 미해소로 읽힘) |
| 4 | Typography | 시간 Caption 11–12 탭형 숫자, 충분한 대비(D5). | 시간은 Consolas 12, #C8C8C8 on 불투명도 90% #121212. 전체 텍스트 스케일·가중치 체계 없음(타이틀만 SemiBold, 상태 28). | E2 | 시간 서체는 탭형이라 방향은 **OK(E2)**, 대비·위계 **EVIDENCE INSUFFICIENT**, 타입 스케일 부재는 **GAP 후보** |
| 5 | Icon consistency | 24 그리드, 단일 아이콘 체계(D4, AC-UI-4). | 아이콘이 이미지/벡터가 아니라 **텍스트 글리프**: ⏏ ⏮ ▶ ⏸ ⏹ ⏭ "⏮\|" "\|⏭" 🔊 CC ♪ ≡ 🖼 ℹ ◐ ⛶. FontSize 14(Open만 16). 컬러 이모지(🔊 🖼)와 단색 기호가 섞이고, "CC"는 문자. 24 그리드 미준수. 글리프 모양은 폰트 대체에 의존. | E2 | **GAP** (AC-UI-4 icon 24 / D4) |
| 6 | Spacing | 8/16 그리드. | 버튼 마진 8, 그룹 16, 행 28/8/48, 패딩 16·8, 플라이아웃 마진 16. 8/16 규칙은 코드상 대체로 준수. 플라이아웃 하단 마진 108은 chrome 높이를 가정한 하드코딩. | E2 | **OK(E2)** (하드코딩 108은 위험 메모) |
| 7 | State feedback (hover/press/focus/selected/disabled) | 모든 상태가 시각적으로 구분, 정제된 피드백. | `TransportButton`은 Style 세터만 있고 **ControlTemplate·트리거 없음**. 기본 WPF 버튼 템플릿의 hover/press 색(밝은 파랑 계열)이 어두운 UI와 충돌할 가능성이 있으나 렌더 미확인. `FocusVisualStyle=null`로 **키보드 포커스 표시 제거**. Mute/Pin 등 토글 상태 시각 구분 코드 없음(Play/Pause 표시 전환도 없음). disabled는 Prev/Next만 기본 템플릿. | E2 | 포커스 표시 제거는 **GAP**, hover/press/disabled는 **EVIDENCE INSUFFICIENT**(렌더), 선택/토글 상태는 **GAP 후보** |
| 8 | Animation quality | 120–180ms 단축 모션, Opacity 중심. | chrome show/hide는 `Opacity`를 즉시 대입(애니메이션 없음, `Storyboard`/`DoubleAnimation` 없음). OSD 페이드는 `DispatcherTimer` 50ms 스텝으로 수동 처리. | E2 | **GAP** (Premium Motion 미구현) |
| 9 | Transition quality | 패널·플라이아웃 전환 매끄러움, 레이아웃 점프 없음. | Playlist는 열 폭을 0↔280~320으로 바꾸고 Visibility 전환 → 영상 영역 폭이 즉시 변함(HWND 재배치). 플라이아웃도 Visibility 즉시 전환. 숨김 시 chrome 행은 유지되고 Opacity만 0이라, 창 배경 #121212와 영상 #000 사이 띠가 보일 가능성(렌더 미확인). | E2 | **GAP 후보** (띠는 EVIDENCE INSUFFICIENT) |
| 10 | Interaction continuity | 동작이 끊기지 않고 예측 가능. | U1·U2(§1) 사용자 관찰. seek는 S70/S71에서 클릭·드래그 개선(사용자 정상 진술, 로그 증거 없음). | E1/E2 | **GAP** (U1·U2) |
| 11 | Visual continuity | 상태·창 크기·테마·전체화면에서 일관. | 색이 컨트롤마다 하드코딩 16진(#121212·#2A2A2A·#3A3A3A·#4466AA·#E6121212 등). `App.xaml` 리소스 비어 있음 = 디자인 토큰/공용 팔레트 없음. 테마 버튼(Dark/Light/System)이 있어도 XAML 값은 고정 색. 전체화면 chrome 모습은 증거 없음. | E2 | 토큰 부재 **GAP 후보**, 전체화면·테마 전환은 **EVIDENCE INSUFFICIENT** |
| 12 | 전체 시스템 통일감 | 하나의 디자인 언어(Lumen). | 1~11 합산. 위계·아이콘·모션·상태·토큰이 한 체계로 묶여 있지 않음. | E2 종합 | **GAP 후보** (렌더 증거 후 확정) |

## 3. D1–D5 · AC-UI-1~9 항목별 대조 (코드 정적 기준, Windows NOT TESTED)

| ID | 기준 | 현 근거 | 대조 |
|---|---|---|---|
| D1 Chrome 밀도·정렬 | 정렬 기준선 있고 밀도 균형 | 기준선은 있으나 컨트롤 16개 고밀도, 폭 912(E3) | **미해소 후보** |
| D2 Timeline | hit 좁음·buffer/hover time·안정 Seek | Slider 높이 24, 버퍼 바 4, hover 시간 Popup 존재, `IsMoveToPoint`+S70/S71 seek 개선 | 코드상 **개선됨(E2)**, 클릭·드래그 체감은 사용자 정상 진술뿐이라 **Windows 증거 부족** |
| D3 Volume | 폭 88–104, mute 묶음 명확 | 슬라이더 96(88–104), Mute+슬라이더를 `VolumeGroup`으로 묶음 | **OK(E2)**, 시각 비율은 **EVIDENCE INSUFFICIENT** |
| D4 아이콘·컨트롤 크기 | 24 그리드 | 버튼 40×40은 충족, 글리프 14(Open 16)로 24 미충족, 체계 혼재 | **미충족** |
| D5 시간 타이포 | 가독·대비 | Consolas 12, #C8C8C8 | **EVIDENCE INSUFFICIENT**(대비 렌더 확인 필요) |
| AC-UI-1 | Timeline Seek·buffer·hover·hit≥24 | 높이 24, 버퍼·hover 있음 | E2 충족 후보, Windows 증거 부족 |
| AC-UI-2 | Current/Total 가독, unknown=--:-- | 기본 텍스트 `--:--` | E2 충족, 가독은 **EVIDENCE INSUFFICIENT** |
| AC-UI-3 | Volume 폭·mute·wheel | 폭 96, Mute, `VolumeGroup_PreviewMouseWheel` | E2 충족 후보 |
| AC-UI-4 | Transport 40×40 · icon 24 | 40×40 충족, icon 14/16 | **부분 미충족(icon)** |
| AC-UI-5 | Sub/Audio flyout 즉시·비모달 | `TrackFlyout` Border, 즉시 표시 | E2 충족 후보 |
| AC-UI-6 | Playlist ≡ 패널·Empty CTA | `PlaylistPanel` 280–320, 빈 상태 문구 "No items — Open a file to start."(CTA 버튼은 하단 Open…) | E2 충족 후보 |
| AC-UI-7 | FS 버튼+더블클릭 · FS에서 Lumen chrome | 버튼 있음, L0 더블클릭 `ToggleFullscreen` | 동작 E2 확인, **FS chrome 모습 EVIDENCE INSUFFICIENT** |
| AC-UI-8 | 8/16 spacing·Blur OFF·Opacity-only·L1≤2 | 8/16 준수, Blur 없음, show/hide Opacity만 | **OK(E2)** |
| AC-UI-9 | Content-first·Invalidate 스팸 없음 | 구조상 chrome 별도 행, 성능은 별도 측정 | E2 구조 확인, 성능 **측정 대기(별도)** |

AC-UI 충족 후보는 코드 존재 확인일 뿐이며 **Windows 검증 PASS가 아니다**.

## 4. 추가 증거 목록과 사용자 캡처 방법 제안

필요 이유: 현재 렌더 증거는 S69 정지 화면 1장(E4)뿐이라 hover/press/focus, chrome 숨김 띠, 전체화면, 모션 같은 항목은 판정할 수 없다.

| # | 대상 | 확인할 항목(평가 항목 번호) | 캡처 방법 제안(사용자 부담 최소) |
|---|---|---|---|
| 1 | 재생 중 기본 화면(영상 1개, 기본 창 960×640) | 1, 2, 3, 4, 5, 6 | Win+Shift+S로 창 전체 1장. |
| 2 | 일시정지 화면 | 1, 7, U2 | 같은 방식 1장. |
| 3 | chrome 표시/숨김 | 8, 9, 10, U1 | 재생 중 마우스를 멈춰 2.5초 뒤 chrome이 숨은 상태 1장 + 마우스를 움직여 나타난 상태 1장(띠 확인). |
| 4 | hover/press/focus/disabled | 7 | 재생 버튼 hover 1장, 마우스 누른 채 1장(어렵다면 생략), Tab 키로 포커스 이동 후 1장, Prev/Next 비활성 상태 1장. |
| 5 | 자막·OSD | 4, 11 | 자막이 있는 영상과 볼륨 OSD가 보이는 순간 각 1장. |
| 6 | 전체화면 | 11, AC-UI-7 | F11 또는 ⛶ 후 chrome 표시 상태 1장. |
| 7 | 창 폭 축소 | 2, 3 | 창을 가장 좁게(640 부근) 줄인 상태 1장(우측 버튼 잘림 확인). |
| 8 | 짧은 동작 영상(5~10초) | 8, 9, 10, U1 | Win+G 또는 Win+Alt+R로 화면 녹화: 재생바 클릭 → chrome 반응, 재생/일시정지 클릭, Playlist 열기·닫기. |
| 9 | 테마 | 11 | ◐ 버튼으로 Dark/Light/System 전환 각 1장. |

제출 형식: 이미지/영상 파일을 채팅에 첨부하고 "빌드 폴더명"만 같이 적으면 충분. 재현 로그(`spike.log`)는 UI 판정엔 불필요.

## 5. 상위 격차 요약

1. **GAP(확인)**: 아이콘이 텍스트 글리프 14px로 24 그리드·단일 체계 미충족(5, D4, AC-UI-4) / 키보드 포커스 표시 제거(7) / chrome 표시·숨김과 패널 전환에 모션 없음(8) / 재생·일시정지 분리(U2) / 바 클릭 후 chrome 재등장(U1, 원인 가설만).
2. **GAP 후보(렌더 확인 필요)**: 16개 컨트롤 고밀도와 폭 912(1~3) / 기본 hover·press 템플릿과 어두운 UI 충돌(7) / chrome 숨김 시 띠(9) / 디자인 토큰 부재(11).
3. **OK(E2)**: 8/16 spacing, Blur OFF·Opacity-only, 볼륨 폭 88–104, Transport 40×40.
4. **판정 불가(EVIDENCE INSUFFICIENT)**: 대비, 전체화면, 테마 전환, hover/press 실제 모습, 시간 가독.

## 6. 한계

- 코드 정적 읽기(E2/E3)는 렌더가 아니다. 실제 모습과 다를 수 있다.
- 사용자 피드백 원문 전체는 UI·UX 봇에 도달하지 않았고 총괄 전달 요지를 썼다.
- 이 표는 구현 지시가 아니다. 구현 범위·착수는 총괄 Gate 결정 후이며, 기능·Architecture 변경이 필요한 항목은 기획·총괄에 제안한다.
- Soft ≠ PASS. Windows NOT TESTED. KI-022/024는 Windows 재검증 대기 유지, Blur/Acrylic/P1 HOLD.
