# Phase A — S17 Window Bounds Persist Soft

## Delivered (Shell-only)
- Persist/restore `Left`/`Top`/`Width`/`Height`/`WindowState` to `%LocalAppData%/desktop-media-player/window-bounds.json`
- Clamp to virtual screen (keep chrome visible)
- Save on close / size / location / state change
- No Facade/Contracts/Engine involvement

## Out of scope
- Blur/Acrylic · P1 · Settings search · S0–S16 re-run · Shell→mpv · native rebuild
