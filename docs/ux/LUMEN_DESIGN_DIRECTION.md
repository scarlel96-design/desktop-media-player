# Lumen Design Direction — Standing SoT

| Field | Value |
|-------|-------|
| Doc ID | UX-LUMEN-DIRECTION-001 |
| Written | 2026-09-23 23:32 KST · Updated 2026-09-23 23:39 KST · @UI·UX 봇 |
| Authority | User directive (기획·설계 회의실) · lasting UI/UX criterion |
| Scope | Phase A **and all later** UI/UX · not Phase-A-only |
| Related | `PHASE_A_PLAYER_SCREEN.md` (AC-UI / D1–D5) · Design Gate Lumen |
| Visual Quality | **Lumen Visual Quality Gate** (below) · separate from but co-validated with AC-UI / D1–D5 |

## Reference platforms (inspiration only · never clone)

Absorb strengths, then **reinterpret into one independent Lumen language** for Desktop Media Player:

| Source | Absorb |
|--------|--------|
| iOS / macOS | Ordered hierarchy · spatial calm · (see Apple-level polish — quality, not skin) |
| Samsung One UI | Clear readability · reachable controls |
| Windows 10/11 | Desktop affordances · mouse/keyboard first |
| Latest GNOME / KDE | Concise desktop chrome |

**Forbid:** copying any platform’s tokens, chrome, or gesture patterns wholesale.

## Standing principles

1. **Video is the protagonist; UI is support** — content-first.
2. **Minimize unnecessary chrome** — quiet L1; show when needed.
3. **Consistent alignment / spacing / density** — 8/16 rhythm; no ad-hoc gaps.
4. **Unified icons / typography / state feedback** — one glyph grid, one type scale, predictable Idle→Opening→Playing→Paused→Buffering→Ended→Error.
5. **Motion short and functional** — ~120–180ms ease-out; no decorative bounce/scale; respect reduced-motion.
6. **No decoration that costs playback performance** — no per-frame Invalidate; no heavy materials on the video path.
7. **Blur default OFF until measured** — Opacity-only show/hide; Acrylic/P1 HOLD until performance evidence + Gate.
8. **Windows desktop: mouse + keyboard first** — hit targets, focus rings, hotkeys; touch is secondary.

## Apple-level polish principles (Lumen Visual Quality Gate)

Lumen does **not** clone Apple UI surface style. It **reinterprets** Apple’s quality bar for a Windows Desktop Media Player.

**Premium ≠ Heavy. Premium = Refined + Consistent + Responsive.**

### 1. Premium Motion

- Motion links cause → effect (not mere fade).
- Minimize abrupt appear/disappear.
- Consistent easing, duration, accel/decel across the product.
- Same motion language for hover / press / open / close / fullscreen / flyout / panel.
- No excessive animation.
- Feel fast and refined.
- **Never** add input latency or hurt playback performance.

### 2. Micro Detail

Design every state finely: pressed · hover · focus · disabled · selected · opening · loading · error · empty · panel transition.

Manage across the product: spacing deltas · icon optical alignment · text baseline · padding · corner radius · opacity · shadow · hit area.

### 3. System-wide Consistency

- Same function → same visual / interaction rule.
- Same hierarchy → same spacing / typography.
- Same panel/flyout/button class → same motion and feedback.
- **No per-screen ad-hoc styles.** One Lumen Design System controls the whole Player.

### 4. Refined Feedback

Every input gets immediate visual feedback.

Forbid: popup spam · excessive OSD · interrupting watch flow.

Feedback is short, clear, and fades naturally.

### 5. Visual Continuity

Playlist / Track / MediaInfo / Settings / OSD / Chrome must feel one product — not separate apps.

Unify: typography · spacing · radius · icon weight · opacity · elevation · motion · interaction states.

### 6. Pixel-level Polish

**Feature working ≠ UI PASS.**

WV also checks:

- 1–2px misalignment
- icon optical centering
- text vertical alignment
- hit-target consistency
- uneven spacing
- inconsistent opacity
- animation timing mismatch
- panel transition discontinuity
- visual weight imbalance

### 7. Performance Rule

Aim for Apple-level polish **without** sacrificing playback.

- Blur default **OFF** (existing policy).
- Any GPU/CPU-costly effect: **measure then adopt**.

## Validation

- **Windows Visual Validation (WV)** checks:
  1. AC-UI-1~9
  2. D1–D5
  3. Standing Direction (above)
  4. **Lumen Visual Quality Gate** (Apple-level polish 1–7)
- Soft Closure ≠ Visual PASS. Soft≠PASS · Windows NOT TESTED until WV evidence.
- Functional Soft success alone does **not** clear Visual Quality Gate.
- Violations → UX debt / KI, not silent PASS.

## Precedence

User goal → PRD / Design Gate → **this Direction + Visual Quality Gate** + UX-001 / AC-UI → Shell constraints → Soft implementation.
