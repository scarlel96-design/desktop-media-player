# Phase A — S31 Soft Escape Dismiss Flyouts

## Delivered (Shell-only)
- `Escape` dismisses `TrackFlyout` / `MediaInfoFlyout` Soft when open
- Fullscreen: visible flyout Soft dismiss first; else existing Exit fullscreen Soft
- `MainWindow.xaml.cs` KeyDown Soft path only
- No new Facade/Contracts/Engine API

## Out of scope
- S1–S30 re-run · Blur/P1 · force-push · PASS claim
- Soft ≠ Windows PASS · KI-022/024 WINDOWS PENDING
