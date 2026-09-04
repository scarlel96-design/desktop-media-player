# Phase A — S0 Contracts / Facade skeleton

**Status:** Implemented (code). Not a product Gate.

## KI-010
- `INativeRuntimeProbe` + `PlaybackFacade.TryProbe`
- Shell `MainWindow` no longer references `DesktopMediaPlayer.Engine.Mpv`
- Composition root (`App`) may still construct `MpvPlaybackEngine`

## Facade extensions (approved)
`FrameStep`, `SetMute`/`GetMute`, `GetPosition`/`GetDuration`, `ListTracks`/`SelectTrack`,
`LoadExternalSubtitle`, `SetSubtitleOffset`, `Screenshot`

## Services
- `IPlaylistService` / `MinimalPlaylistService` — Open only via Facade

## Tests
Fake engine + Facade probe/mute/frame-step/subtitle path/playlist PlayAt
