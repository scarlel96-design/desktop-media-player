# Phase A — S20 Soft Persist Playlist

## Delivered (Shell-only)
- Persist playlist paths + CurrentIndex Soft to `%LocalAppData%/desktop-media-player/playlist-prefs.json`
- Restore on start via existing `IPlaylistService.Clear` + `Add` only
- Missing paths Soft skip
- Restore = list only (no auto Open / PlayAt / Play)
- Persist on playlist mutate Soft + window close
- Empty list Soft OK
- No new Facade/Contracts/Engine API

## Out of scope
- S1–S19 re-run · Remove-by-index · wrap · Blur/P1 · Shell→mpv · native · auto-play restore · KI PASS
