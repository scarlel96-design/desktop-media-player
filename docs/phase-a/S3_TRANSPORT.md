# Phase A — S3 Transport

## Delivered
- Play / Pause / Stop via Facade
- Open appends to `IPlaylistService` then `PlayAt`
- Prev / Next via playlist — **stop at ends** (no wrap); buttons disabled via `CanPlayPrevious`/`CanPlayNext`
- Composition: `App.Playlist = MinimalPlaylistService(Facade)`

## Out of scope
- S4 Volume compact/Mute
- Playlist L2 panel UI (S5)
