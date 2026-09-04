# Phase A — S4 Volume compact + Mute

## Delivered
- Mute button 40×40 → `IPlaybackEngine.SetMute` / glyph refresh
- Volume slider width **96** clamped **88–104** (UX-001 AC-UI-3)
- Mouse wheel on volume group (±5)
- Unmute on volume>0 while muted; restore last audible volume on unmute from 0
- Facade only · Blur OFF · no P1

## Soft
Engine `GetMute` is best-effort sync read; UI keeps `_muteUi` for immediate glyph.
