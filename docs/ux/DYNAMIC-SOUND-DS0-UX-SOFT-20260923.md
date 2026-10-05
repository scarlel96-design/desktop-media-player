# Dynamic Sound DS-0 — UX Soft (문서만 · Soft≠Visual PASS)

| Field | Value |
|-------|-------|
| Written | 2026-09-23 23:58 KST · @UI·UX 봇 |
| Scope | DS-0 Research / DS-5 UI foreshadow · **코딩·생산 통합 금지** |
| Soft Phase A / S39 | **비침범** |
| Soft≠PASS | Soft ≠ AC-UI Visual PASS · Soft ≠ Visual Quality Gate PASS |
| Align | Lumen Direction · Visual Quality Gate · AC-UI quiet chrome · Blur OFF |

## Product framing (UI copy spirit)

- Dynamic Sound = Analyze→Classify→Adaptive DSP→Smooth→**Preserve Intent**.
- **Not** EQ · simple normalizer · aggressive compressor — do not brand or layout as those.

## Soft MVP chrome (DS-5 foreshadow · DS-0 design lock)

| Element | Spec |
|---------|------|
| Control | **Off \| On** only in Soft MVP chrome foreshadow |
| Default | **Off** (passthrough / identity — no DSP) |
| Intensity | **검토만** · Soft MVP chrome에 상시 노출 금지 (Advanced/DS-5+) |
| Placement | Settings → **Audio** (권고) · Player L1에 상시 토글 넣지 않음 (quiet chrome) |
| Optional L1 | 없음이 기본. 필요 시 Media Info/오디오 플라이아웃 하위 1줄만 — **OSD 상시 금지** |

## States (Micro Detail · Visual Quality Gate)

| State | UI |
|-------|-----|
| Off | 토글 Off · 부가 chrome 없음 · 영상 방해 0 |
| Turning On | 짧은 상태 반영(토글/체크)만 · **팝업·배너 금지** |
| On (stable) | 토글 On · 선택적 Caption 1줄(“Dynamic Sound”) · meter/그래프 Soft MVP 비범위 |
| Fail-safe → Off | Facade 이벤트만 · 비침투 칩 **1회·자동 소멸**(≤~2s) 또는 Settings 내 문구 · OSD 남발 금지 |
| Opening/Loading media | Dynamic Sound UI 추가 스피너 금지 (기존 Opening만) |

## OSD / Feedback rules (Refined Feedback)

- **금지:** 재생 중 반복 OSD · “analyzing…” 상시 · 라우드니스 숫자 플로팅 · 매 분류 변경 토스트.
- **허용:** 토글 직후 Settings/플라이아웃 내 즉시 상태 · Fail-safe 시 비침투 칩 1회.
- Motion: Lumen Premium Motion — Off↔On은 Opacity/체크만 · abrupt panel 금지 · 입력 지연·재생 성능 저하 금지.

## Consistency

- Same Audio settings pattern as existing Lumen rows (spacing 8/16 · hit ≥40 height · caption 11–12).
- No ad-hoc Dynamic Sound visual language.
- Shell binds **Facade events only** — no af/lavfi strings · no raw meter driving UI DSP.

## Explicit non-goals (DS-0/ Soft)

- EQ curve UI · multiband · “AI enhance” marketing chrome
- Intensity slider in Soft MVP chrome
- Blur/Acrylic/P1 · Waveform decoration
- Claiming AC-UI or Visual Quality Gate PASS from this Soft doc

## Handoff

- @기획 봇: Off=무처리 · Intensity=비노출 Soft MVP와 AC-DS 정합
- @수석 엔지니어 봇: UI ↔ Facade Enable만 · Shell≠af
- @비서 봇: Continuity에 본 Soft UX 기록
- @총괄 봇: production UI 통합=총괄 Gate · Soft 본선 비침범
