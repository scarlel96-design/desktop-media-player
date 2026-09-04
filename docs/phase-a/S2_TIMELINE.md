# Phase A — S2 Timeline / Current·Total Time

## Delivered
- Current/Total captions (`--:--` when unknown) with tabular Consolas 12
- Seek slider Maximum tracks duration; Seek via `IPlaybackEngine.Seek` only
- Position poll ≤100ms (`DispatcherTimer`) while opening/playing/paused
- Drag suppresses slider writeback until commit
- Soft CS0108: `VideoHwndHost.ChildHwnd` (no longer hides `HwndHost.Handle`)

## Out of scope (later)
- Buffer bar / hover time tooltip (AC-UI-1 partial)
- Thumbnail / chapters
