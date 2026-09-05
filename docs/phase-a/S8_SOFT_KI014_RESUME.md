# Phase A — S8 Soft (KI-014 Δrate · Resume@FirstFrame)

## Scope (Architecture Soft only)
- **KI-014 Soft:** log `frame-drop` / `decoder-drop` / `vo-drop` **absolute** counts **and** sample-to-sample `*-delta` / `*-rate` (/s) in `render_path` lines.
- **Meaning:** cumulative `frame-drop-count` ≠ perceived stutter. Use Δrate between metrics samples (~2s) for Soft evidence. Playback path (vo/hwdec/seek algo) **unchanged**.
- **Resume@FirstFrame Soft:** arm resume from `IResumeStore` on Open/playlist change; **Seek only once in `OnFirstFrame`**. Removes opening-race early Seek.

## Out of scope
- P1 · Blur/Acrylic · Settings search · native rebuild · S1–S7 re-run · playback path changes

## Evidence keys
`render_path` … `frame-drop=N … frame-drop-delta=D frame-drop-rate=R/s` (and decoder/vo equivalents). First sample after Open has delta `(n/a)`.
