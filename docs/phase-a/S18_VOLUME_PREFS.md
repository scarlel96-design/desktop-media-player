# Phase A — S18 Soft Persist Volume

## Delivered (Shell-only)
- Persist Volume + Mute Soft to `%LocalAppData%/desktop-media-player/volume-prefs.json`
- Restore on start via existing `VolumeSlider` / `SetMute` → Facade path
- Persist on volume change, mute toggle, and window close
- No new Facade/Contracts/Engine API

## Out of scope
- Blur/Acrylic · P1 · Settings search · S0–S17 re-run · Shell→mpv · native rebuild
