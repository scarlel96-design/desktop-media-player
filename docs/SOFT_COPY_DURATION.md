# Soft Copy Duration (S51 Soft)

Shell Soft: Ctrl+Shift+L SoftCopyDuration Soft → FormatTime(GetDuration Soft) Clipboard Soft + OSD Soft Length copied.
Soft no-op Soft if no facade Soft / duration NaN, <=0, unknown ("--:--") Soft / exception Soft. Ctrl+P Playlist Soft · Ctrl+Shift+C/N/D/H/E/P/T Soft unchanged. Key.L Soft was unused at e865a09.
Shell only Soft: existing _facade.GetDuration() + existing FormatTime only Soft; no Facade/Engine/Contracts change.
Soft != Windows PASS. KI-022/024 WINDOWS PENDING.
