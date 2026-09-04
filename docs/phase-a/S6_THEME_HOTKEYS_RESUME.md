# Phase A — S6 Theme / Hotkeys / Resume / Auto-hide

## Delivered
- Theme cycle Dark → Light → System (`AppsUseLightTheme`) · solid brushes only (Blur OFF)
- Chrome auto-hide 2.5s while Playing · Opacity-only · paused when Sub/Audio flyout or Playlist open
- Hotkeys: Space, ←/→ seek 5s, ↑/↓ volume, M mute, F/F11/Esc FS, Ctrl+O/S/P/T
- Resume: `%LocalAppData%/desktop-media-player/resume.json` via `IResumeStore` / `FileResumeStore`
- Mute UI synced from `GetMute` on position poll / state change

## Out of scope
- Settings search UI
- Blur/Acrylic
- P1 wrap / polish animation
