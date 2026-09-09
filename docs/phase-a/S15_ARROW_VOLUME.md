# Phase A — S15 Up/Down Volume Soft

## Delivered (Shell-only)
- `Up` / `Down` → existing volume path ±5 (`AdjustVolumeBySteps` → VolumeSlider → Facade `SetVolume`)
- Reuses S14 Soft Opacity OSD (`ShowVolumeOsd`)
- Wheel path shares the same helper
- No new Facade/Contracts/Engine API

## Out of scope
- Blur/Acrylic · P1 · Settings search · S0–S14 re-run · Shell→mpv · native rebuild
