# Soft Copy Remaining (S52 Soft)

Shell Soft: Ctrl+Shift+R SoftCopyRemaining Soft → remaining = max(0, GetDuration − GetPosition) Soft → FormatTime(remaining, durationKnown: true) Clipboard Soft + OSD Soft Remaining copied.
Soft no-op Soft if no facade Soft / duration NaN, Infinity, <=0 Soft / position NaN or negative Soft / result "--:--" Soft / exception Soft. Ctrl+P Playlist Soft · Ctrl+Shift+C/N/D/H/E/L/P/T Soft unchanged. Key.R Soft was unused at 1395eb1.
Shell only Soft: existing _facade.GetDuration() / GetPosition() + existing FormatTime only Soft; no Facade/Engine/Contracts change.
Soft != Windows PASS. KI-022/024 WINDOWS PENDING.
