# Phase A — S9 Timeline Buffer + Hover Time

## Delivered (AC-UI-1 residual)
- Buffer bar under Seek (`ProgressBar`) from Facade `GetBufferedEndSeconds()` (mpv demuxer/cache duration + position)
- Soft: cache property = 0 → empty bar (real data; no position-fake)
- Hover time tooltip on timeline · throttle **≤100ms**
- Seek remains Facade-only

## Out of scope
- Thumbnail / chapter (P1)
- Blur / Acrylic / Settings search
- Soft S8 re-run · playback path / native rebuild
