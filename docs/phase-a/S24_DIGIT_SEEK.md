# Phase A — S24 Soft Digit Seek (D0–D9 / NumPad)

## Delivered (Shell-only)
- `D0`–`D9` / `NumPad0`–`NumPad9` → `Seek(duration * n / 10)`
- `duration ≤ 0` Soft no-op
- Existing Facade `GetDuration` / `Seek` only · `Window_KeyDown` Soft path
- No new Facade/Contracts/Engine API

## Out of scope
- Rate · Chapter · Blur/P1 · Mute OSD · S1–S23 re-run · Shell→mpv · native · KI PASS
