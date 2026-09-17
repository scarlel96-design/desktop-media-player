# Phase A — S23 Soft Home/End Seek

## Delivered (Shell-only)
- `Home` → `Seek(0)` · `End` → `Seek(duration)`
- `duration ≤ 0` Soft no-op for End
- Existing Facade `GetDuration` / `Seek` only · `Window_KeyDown` Soft path
- No new Facade/Contracts/Engine API

## Out of scope
- Rate · Blur/P1 · Chapter · S1–S22 re-run · Shell→mpv · native · KI PASS
