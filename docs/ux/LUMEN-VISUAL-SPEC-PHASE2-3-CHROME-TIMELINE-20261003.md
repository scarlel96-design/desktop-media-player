# Lumen Visual Spec — Phase 2 Player Chrome + Phase 3 Timeline (문서 전용 · 코드 변경 없음)

| 항목 | 값 |
|---|---|
| Doc ID | UX-LUMEN-SPEC-P2P3-001 |
| 작성 | UI·UX 봇, 2026-10-03 KST (총괄 요청: 수석 검토 대기 중 Phase 2 이어서 작성) |
| 개정 | **Rev4** 2026-10-03 05:4x KST — 총괄 정정(05:5x KST): T3-2는 신규 색 키 0, 전용 `Track.Fill/Thumb`·`Focus.Ring`은 Phase 7 이월. 총괄 수정 A(숨김 띠 대상 Grid·전체화면 Collapsed), B(hover 확대 철회, 레일 4 고정), C(T3-3 상한·VerticalOffset), T3-2 최종 스펙(§3.1~3.2 교체), O6=약 896·O7=썸 숨김 확정 |
| 선행 | `LUMEN-VISUAL-SPEC-PHASE1-FOUNDATION-20261003.md` Rev3 (토큰 키·F-1=S78 LOCK/F-2/F-3). 색 키는 `Color`+`Brush` 쌍, 파일은 `Lumen.Tokens.Shared/Colors.Dark/Colors.Light`. 이 문서는 그 키(`Lumen.*`)만 사용하고, 추가 키는 §5에 따로 적는다 |
| 근거 | `97c0360` 코드 정적 읽기(E2), 계산(E3), 사용자 캡처 픽셀 측정(E5: `USER-CAPTURE-REVIEW-20261003-0345.md`) |
| 상태 | **UI PASS / Visual PASS = NOT EVALUATED. Soft ≠ PASS. Windows NOT TESTED.** 설계 스펙이며 구현·검증 결과가 아니다 |
| 제약(총괄·수석) | 영상 위 오버레이 금지 · Effect(DropShadow·Blur)/OpacityMask/AllowsTransparency 금지 · 모션은 Opacity·RenderTransform만 · 레이아웃 속성 애니메이션 금지 · 기존 `x:Name`·핸들러 이름 불변 · Facade/Architecture 변경 없음 |

## 0. 요약

