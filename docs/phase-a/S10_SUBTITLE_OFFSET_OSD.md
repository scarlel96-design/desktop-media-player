# Phase A — S10 Subtitle Offset (±) + OSD

## Delivered
- Hotkeys `[` / `]` → ±0.1s cumulative offset via Facade `SetSubtitleOffset`
- OSD center text · Opacity-only hold ~1.2s then ~300ms fade (total ≤~1.5s)
- Engine keeps subtitle layer (`sub-delay` only inside Engine Host)
- Shell never sets `sub-delay` / never calls mpv

## Out of scope
- Blur/Acrylic · P1 · Settings search · S1–S9 re-run · UI overlay subtitle path
