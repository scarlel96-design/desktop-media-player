# Phase A — S22 Soft PageUp/PageDown Seek ±60s

## Delivered (Shell-only)
- `PageUp` → Seek(pos+60) · `PageDown` → Seek(pos-60)
- Duration clamp Soft via existing Facade `GetPosition` / `GetDuration` / `Seek`
- Wired in existing `Window_KeyDown` Soft path
- No new Facade/Contracts/Engine API

## Out of scope
- Home/End · rate · Blur/P1 · S1–S21 re-run · Shell→mpv · native · KI PASS
