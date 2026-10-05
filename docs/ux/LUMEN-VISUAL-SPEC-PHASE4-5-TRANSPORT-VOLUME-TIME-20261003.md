# Lumen Visual Spec — Phase 4 (Transport) · Phase 5 (Volume / Time)

| 항목 | 내용 |
|---|---|
| 작성 | UI·UX 봇, 2026-10-03 (문서 전용, 코드 변경 없음) |
| 상태 | **Rev2** — 수석 조건부 승인(총괄 전달) 7개 항목 반영. Rev1에서 바뀐 곳은 §9 개정 이력 참조 |
| 선행 | Phase 1 Foundation Rev3, Phase 2·3 Rev4(T3-2 신규 색 키 0, `Focus.Ring`·`Track.*` 전용 토큰은 Phase 7 이월) |
| 증거 등급 | 코드는 내 체크아웃 `97c0360`(S71)의 정적 읽기(E2). S72~S79 이후 변경(특히 S74 Play/Pause 토글)은 **미확인**. 아이콘 도형은 박스에서 래스터 미리보기로 측정(E3). WPF 렌더 **NOT TESTED**. **UI PASS / Visual PASS = NOT EVALUATED, Soft ≠ PASS** |
| 미리보기 | `docs/ux/LUMEN-ICON-PREVIEW-TRANSPORT-20261003.png` (전송 계열 아이콘 8종, 박스 PIL 렌더) |

## 0. 요약