1. **숨김 띠(#121212, 측정 113px, 창 모드만)**는 chrome 행을 유지한 채 **영상 열 바깥 Grid(`Grid.Column=0`)** 배경을 검정(`Lumen.Surface.Video`)으로 바꿔 해소한다. 레이아웃·HWND 크기 변화가 없다. 대가: 숨김 중에도 하단 약 100 DIP가 검은 여백으로 남는다. 영상 영역을 키우는 방식은 HWND 재배치를 동반하므로 Phase 8 평가로 미룬다.
2. 컨트롤 폭 모델: Play/Pause 통합(U2) 후 15개 버튼 + 볼륨 96 + 구분선 4개 = 약 **892 DIP**. 현재 `MinWidth=640`에서는 맞지 않는다. **결정 O6 필요**(§2.4).
3. **Slider 교체의 필수 계약(안 지키면 시크가 깨진다)**: S70/S71 코드가 `SeekSlider.Template.FindName("PART_Track")`과 그 `Thumb`을 사용한다. 새 템플릿은 `PART_Track`(Track)과 Thumb을 반드시 포함하고 `IsMoveToPointEnabled` 동작과 핸들러(`PreviewMouseLeftButtonDown`/`Up`, `LostMouseCapture`)를 건드리지 않는다.
4. `HoverTimePopup`은 `AllowsTransparency="True"`가 금지 규칙과 충돌한다. 불투명 Popup(`AllowsTransparency=False`, 반경 0)으로 교체한다. Popup을 쓰는 이유: 툴팁이 영상 HWND 위에 떠야 하는데 일반 요소는 HWND 아래에 가려진다(airspace).
5. Timeline 변경은 시크 경로라서 정책상 **고위험(seek)** 분류다. Windows 확인 1건이 필요할 가능성이 크다(§4.3). 루틴 요청이 아니라 총괄이 판단할 항목이다.

## 1. Phase 2 — Player Chrome

### 1.1 면과 구분 (Hierarchy · Visual continuity)

| 항목 | 현재(E2) | Lumen 값 |
|---|---|---|
| chrome 배경 | `Legacy.Chrome.Bg` (#E6121212, 90% 불투명) | `Lumen.Surface.Chrome`(불투명). chrome은 영상 위가 아니라 별도 행이므로 알파가 필요 없다 |
| 영상과의 경계 | 없음 | 상단 1px 헤어라인 `Lumen.Border.Subtle`. chrome `BorderThickness="0,1,0,0"`, 상단 패딩을 7로 줄여 전체 높이 100 DIP 유지 |
| 숨김 상태 바탕 | 창 배경(#121212, Light #F2F2F2)이 드러남 → 띠 | 영상 열 `Grid`의 `Background`를 `Lumen.Surface.Video`(#000000, 양 테마 동일)로 설정. chrome이 투명(Opacity 0)이어도 검은 바탕이 보임 |
| 적용 대상 요소 | — | **영상 열 바깥 Grid(`Grid.Column="0"`)의 `Background`**이다. 행 0의 영상 `Border`(이미 `#000000`)와는 **다른 요소**다(행 0 Border만 검정이면 행 1의 chrome 자리가 안 칠해진다). 전체화면은 S77에서 chrome 행이 Collapsed로 처리되어 띠가 없으므로, 이 변경의 대상은 **창 모드만**이다 |
| 레이아웃 높이 | 28 + 8 + 48 + 패딩 16 = 100 DIP(E3) | 100 DIP 고정(`Lumen.Size.ChromeHeight`). 측정된 띠는 113px(125% → 약 90 DIP)로 계산값과 약 10 DIP 차이가 나며 원인은 확인하지 못했다(캡처 가장자리 영향 가능, 추정) |

숨김 띠 해소 방식 비교(결정 근거):

| 안 | 내용 | 장점 | 위험 | 채택 |
|---|---|---|---|---|
| A | 행 유지 + 영상 열 바탕 검정 | 레이아웃 불변, HWND 불변, Opacity만 | 숨김 중에도 하단 여백 100 DIP | **Phase 2 채택** |
| B | 숨김 시 chrome 행 Collapsed → 영상 영역 확장 | 진짜 content-first | HWND가 show/hide마다 재배치되어 영상 점프·시크 체감 영향, 고위험(video output) | Phase 8에서 평가 |

### 1.2 컨트롤 그룹과 간격 (Alignment · Density)

기능 노출은 바꾸지 않는다(O1). 그룹은 구분선과 간격으로만 나눈다. 단위: 버튼 40×40, 그룹 내 간격 8, 구분선 폭 1·높이 20·`Lumen.Border.Subtle`·양쪽 마진 8(그룹 간격 17).

| 그룹 | 구성(현재 순서 유지) | 위치 |
|---|---|---|
| G1 파일 | Open | 좌 |
| G2 전송 | Prev · Play/Pause(U2 후 1개) · Stop · Next | 좌 |
| G3 프레임 | FrameBack · FrameStep | 좌 |
| G4 볼륨 | Mute + Volume 슬라이더(96) | 좌 |
| G5 트랙 | Sub · Audio | 우 |
| G6 도구/창 | Playlist · Screenshot · Info · Theme · Fullscreen | 우 |

정렬 규칙: 모든 컨트롤의 수직 중심은 컨트롤 행(48) 중심 하나에 맞춘다. 좌 클러스터와 우 클러스터 사이는 `*` 공백. 시간 행(28)의 시간 라벨과 슬라이더 중심선이 일치하도록 라벨은 `VerticalAlignment=Center`, 슬라이더 높이 24는 유지.

### 1.3 시간 라벨 (Typography · D5)

- 서체: 현재 Consolas 12 유지(O3: 탭형 숫자 여부는 렌더로 확정하기 전까지 바꾸지 않는다). 크기·색은 `Lumen.Type.Caption`, `Lumen.Text.Secondary`.
- 폭 고정: 시간 문자열 길이가 `--:--` → `00:25` → `1:00:00`으로 바뀌면 슬라이더 폭이 흔들린다. 현재/전체 라벨에 `MinWidth`를 두고 현재=오른쪽 정렬·전체=왼쪽 정렬로 안쪽 가장자리를 고정한다. `MinWidth` 값은 `Consolas 12`에서 `0:00:00`이 들어가는 크기로 하고(약 56–64, 렌더로 확정), 추정값이므로 구현 Atomic에서 측정한다.
- 현재/전체 구분 문자는 추가하지 않는다(기존 `--:--` 규칙 유지, AC-UI-2).

### 1.4 폭 한계 (O6, 총괄 결정 필요)

| 모델 | 폭(DIP) |
|---|---|
| 현재(게이트표 E3) | 912 |
| Play/Pause 통합 + 그룹/구분선 적용 | 892 (계산: 버튼 15×40 + 볼륨 96 + 그룹 내 간격 + 구분선 4×17 + 좌우 패딩 32 + 클러스터 최소 간격 16) |

기본 창 960 DIP에는 들어간다(창 비클라이언트 영역을 빼도 대략 944). 그러나 `MinWidth=640`까지 줄이면 잘리거나 겹친다.

| 옵션 | 내용 | 장단 |
|---|---|---|
| (a) `MinWidth`를 약 896으로 상향 | 한 줄 1개로 설정 | 가장 단순, 기능 노출 불변. 작은 창은 포기 |
| (b) 좁은 폭에서 컨트롤 행을 2행으로 감기 | 폭 임계 이하에서 우 클러스터를 아래 행으로 | 작은 창 유지, chrome 높이 변화·`108` 하드코딩 플라이아웃 위치에 영향 |
| (c) 임계 이하에서 일부 컨트롤을 더보기로 | O1과 충돌(노출 변경) | 비권고 |

**권고: (a)**. 창 최소 크기 변경은 사용자에게 보이는 동작이지만 재생·시크와 무관한 UI 범위다. 총괄이 O6로 결정하고, 필요하면 사용자에게 한 문장으로 확인한다.

### 1.5 토글/선택 상태 규칙(적용은 Phase 4 템플릿에서, 여기서는 대상 목록만 확정)

선택(Selected) 표시 대상: Mute(무음일 때), Playlist(패널 열림), Sub/Audio(플라이아웃 열림), Info(플라이아웃 열림), Fullscreen(전체화면일 때). 비활성: Prev/Next가 큐 끝일 때. 사용자 캡처에서 비활성 Next가 #F4F4F4 면으로 가장 밝아 보이는 문제가 확인되었다. 이 항목은 Phase 4에서 해소한다.

## 2. Phase 2 Atomic (컴포넌트 단위, Shell 한정)

공통: 빌드 0/0, 테스트 18/18, Facade 등 금지 영역 수정 없음, 롤백=revert, 렌더 PASS 주장 금지, 한 Atomic은 컴포넌트 하나.

| ID | 내용 | 상한 | 시각 변화 | 완료 기준(내부) |
|---|---|---|---|---|
| P2-1 | chrome 면: `BottomChrome.Background`를 `Lumen.Surface.Chrome`으로, 상단 헤어라인 추가, 영상 열 `Grid` 바탕을 `Lumen.Surface.Video`로. 토큰 추가(§5) | `MainWindow.xaml` ≤+12줄, `Lumen.Colors.*` 각 ≤+3줄 | **있음**(chrome 색 #E6121212 → #1A1A1C 불투명, 숨김 띠 → 검정) | 빌드 통과. `Legacy.Chrome.Bg` 참조 제거(키는 유지). 키 집합 Dark=Light 유지 |
| P2-2 | 그룹 구분선·간격: 구분선 4개 추가, 버튼 `Margin` 규칙 정리(그룹 내 8) | `MainWindow.xaml` ≤+30줄 | 있음 | 기존 `x:Name`·`Click` 불변(diff로 확인). 폭 계산표(§1.4) 대조 |
| P2-3 | 시간 라벨 `MinWidth`와 정렬, 라벨 색·크기를 토큰으로 | `MainWindow.xaml` ≤+8줄, `Controls.xaml` ≤+6줄(`ChromeCaption` 수정) | 있음(소) | 시간 문자열 `--:--`/`00:25`/`1:00:00` 변화에도 슬라이더 폭 불변(정적 계산) |
| P2-4 | (O6 결정 후) `MinWidth` 조정 | `MainWindow.xaml` ≤+1줄 | 있음(창 최소 크기) | 계산표와 일치 |

P2-1~P2-3은 "같은 컨트롤/흐름의 관련 변경 2~5개"이므로 Validation Batch 1회로 묶는다. 사용자 요청은 하지 않고 내부 증거(빌드, 정적 대조)로 닫는다. 사용자 확인은 Phase 11 Gate 전에 한 번으로 모은다(총괄 판단).

## 3. Phase 3 — Timeline

### 3.1 구조와 상태 (T3-2 최종 스펙, 수정 B 반영)

현재 구조(E2): `Grid`(MinHeight 24) 안에 `BufferBar`(ProgressBar, 높이 4, `Foreground #4466AA`, `Background #22FFFFFF`) → `SeekSlider`(기본 WPF Slider, 높이 24, 배경 투명) → `HoverTimePopup`. 캡처에서 기본 Slider 트랙이 밝은 회색(#E7EAEA, 테두리 #D6D6D6)으로 보였다.

새 구조(`x:Name` 불변). `BufferBar`는 `SeekSlider`의 **형제 요소**(`IsHitTestVisible=False`)라 슬라이더 템플릿 트리거로 `BufferBar`를 움직일 수 없다. 그래서 **레일 높이 4 고정, 버퍼 막대 확대 없음, hover 시 썸만 표시**한다. `ScaleY` 확대(4→6)와 120ms 페이드는 Phase 7 모션으로 이월한다.

| 층 | 요소 | 역할 | 토큰 |
|---|---|---|---|
| 1 | `BufferBar` | 레일 + 버퍼: `Background`=레일, `Foreground`=버퍼. 높이 4 | `Lumen.Track.Rail`, `Lumen.Track.Buffer`(이미 Dark/Light에 있음, 총괄 확인) |
| 2 | `SeekSlider`(새 `ControlTemplate` `LumenSeekSlider`) | 재생 진행(채움) + 썸. 레일은 그리지 않음 | 채움 `Lumen.AccentBrush`, 썸 `Lumen.Text.PrimaryBrush`(S87 신규 색 키 0, 전용 토큰은 Phase 7) |
| 3 | `HoverTimePopup` | 시간 툴팁(§3.3) | `Surface.Raised`, `Border.Subtle`, `Type.Caption` |

**T3-2 템플릿 구조(확정)**

```
Grid (Height 24, Background=Transparent)
  Track  x:Name="PART_Track"
    Track.DecreaseRepeatButton : RepeatButton(Style 투명 히트)
    Track.Thumb                : Thumb x:Name="SeekThumb"  (1개)
    Track.IncreaseRepeatButton : RepeatButton(Style 투명 히트)
```

- `RepeatButton` 두 개의 `ControlTemplate`은 `Grid Background="Transparent"`(24 전체 높이 히트)이다. **Decrease만** 안에 채움 `Border`를 둔다: 높이 4, 세로 중앙, `Background=Lumen.AccentBrush`, `Margin="0,0,-7,0"`(채움 끝을 썸 가장자리가 아니라 썸 **중심**에 맞춘다. 썸 폭 14의 절반 7). Increase는 채움 없음. 총괄 문구의 "투명 RepeatButton"을 "배경이 `Transparent`인 히트 영역"으로 해석했다. 이와 다르면 알려 주세요.
- `Thumb`(`SeekThumb`): 폭 14 고정(평상시·hover 동일), 높이 24(히트). 템플릿 = `Grid Background="Transparent"`(히트 테스트 가능 요소, 필수) + 가운데 `Ellipse` 14×14 `Fill=Lumen.Text.PrimaryBrush`, `Stroke=Lumen.Surface.Chrome`, `StrokeThickness=1`(채움 색과의 구분 용도. 아래 대비값은 `Track.*` 후보 색으로 계산한 것이며 Dark 썸/채움 2.19, Light 2.90으로 3:1 미만이라 외곽선으로 분리한다. `AccentBrush`·`Text.PrimaryBrush` 실제 값으로 구현 시 재계산한다).
- 평상시 썸 숨김은 **`Opacity=0`만** 사용한다. `Visibility`는 쓰지 않는다(히트 테스트와 S71의 `thumb` 핸드오프가 유지되어야 하기 때문).
- 상태(템플릿 `Trigger`, `Setter`만 사용, 스토리보드 없음):

| 상태 | 조건 | 썸 Opacity | 비고 |
|---|---|---|---|
| Rest | — | 0 | 레일 4, 채움 `AccentBrush` |
| Hover | `SeekSlider.IsMouseOver` | 1 | 즉시 전환(페이드는 Phase 7) |
| Drag | `SeekThumb.IsDragging`(`SourceName`) | 1 | 포인터가 슬라이더 밖으로 나가도 유지 |
| Focus(키보드) | `SeekSlider.IsKeyboardFocused` | 1 | 표시 조건만 반영. 포커스 링(`Focus.Ring`) 스타일은 Phase 7로 이월 |
| Disabled | `IsEnabled=False` | 0 | 채움 폭 0은 값이 0이므로 자연 |

- 알려진 어긋남(허용, 기록): WPF `Track`은 썸 폭만큼을 뺀 길이에서 위치를 계산하므로, 진행 채움(썸 중심 기준)과 선형으로 그려지는 `BufferBar`가 양 끝에서 **최대 7px** 어긋날 수 있다. 렌더 확인 전 값이며 허용 범위로 기록한다(수석 권고).
- Light 레일 대비(`Track.Rail` #B8B8C0 vs #F4F4F6)는 1.79:1로 낮다(장식 요소, 채움은 5.45:1). Light 완결은 Phase 9이므로 그때 레일 값을 재조정한다(기록).

### 3.2 계약(구현 필수, LOCK 조건)

- `SeekSlider`는 계속 `Slider`다. 템플릿에 이름 **`PART_Track`**인 `Track` 1개, `Thumb` 1개, 투명 히트 `RepeatButton` 2개. 썸 템플릿 안에 `Transparent` 이상 `Background` 요소 필수.
- 썸 크기는 평상시에도 14 고정. 숨김은 `Opacity=0`만.
- `IsMoveToPointEnabled="True"`, 기존 `AddHandler`로 등록된 `PreviewMouseLeftButtonDown`·`PreviewMouseLeftButtonUp`과 `LostMouseCapture` 핸들러, `_seekDragging` 흐름은 **변경하지 않는다**. `.cs` 변경 0줄.
- 히트 높이 24(`Size.TimelineHit`) 유지. 시각 높이는 4.
- `Effect`, `OpacityMask`, `AllowsTransparency`, 레이아웃 속성 애니메이션 사용 금지.
- 시크 지연(관찰된 63~608 ms)은 UI 범위 밖이며 이 Atomic에서 다루지 않는다.

### 3.3 Hover 시간 툴팁

| 항목 | 현재(E2) | 규칙 |
|---|---|---|
| 창 속성 | `Popup AllowsTransparency="True"` | **`AllowsTransparency=False`**, `PopupAnimation=None`. `AllowsTransparency`를 끄면 모서리 반경이 사라지므로 **반경 0 전제**(`Radius.Tooltip` 미사용) |
| 면 | `#E0181818` 반투명 + 반경 4 | `Lumen.Surface.Raised` 불투명 + 1px `Lumen.Border.Subtle` |
| 글자 | `#EEE` 12 | `Lumen.Text.Primary`, `Type.Caption` |
| 위치 | `VerticalOffset=-28`, 좌우 클램프 | **현행 `-28` 유지**(`.cs` 0줄 상한). 8 DIP 간격으로 맞추는 변경은 하지 않는다 |
| 갱신 | `HoverThrottle`=100 ms(최소 간격)이 코드에 있음 | **기존 로직 유지**(변경 없음) |

### 3.4 Atomic (Phase 3)

| ID | 내용 | 상한 | 시각 변화 | 위험 |
|---|---|---|---|---|
| T3-1 | `BufferBar` 레일(`Background`=`Lumen.Track.Rail`)·버퍼(`Foreground`=`Lumen.Track.Buffer`) 토큰화. 높이 4 고정, 확대 없음 | `MainWindow.xaml` ≤+6줄 | 있음(소) | 낮음 |
| T3-2 | `SeekSlider` 새 템플릿(`Controls.xaml`의 `LumenSeekSlider`), 구조·상태 §3.1(페이드·확대 없음) | `Controls.xaml` ≤+70줄(스토리보드 없음 기준), `MainWindow.xaml` ≤+2줄, `.cs` 0줄 | 있음 | **고위험(seek)**: §3.2 계약 위반 시 클릭·드래그 시크 회귀 |
| T3-3 | `HoverTimePopup`을 불투명 Popup으로 교체(`VerticalOffset -28` 유지) | `MainWindow.xaml` ≤+12줄, `.cs` 0줄 | 있음 | 낮음~중간(Popup 렌더) |

확정 순서(총괄): F-2 → F-3 → P2-1 → P2-2 → P2-3 → P2-4 → T3-1 → T3-3 → T3-2. P2-3과 T3-2는 F-3의 `Controls.xaml` 이후. T3-2를 마지막에 두는 이유는 가장 위험한 것을 독립 revert 가능하게 하기 위함이다.

### 3.5 검증·Windows 확인 (총괄 판단용)

- 내부(박스 가능): 빌드 0/0, 18/18, 정적 대조(`PART_Track` 존재·이름, 핸들러 diff 0, `x:Name` diff 0), 금지 요소 검색(`AllowsTransparency`, `Effect`, `OpacityMask` 0건).
- 박스에서 못 하는 것: WPF 렌더·클릭·드래그. 따라서 T3-2는 정책상 **고위험(seek bug)** → Windows 확인 ASAP 후보. 필요 시 요청은 최소화해 영상 한 건(재생 중 바 클릭 2회, 썸 드래그 1회, hover 시간 툴팁)으로 하고, 평가는 이 문서의 상태 표와 대조한다.
- 확인 불가 항목은 PASS로 쓰지 않는다: Visual PASS = NOT EVALUATED.

## 4. 격차 매핑 (이 문서 범위)

| 격차 | 해소 Atomic |
|---|---|
| 숨김 시 #121212 띠(평가 9) | P2-1 |
| 밀도·그룹 위계·정렬(1, 2, 3, D1) | P2-2, P2-4(O6) |
| 시간 가독·폭 흔들림(4, D5, AC-UI-2) | P2-3 |
| 밝은 기본 시크 트랙(11) | T3-1, T3-2 |
| 시간 툴팁 AllowsTransparency 충돌(규칙) | T3-3 |
| hover/press/focus/disabled/selected(7) | Phase 4(버튼 템플릿) |

## 5. 토큰 추가 제안 (F-1 키 집합에 대한 부록, Dark와 Light 동일 키 유지)

| 키 | 위치 | 값 |
|---|---|---|
| `Lumen.Surface.Video` | Dark/Light | #000000 / #000000 |
| `Lumen.Size.ChromeHeight` | Shared | 100 |
| `Lumen.Size.Divider` / `Lumen.Size.DividerHeight` | Shared | 1 / 20 |

**T3-2(S87)는 신규 색 키 0**(총괄 정정). 채움 `Lumen.AccentBrush`, 썸 `Lumen.Text.PrimaryBrush`, 외곽선 `Lumen.Surface.Chrome`을 쓴다. `Track.Rail`·`Track.Buffer`·`Surface.Chrome`은 Dark/Light에 이미 있다(총괄 확인). 전용 `Track.Fill`·`Track.Thumb`·`Focus.Ring` 토큰은 Phase 7로 이월한다. F-1 이후 P2-1에서 추가한다(총 +3~5줄). F-1 줄 수 상한(≤150)에는 영향 없음(초안 98줄).

## 6. 결정 필요

| ID | 질문 | 권고 | 결정자 |
|---|---|---|---|
| O6 | 최소 창 폭 약 896으로 상향 | **확정(총괄)**: P2-4, 수석 검토 후 LOCK | 총괄 |
| O7 | 평상시 시크 썸 | **확정(총괄)**: 숨김(Opacity 0). 썸 크기는 평상시에도 14 고정 | 총괄 |

## 7. 한계

- 모든 판정은 코드 정적 읽기, 계산, 사용자 캡처 픽셀 측정에 한정된다. 렌더 확인 전이다.
- 폭 892 DIP, 시간 라벨 `MinWidth` 56–64는 계산·추정이다. 구현 Atomic에서 실측으로 보정한다.
- 이 문서는 구현 지시가 아니다. 착수는 수석 구조 검토 후 총괄 LOCK.
- UI PASS / Visual PASS = NOT EVALUATED. Soft ≠ PASS. Windows NOT TESTED. Blur/Acrylic/P1 HOLD.
