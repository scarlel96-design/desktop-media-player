# Phase A — S14 Window MouseWheel Volume Soft

## Delivered (Shell-only)
- Window `PreviewMouseWheel` → volume ±5 via existing `VolumeSlider` → Facade `SetVolume`
- Soft OSD: center Opacity text `Volume N` (reuse OSD fade)
- Volume group wheel unchanged (no double-step)
- No new Facade/Contracts/Engine API

## Out of scope
- Blur/Acrylic · P1 · Settings search · S0–S13 re-run · Shell→mpv · native rebuild