1. 현재 버튼은 이모지 글리프(🔇/🔊 등)와 WPF 기본 상태 색(hover #C2E7FB 등)을 쓴다. Phase 4는 이것을 **자체 제작 Geometry 아이콘 20종 + 하나의 `LumenIconButton` 템플릿**으로 바꾼다. 상태는 Rest / Hover / Pressed / Disabled / Selected이고 모두 색과 Opacity 전환만 쓴다.
2. 계층은 두 단계다. **1단계는 Play/Pause**(원형 면 + 밝은 아이콘, 40×40 유지). 나머지는 모두 2단계(면 없음, hover 때만 면).
3. 아이콘 소비 방식은 `Content`에 Geometry를 넣고 템플릿이 `Path`로 그리는 것이다. 그래서 `.cs`가 `Content`에 문자열을 넣는 곳(뮤트, 테마, 전체화면, S74의 Play/Pause)은 **같은 Atomic에서 함께** 바꿔야 한다. 대상 줄은 §2.5에 있다.
4. Phase 5는 볼륨 슬라이더 템플릿(T3-2와 같은 구조), 시간 라벨 위계(현재 시간 Primary, 전체 시간 Secondary), 그리고 열린 결정 2개(O8, O9)다.
5. **신규 색 키 0을 목표**로 한다(총괄 정정 방침). 기존 `Text.*`와 `State.*` 키로 충당하고, 없으면 구현이 diff로 확인해 보고한다.
6. 포커스 링은 Phase 7 이월이다. 지금(`FocusVisualStyle=null`)보다 나빠지지 않는다.

## 1. 전제와 한계

- S74에서 Play/Pause가 단일 토글이 되었다고 가정한다(총괄 순서 U2). **버튼의 최종 `x:Name`과 `.cs` 갱신 위치는 내가 확인하지 못했다.** P4-4 LOCK 때 구현이 현재 이름과 `Content` 대입 줄을 알려 주어야 한다.
- 현재 코드(E2, `97c0360`)에서 아이콘 문자열을 `.cs`가 바꾸는 곳: 뮤트 `MuteButton.Content = muted ? "🔇" : "🔊"`(약 1049행), 테마 `ThemeButton.Content = _theme switch …`(약 2041행), 전체화면은 툴팁만 바꾸고 `Content`는 정적(1764/1771행). 최신 코드에서 줄 번호가 달라졌을 수 있다. 수용 기준의 `rg`로 재확인한다.
- Prev/Next의 `IsEnabled`는 `.cs`가 제어한다(465~471행). 템플릿은 `IsEnabled`만 보고 상태를 그리므로 `.cs` 변경이 필요 없다.
- 박스는 WPF를 렌더하지 못한다. 시각 확인은 Gate 스냅샷에서 한다. 사용자에게 루틴 캡처를 요청하지 않는다.

## 2. Phase 4 — Transport

### 2.1 계층과 면

| 단계 | 버튼 | 크기 | Rest 면 | Rest 아이콘 색 | Hover 면 | Pressed 면 |
|---|---|---|---|---|---|---|
| 1 | Play/Pause | 40×40 원형(반경 20) | `Lumen.State.HoverOverlay` | `Lumen.Text.SecondaryBrush` | `Lumen.State.PressedOverlay` | `Lumen.State.PressedOverlay` |
| 2 | 나머지 전부 | 40×40, 반경 `Radius.Control` | 없음(Transparent) | `Lumen.Text.SecondaryBrush` | `State.HoverOverlay` | `State.PressedOverlay` |

- **Play/Pause 상태 매핑(Rev2 확정, 신규 키 0)**: Rest 면 = Hover 오버레이, Hover 면 = Pressed 오버레이, Pressed 면 = Pressed 오버레이, 아이콘 Rest = Secondary. Hover와 Pressed의 면이 같아지므로 구분은 **아이콘 색**으로 한다(Hover·Pressed에서 Text.Primary). 그래서 눌림은 면 변화 없이 아이콘이 밝아진 상태로 보이며, 면 단계 구분은 Phase 7 모션/토큰에서 보강한다.
- 이유: 현재는 모든 버튼이 같은 비중이라 주 동작이 보이지 않는다(격차 표 항목 4). 크기를 키우면 폭 모델(약 892 DIP)이 깨지므로 **크기가 아니라 면과 색**으로 강조한다.
- Hover 때 아이콘 색은 `Text.Primary`로 올린다(2단계). 이렇게 하면 비활성 Next가 가장 밝게 보이던 문제(캡처 검토 확정)가 없어진다.
- Disabled는 아이콘 색 `Lumen.Icon.DisabledBrush`, 면 없음, hover 없음. 아이콘이 Rest보다 확실히 어두워야 한다(Dark 기준 대비 계산은 구현 시 실제 값으로 재계산).
- 상태 전환은 `Trigger`의 `Setter`로 **즉시 전환**한다(스토리보드 없음). 120ms 페이드는 Phase 7.
- 금지: `Effect`, `OpacityMask`, `AllowsTransparency`, 레이아웃 애니메이션.

### 2.2 아이콘 규격

- 그리드 24×24, 안전 영역 2 DIP(내용은 2~22). 버튼 40 안에서 아이콘 24, 패딩 8.
- **채움형**(전송 계열): `Path`의 `Fill`과 `Stroke`를 같은 브러시로, `StrokeThickness=1.5`, `StrokeLineJoin=Round`로 모서리를 둥글린다. 아래 좌표는 **코어 도형**이고, 시각 외곽은 사방 0.75 커진다.
- **외곽선형**(프레임 이동 두 개, 보조/정밀 동작을 뜻함): 사전에는 **EvenOdd 링 도형**(path 맨 앞 `F0 `, `FillRule` 속성 금지)으로 저장해서 템플릿 하나로 그린다. 템플릿 `Stroke 1.5`가 사방 0.75를 더하므로 **링 코어 두께는 0.5 안팎, 시각 두께는 약 2.0**이다(수석 확인 항목, S88에서 같은 규칙 적용).
- **선 요소 일반 규칙(S89로 확정)**: 스펙의 "두께 1.5 호/선"은 `Stroke`가 1.5를 더하므로 코어 두께 0이 되어 그릴 수 없다. 모든 선 요소는 **코어 두께 0.5 이상의 링/막대 면**으로 그리고 시각 두께는 코어 + 1.5 = **2.0**이다. 11종의 편차표는 `LUMEN-ICON-GEOMETRY-P4-2-S89-20261003.md`에 있다.
- 정렬 기준은 **bbox 중심이 아니라 면적 무게중심이 12,12**이다(Play는 무게중심 11.90, 시각 bbox 중심은 오른쪽). 측정값은 아래 표.
- 모든 Geometry는 `Freeze`(`po:Freeze="True"`). 키 이름은 `Lumen.Icon.<Name>`, 형식은 `Geometry`.

**인벤토리(20종 + Phase 6용 Close 1종)**

Open, Prev, Play, Pause, Stop, Next, FrameBack, FrameStep, VolumeOn, VolumeOff, Subtitle, Audio, Playlist, Screenshot, Info, ThemeDark, ThemeLight, ThemeSystem, FullscreenEnter, FullscreenExit. (Close는 Phase 6 플라이아웃에서.)

**전송 계열 8종 기준 도형(구현 출발점, 박스 렌더로 측정)**

| 이름 | Geometry (코어) | 무게중심 | 면적(시각, DIP²) |
|---|---|---|---|
| Play | `M9,6.25 L9,17.75 L17.75,12 Z` | 11.90, 12.00 | 76 |
| Pause | `M8.25,6.25 H10.25 V17.75 H8.25 Z M13.75,6.25 H15.75 V17.75 H13.75 Z` | 12.00, 12.00 | 92 |
| Stop | `M8,8 H16 V16 H8 Z` | 12.00, 12.00 | 90 |
| Prev | `M6.5,6.25 H7.75 V17.75 H6.5 Z M17.5,6.25 V17.75 L10.75,12 Z` | 12.35, 12.00 | 99 |
| Next | `M16.25,6.25 H17.5 V17.75 H16.25 Z M6.5,6.25 V17.75 L13.25,12 Z` | 11.69, 12.00 | 98 |
| Open | `M12,5.75 L6.75,12.5 H17.25 Z M6.75,15.75 H17.25 V16.5 H6.75 Z` | 12.00, 12.13 | 84 |
| FrameStep (외곽선형) | 삼각형 `M6.75,6.5 V17.5 L14.25,12 Z` + 막대 `M17.25,6.5 V17.5`, 두께 2 | 11.65, 12.00 | 82 |
| FrameBack (외곽선형) | 삼각형 `M17.25,6.5 V17.5 L9.75,12 Z` + 막대 `M6.75,6.5 V17.5`, 두께 2 | 12.40, 12.00 | 82 |

- 면적을 76~99 범위로 맞춰 아이콘 간 시각 무게를 비슷하게 했다. Stop은 처음 110이라 90으로 줄였다.
- Prev/Next의 무게중심 편차(12.35/11.69)는 비대칭 도형의 특성이며 허용 범위로 기록한다. 렌더 확인은 Gate에서.
- Prev/Next(채움)와 FrameBack/FrameStep(외곽선)의 구분은 "채움 대 외곽선"이다. 24 DIP에서 구분되는지는 렌더 확인 항목이다(WV 후보, 지금은 NOT EVALUATED).
- 나머지 12종 중 **11종은 S89(P4-2)에서 도형이 확정**되었다(`LUMEN-ICON-GEOMETRY-P4-2-S89-20261003.md`, 시트 `LUMEN-ICON-SHEET-P4-2-20261003.png`). FullscreenExit와 Close는 이번에 제외했다. 아래 개념 설명은 S89 확정 도형과 다르면 **S89 문서가 우선**한다(예: VolumeOn 호는 두께 1.5 선이 아니라 코어 0.5 링 조각, Screenshot은 모서리 브래킷 4개 + 중앙 점, Subtitle은 둥근 사각 링 + 막대 3개).
  - VolumeOn: 스피커 본체 + 호 1~2개(채움+외곽선 혼합 금지, 본체 채움 / 호는 두께 1.5 선). VolumeOff: 본체 + 대각선(호 없음). 이모지 대응 이름이 아니라 상태 이름을 쓴다.
  - Subtitle: 둥근 사각 안에 두 줄(CC가 아니라 자막 줄 표현). Audio: 음표 하나. Playlist: 가로선 3개 + 재생 삼각형. Screenshot: 카메라 대신 모서리 프레임 4개 + 중앙 점. Info: 원 + i.
  - ThemeDark: 달. ThemeLight: 해(원 + 짧은 선 8). ThemeSystem: 반반 원(왼쪽 채움, 오른쪽 외곽선). 현재 테마가 한눈에 구분되어야 한다.
  - FullscreenEnter: 모서리 바깥 화살표 4개. FullscreenExit: 모서리 안쪽 화살표 4개.

### 2.3 `LumenIconButton` 템플릿 (`Themes/Controls.xaml`, F-3 이후)

```
Button  (Focusable 유지, FocusVisualStyle=null 유지 — 포커스 링은 Phase 7)
  Border x:Name="Face"  (CornerRadius=Radius.Control, Background=Transparent)
    Path x:Name="Icon"  (Data={TemplateBinding Content}, Fill/Stroke={TemplateBinding Foreground},
                         StrokeThickness=1.5, StrokeLineJoin=Round, Width=24, Height=24, Stretch=None)
```

| 상태 | 조건 | Face 배경 | Icon 색 |
|---|---|---|---|
| Rest | — | Transparent | `Foreground`(스타일 기본 `Text.Secondary`) |
| Hover | `IsMouseOver` | `State.HoverOverlay` | `Text.Primary` |
| Pressed | `IsPressed` | `State.PressedOverlay` | `Text.Primary` |
| Disabled | `IsEnabled=False` | Transparent | `Lumen.Icon.DisabledBrush` (hover·pressed 무시: 트리거 순서로 최우선) |
| Selected | 스타일 파생에서 DataTrigger | `State.HoverOverlay` 유지 | `Text.Primary` |

- Play/Pause 전용 스타일 `LumenPrimaryIconButton`은 `BasedOn`으로 Face 반경 20, Rest 면 `HoverOverlay`, Hover 면 `PressedOverlay`만 덮어쓴다(아이콘 색은 기본 스타일 그대로: Rest Secondary, Hover·Pressed Primary).
- **Selected 대상**은 Playlist와 Info만 지금 반영한다. 방식은 해당 버튼의 인라인 `Button.Style`(`BasedOn="{StaticResource LumenIconButton}"`)에 `DataTrigger Binding="{Binding Visibility, ElementName=<패널 이름>}" Value="Visible"`을 두는 것이다(리소스 사전의 Style에서는 `ElementName`이 풀리지 않으므로 인라인이어야 한다). 버튼당 약 +7줄, `.cs` 0줄.
- Sub/Audio는 하나의 플라이아웃을 공유하므로 어느 쪽이 열렸는지가 XAML만으로는 알 수 없을 수 있다. **Phase 6(플라이아웃)으로 이월**한다. Mute와 Fullscreen은 아이콘 자체가 상태를 보이므로 Selected 면을 쓰지 않는다.
- 기본 WPF 템플릿 대체로 Aero2의 hover #C2E7FB/pressed #C7E6F5/disabled #F4F4F4 색은 모두 사라진다(격차 표 항목 3, 캡처로 확인된 원인).
- 키보드 Space/Enter는 버튼 기본 동작을 유지한다. 마우스로 클릭한 버튼이 포커스를 유지해서 이후 Space가 그 버튼을 다시 누를 수 있는 현상은 지금도 있는 동작이며 Atomic 범위 밖이다(수석 참고용 기록).

### 2.4 소비 방식과 `.cs` 규칙

- XAML: `<Button Style="{StaticResource LumenIconButton}" Content="{StaticResource Lumen.Icon.Prev}" … />` (기존 `x:Name`, `Click`, `ToolTip`, `AutomationProperties`는 그대로).
- `.cs`: `X.Content = "문자열"` → `X.Content = (Geometry)FindResource("Lumen.Icon.Y")`로 바꾼다. **같은 Atomic 안에서 XAML과 `.cs`를 함께 바꾼다**(안 그러면 문자열이 `Path.Data`로 들어가 아이콘이 비거나 바인딩 오류가 난다).
- 수용 기준(Soft): `rg -n "\.Content\s*=" src/DesktopMediaPlayer.Shell/*.cs`에서 아이콘 버튼 대상이 0건. 잔존 문자열 글리프(이모지·기호)도 `rg`로 0건.

### 2.5 Atomic 분할

| ID | 내용 | 파일·줄 상한 | 위험 |
|---|---|---|---|
| P4-1 | `Themes/Lumen.Icons.xaml` 신설, 전송 계열 8종 Geometry + 외곽선형 링 변환. 미사용 상태로 App.xaml 병합 | 신규 ≤100줄, App.xaml +1 | 시각 변화 0. 키 중복·XML 구문만 확인 |
| P4-2 (S89) | 나머지 중 11종 Geometry(VolumeOn, VolumeOff, Subtitle, Audio, Playlist, Screenshot, Info, ThemeDark, ThemeLight, ThemeSystem, FullscreenEnter). **FullscreenExit·Close 제외**. 도형·시트는 UI·UX가 제공 | 신규 ≤120줄 | 시각 변화 0 |
| P4-3 | `LumenIconButton`·`LumenPrimaryIconButton` 템플릿 추가(미적용). 사용 키가 Dark/Light에 있는지 diff | `Controls.xaml` ≤+70줄 | 시각 변화 0 |
| P4-4 | G2(Prev, Play/Pause, Stop, Next)에 적용, Play/Pause 계층 강조. S74의 Play/Pause `Content` 갱신 줄을 Geometry로. **버튼마다 정적 `AutomationProperties.Name`을 넣는다**(토글 상태별 동적 이름은 이번 범위 밖) | `MainWindow.xaml` ≤+30줄(이름 포함 여부는 구현이 줄 수 재산정), `.cs` ≤+8줄(S74 줄 확인 후) | 재생 토글 경로 접점. 핸들러·`Click` 이름 불변 |
| P4-5 | G1(Open), G3(FrameBack, FrameStep) 적용, 정적 `AutomationProperties.Name` 포함 | xaml ≤+12줄 | 낮음 |
| P4-6 | G4 Mute 적용(`.cs` 아이콘 교체 대응), 정적 `AutomationProperties.Name` 포함 | xaml ≤+4줄, `.cs` ≤+6줄 | 낮음 |
| P4-7 | G5·G6(Sub, Audio, Playlist, Screenshot, Info, Theme, Fullscreen) 적용, Playlist·Info Selected, Theme 3상태 `.cs` 대응, 정적 `AutomationProperties.Name` 포함. **Fullscreen은 Enter 아이콘만 정적으로 적용**하고 Exit 동기화(전체화면 상태에 따른 아이콘 교체)는 이번 제외, Phase 6 이후 | xaml ≤+45줄, `.cs` ≤+10줄 | 낮음 |

- 순서: P4-1 → P4-2 → P4-3 → P4-4 → P4-5 → P4-6 → P4-7. 앞의 세 개는 시각 변화가 없어 독립 되돌리기가 쉽다.
- **Validation Batch**: 사용자에게 보이는 변화는 P4-4~P4-7에 몰려 있고 모두 같은 컨트롤 줄이다. 총괄 정책에 따라 이 묶음을 Soft 증거(구문·키 diff·`rg`·테스트)로 마감하고 Windows 스냅샷은 **Gate 시점에 1회**로 모은다. 재생·시크·프레임·충돌 고위험 접점은 P4-4의 Play/Pause 토글 경로뿐이며, 그 판단(Windows 확인 필요 여부)은 총괄에게 맡긴다.

## 3. Phase 5 — Volume · Time

### 3.1 볼륨 (격차 3: D3)

현재: `VolumeSlider`는 기본 WPF 슬라이더(폭 96, 0~100, `TickFrequency=1`, `IsSnapToTickEnabled`). `.cs`는 `Value`, `ValueChanged`(`VolumeSlider_ValueChanged`), 휠(`step`), `_lastAudibleVolume`만 사용하고 템플릿 파트(`PART_Track`)에 의존하지 않는다(E2).

| 항목 | 규격 |
|---|---|
| 템플릿 | `LumenVolumeSlider`. 구조는 T3-2와 같다(`Track` + `Thumb` 1 + 투명 히트 `RepeatButton` 2). 이 슬라이더는 버퍼가 없으므로 **레일도 템플릿이 그린다**(Decrease 쪽 채움 `AccentBrush` 높이 4, Increase 쪽 레일 `Lumen.Track.RailBrush`). |
| 썸 | 폭 14 고정, `Opacity` 0↔1(`Visibility` 금지), 표시 조건 `IsMouseOver` / `IsDragging` / `IsKeyboardFocused`. 외곽선 `Surface.Chrome` 1px. T3-2와 동일 규칙. |
| 폭·높이 | 폭 96 유지(AC-UI-3 범위 88~104), 히트 높이 24, 시각 레일 4. Mute 40 + 간격 8 + 슬라이더 96. |
| 정렬 | 슬라이더 세로 중심을 Mute 버튼 중심(20)과 맞춘다. |
| 핸들러 | `ValueChanged`, 휠, `_lastAudibleVolume` 흐름은 건드리지 않는다. `.cs` 0줄. |
| 접근성 | 기존 `ToolTip`, `AutomationProperties` 유지. |

- 비율 해석(D3): 슬라이더 폭 96은 아이콘 그리드 24의 4배이고 레일 4는 24의 1/6이다. 이 비율을 규칙으로 기록하고 변경하지 않는다.
- **O8 (결정 필요, 권고 있음)**: 볼륨 트랙 클릭의 동작을 시크와 같은 "클릭한 위치로 이동"(`IsMoveToPointEnabled=True`)으로 통일할지. 현재는 기본값(클릭하면 한 페이지 단위로 이동)이다. 사용자에게 보이는 동작 변화이므로 총괄이 결정한다. **권고: 통일(True)**. 리스크는 낮고(`.cs` 0줄) 같은 슬라이더가 같은 방식으로 움직인다.
- **O9 (결정 필요, 권고 있음)**: 볼륨 수준별 아이콘(0 / 낮음 / 높음 3단계)을 둘지. 현재 `.cs`는 muted 여부만 판단한다. 3단계는 `.cs` 판정 추가(약 +10줄)가 필요하다. **권고: 지금은 2단계(VolumeOn/VolumeOff)만**, 3단계는 요청이 있을 때.
- 볼륨 OSD(`Volume 80`, 기존 `SubtitleOsdText`)의 스타일은 Phase 7(OSD)에서 다룬다. OSD 남발 금지 원칙은 유지한다.

### 3.2 시간 라벨 (격차 5: D5)

P2-3에서 레이아웃(MinWidth, 정렬)은 이미 정했다. Phase 5는 위계와 글꼴만 다룬다.

| 항목 | 규격 |
|---|---|
| 위계 | 현재 시간 `Text.Primary`, 전체 시간 `Text.Secondary`. **구분 문자 `/`를 별도 요소로 분리하는 일은 이번 제외**(수석 결정, 기존 요소 구성 유지). |
| 미확정 표시 | `--:--`는 `Text.Tertiary`. `.cs` 변경 없이 `TextBlock`의 `Text` 값을 **`RelativeSource Self` 바인딩 `DataTrigger`**로 판정한다(+약 6줄). |
| 글꼴 | **Consolas Caption 유지**. Segoe UI Variable의 표 형식 숫자 폭(고정 폭 숫자)은 박스에서 검증할 수 없다(글꼴이 없음). Gate 스냅샷에서 숫자 폭이 안정적이면 O3 결정을 다시 연다. 그때까지 숫자 폭 흔들림 방지는 Consolas가 보장한다. |
| 대비 | `Text.Secondary` on `Surface.Chrome`은 Dark 8.42, Light 6.73(계산, 4.5 이상), `Text.Tertiary`는 Dark 5.35(계산). 구현 시 실제 키 값으로 재계산한다. |

### 3.3 Atomic 분할

| ID | 내용 | 파일·줄 상한 | 위험 |
|---|---|---|---|
| V5-1 | `LumenVolumeSlider` 템플릿 추가 + `VolumeSlider`에 적용 | `Controls.xaml` ≤+60줄, `MainWindow.xaml` ≤+3줄, `.cs` 0줄 | 낮음~중간(T3-2와 구조 공유, 볼륨 값 쓰기 경로 불변) |
| V5-2 | 시간 라벨 위계(현재/전체/구분/미확정 색) | `MainWindow.xaml` ≤+14줄, `.cs` 0줄 | 낮음 |
| V5-3 | O8 채택: `IsMoveToPointEnabled=True`. **S87 Windows 결과 이후에 연다** | xaml +1줄 | 낮음 |
| V5-4 | ~~볼륨 3단계 아이콘~~ O9가 2단계로 채택되어 **열지 않음** | — | — |

- 순서: T3-2 이후에 V5-1을 한다(같은 슬라이더 템플릿 패턴을 재사용하고, T3-2의 렌더 결과를 먼저 본다).

## 4. 격차 표 매핑

| 격차 | 해소 Atomic |
|---|---|
| 아이콘·글리프 불일치(항목 1) | P4-1, P4-2, P4-4~7 |
| 상태 색(WPF 기본 hover/pressed/disabled)(3) | P4-3, 적용 시 P4-4~7 |
| 주 동작 위계(4) | P4-3, P4-4 |
| 볼륨 비율(D3) | V5-1 |
| 시간 타이포(D5) | P2-3(레이아웃), V5-2(위계) |
| Play/Pause 분리(U2) | S74(글리프), P4-4(아이콘) |
| 포커스 가시성 | Phase 7 이월 |
| 플라이아웃 Sub/Audio Selected | Phase 6 이월 |

## 5. 토큰

- 신규 색 키 목표 0. 사용 예정 키: `Lumen.Text.{Primary,Secondary,Tertiary,Disabled}Brush`, `Lumen.State.{Hover,Pressed}OverlayBrush`, `Lumen.Surface.ChromeBrush`, `Lumen.AccentBrush`, `Lumen.Track.RailBrush`. **존재 여부는 구현 Atomic이 Dark/Light diff로 확인**하고, 없는 키가 있으면 키 목록을 총괄에게 보고한 뒤 첫 소비처에서 추가한다(이전 정정 방침과 같음).
- 크기 키: `Lumen.Size.Hit`(40, 버튼 크기)와 `Lumen.Size.Icon`(24, 아이콘 크기)을 쓴다. `Lumen.Size.IconButton`이라는 키는 쓰지 않는다(수석 정정).
- Geometry는 색이 아니므로 위 목표와 별개다. `Lumen.Icon.*`는 Dark/Light 공통이므로 `Lumen.Icons.xaml` 한 파일에 둔다.

## 6. 검증 증거 계획 (Soft, 사용자 요청 없음)

- 구문: 변경 XAML 전부 XML 파싱 OK, 리소스 키 중복 0, Dark/Light 키 집합 동일.
- `rg`: 아이콘 버튼 대상의 `.Content =` 문자열 0건, 이모지 글리프 0건, `x:Name`·`Click` 이름 diff 0.
- 아이콘: P4-1/P4-2의 미리보기 시트에서 무게중심 12±0.5, 면적 70~100 범위.
- 시각: Windows 렌더는 NOT TESTED. Gate에서 1회 스냅샷으로 모은다. 확인 항목 후보: Prev/Next 대 Frame 구분, Play/Pause 계층, Disabled 대비, ThemeDark/Light/System 구분, 숫자 폭.

## 7. 리스크

| 리스크 | 대응 |
|---|---|
| `.cs`가 문자열 `Content`를 대입하는 줄을 놓치면 아이콘이 비거나 바인딩 오류 | §2.4 같은 Atomic 교체 + `rg` 수용 기준 |
| S74의 Play/Pause 토글 구현 방식이 가정과 다름 | P4-4 LOCK 전에 현재 `x:Name`과 갱신 줄을 구현이 알려 준다 |
| 리소스 사전 Style에서 `ElementName` 바인딩이 풀리지 않음 | Selected는 인라인 `Button.Style`로 한다 |
| Segoe UI Variable 부재로 숫자 폭 검증 불가 | Consolas 유지, Gate에서 재평가 |
| 포커스 링 부재 | 현행 유지, Phase 7에서 `Focus.Ring`과 함께 |
| 렌더 미확인 | 모든 PASS 주장 금지(NOT EVALUATED) |

## 8. 열린 결정

| ID | 결정 | 권고 | 결정자 |
|---|---|---|---|
| O8 | 볼륨 트랙 클릭을 클릭 위치 이동으로 통일 | **채택(통일)**. 단 실제 적용(V5-3)은 **S87 Windows 결과 이후** | 총괄 확정 |
| O9 | 볼륨 수준별 3단계 아이콘 | **채택(2단계)**. V5-4는 열지 않는다 | 총괄 확정 |

## 9. 개정 이력

| Rev | 내용 |
|---|---|
| Rev1 | 초안(2026-10-03 06:00 KST). |
| Rev2 | 수석 조건부 승인 반영(총괄 전달): ① 키 정정(`Lumen.Icon.DisabledBrush`, `Lumen.Size.Hit` 40 / `Lumen.Size.Icon` 24, 신규 키 0 유지) ② Play/Pause 상태 매핑 확정 ③ P4-4~7에 정적 `AutomationProperties.Name` 포함 ④ 외곽선 아이콘 링 코어 0.5 안팎, 시각 2.0 ⑤ P4-7 Fullscreen Exit 동기화 제외(Enter 정적만) ⑥ V5-2 `/` 요소 분리 제외, `--:--`은 `RelativeSource Self` DataTrigger ⑦ O8 채택(V5-3은 S87 Windows 결과 이후), O9 2단계 채택. 추가로 S89 도형 확정(11종, 선 요소 코어 0.5 면 → 시각 2.0)을 §2.2·§2.5에 반영. |
| Rev3 | S97 LOCK(총괄): V5-2 스타일 2개(`ChromeTimeLabel`, `ChromeTimeCurrent`)를 `Controls.xaml`(≤+16)에 두고 `MainWindow.xaml`은 Style 2줄 교체(≤+2/-2)로 한다. 편차 사유는 인라인 Style 2개가 약 +21줄로 ≤+14 상한을 넘기기 때문이다. `.cs` 0, 신규 토큰 0. 수석 기록(2026-10-03 18:1x). |
