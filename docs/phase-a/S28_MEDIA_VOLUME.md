# Phase A — S28 Soft VolumeUp / VolumeDown

## Delivered (Shell-only)
- `VolumeUp` → `AdjustVolumeBySteps(+5)` Soft (existing SetVolume / ShowVolumeOsd path)
- `VolumeDown` → `AdjustVolumeBySteps(-5)` Soft
- `Window_KeyDown` Soft path · no new Facade/Contracts/Engine API

## Out of scope
- VolumeMute media key · Blur/P1 · S1–S27 re-run · Shell→mpv · native · KI PASS
- USER WINDOWS TEST REQUIRED (media keys) tracked separately from Soft Closure
