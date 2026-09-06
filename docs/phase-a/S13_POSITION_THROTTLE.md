# Phase A — S13 Facade OnPositionChanged Throttle Soft

## Delivered
- `PlaybackFacade.OnPositionChanged` fan-out throttled to ≤100ms
- Identical (pos, dur) suppressed
- Latest pending value always delivered (timer flush / immediate when due)
- Contracts signature unchanged · Engine/Shell UI untouched

## Out of scope
- Blur/Acrylic · P1 · S0–S12 re-run · Shell→mpv · native rebuild
