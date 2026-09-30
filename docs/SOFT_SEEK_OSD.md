# S57 Soft Seek Time OSD

- Shell only. `SoftSeekBySeconds` calls `ShowSeekOsd(target, duration)` right after `Seek`.
- Applies to Shift ±10, Ctrl+Shift ±30, PageUp/PageDown ±60. Left/Right, Home/End, digits unchanged.
- Text `position / duration` via `FormatTime` (`--:--` when duration unknown); existing OSD timer reused.
- Soft != Windows PASS.
