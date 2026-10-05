# Handoff — Dynamic Sound DS-0 Research

| Field | Value |
|-------|-------|
| Written | 2026-09-23 23:56 KST |
| From | Active Chief / Continuity (비서 기록) |
| To | **기획·설계 회의실** (기획 · 기술조사 · 수석 · UI·UX · 총괄) |
| Stage | **DS-0 Research** |
| Soft track | Phase A Soft / S39 **unbroken** · Soft≠PASS · Usage GREEN Soft |
| Production integration | **총괄 Gate** 후에만 |

## Ask

1. Define problem/scope: Dynamic Sound ≠ EQ/normalizer/aggressive compressor.
2. Principles: Analyze+Classify+Adaptive DSP+Smooth+Preserve Intent.
3. Stage plan DS-0→DS-5 as in Continuity.
4. License: Essentia/aubio out by default · FFmpeg LGPL audit · libebur128 MIT candidate.
5. Architecture: Shell≠lavfi string · IDynamicSoundService · Facade/Engine boundaries.
6. Do **not** break Soft 본선; Design parallel only until Gate.

## SoT

→ `docs/continuity/DYNAMIC-SOUND-DIRECTIVE-20260923.md`

---

## ADDENDUM update — 2026-09-24 00:10 KST

| Exit | Status |
|------|--------|
| ANALYSIS PATH | **SPIKE REQUIRED** — Primary A(+asplit) Host · B/C primary REJECT |
| SERVICE OWNERSHIP | Contracts **미고정** · Application → Controller → minimal Engine contract |
| DSP CONTROL MODEL | **LOCKED** — continuous≠preset · graph keep · ON≠louder FAIL |
| LICENSE MATRIX | **LOCKED** |
| PRODUCTION CODE | **금지** · Design Gate PASS 전 Spike 성공 |

SoT: `docs/continuity/DYNAMIC-SOUND-DS0-ADDENDUM-20260924.md`
