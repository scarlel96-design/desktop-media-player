# Phase A — S21 Soft Playlist Clear

## Delivered (Shell-only)
- Playlist panel **Clear** button → existing `IPlaylistService.Clear()` only
- Soft disable when list empty
- After Clear: UI Soft empty + `PlaylistPrefsStore` Soft sync (existing persist)
- Transport Soft refresh via `RefreshTransportEnabled`
- No new Facade/Contracts/Engine API

## Out of scope
- Remove-by-index · wrap · Blur/P1 · S1–S20 re-run · Shell→mpv · native · auto-play · KI PASS
