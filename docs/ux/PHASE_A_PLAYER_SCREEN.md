# Phase A Player Screen — Lumen (문서만 · 코딩 착수 아님)

| Field | Value |
|-------|-------|
| Doc ID | UX-2026-09-05-001 |
| Updated | 2026-09-05 03:26 KST |
| Authority | UI·UX 제출 · 총괄 채택 · 기획 AC 편입 · 수석 Shell 제약 확인 |
| Implementation | Spike PASS WITH KI · **S1 Control Bar skeleton in Shell** |

## Assumptions

- Shell=WPF overlay · 영상/자막=엔진 L0 · Lumen · Blur default OFF · auto show/hide
- Phase A=Core Playback MVP controls · Thumbnail/chapter/설정검색=비범위(슬롯만)

## UI debt D1–D5 → Phase A 초기 필수

| ID | Debt |
|----|------|
| D1 | Chrome 밀도 부족 · 정렬 기준선 없음 |
| D2 | Timeline 빈약 · hit 좁음 · buffer/hover time 부재·불안정 Seek |
| D3 | Volume 비율 이상 · mute 묶음 불명확 |
| D4 | 아이콘·컨트롤 크기 불일치 (24 그리드 미준수) |
| D5 | Current/Total time 타이포·대비 약함 |

## Layout (Bottom chrome, 16px grid)

- Heights: control row 48 + timeline row 28 + pad 8+8 ≈ **92px** chrome (DPI scale)
- Safe margin 16 · control gap 8 (group 16)
- Material: solid/opacity · Blur OFF · shadow elevation 1 static

## Controls (summary)

Timeline hit≥24 · track 4/6 · thumb 12/14 · Seek via Facade · hover time only · tooltip throttle ≤100ms  
Time Caption 11–12 tabular · Volume icon24 + slider **88–104** · Transport **40×40** / glyph 24  
Sub/Audio flyout immediate · Playlist L2 width **280–320** · FS toggle + dblclick L0  
Auto-hide 2.5s playing idle · Opening=spinner only

## AC-UI (Phase A 화면 AC · 기획 편입)

| ID | Criterion |
|----|-----------|
| AC-UI-1 | Timeline Seek·buffer·hover time·hit≥24 |
| AC-UI-2 | Current/Total 가독 · unknown=--:-- |
| AC-UI-3 | Volume width 88–104 · mute·wheel · 비율 debt 해소 |
| AC-UI-4 | Transport 40×40 · icon 24 |
| AC-UI-5 | Sub/Audio flyout immediate · no modal |
| AC-UI-6 | Playlist ≡ panel · Empty CTA |
| AC-UI-7 | FS button+dblclick · Lumen chrome in FS |
| AC-UI-8 | Lumen spacing 8/16 · blur OFF · Opacity-only · L1≤2 |
| AC-UI-9 | Content-first · no Invalidate spam/blur (perf measured separately) |

## Defaults / Open

| Topic | Decision |
|-------|----------|
| Prev/Next at queue end | **stop** (disabled / Ended) · wrap=P1+ setting |
| Accent | OPEN (브랜딩) |

## Shell constraints (수석 · Facade/Shell 예산)

- Timeline hit ≥24 · tooltip throttle ≤100ms · Seek=`IPlaybackEngine.Seek` only
- Volume width 88–104 · hit 40×40 / icon 24
- Playlist L2 280–320 · open pauses auto-hide
- L1 ≤2 · Blur OFF · Opacity-only show/hide · no per-frame Invalidate
- Sub/Audio → engine track API (no UI overlay subtitle path)
- Prev/Next default **stop** in Playlist service
