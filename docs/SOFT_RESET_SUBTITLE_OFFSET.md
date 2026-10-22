# Soft Reset Subtitle Offset (S53 Soft)

Shell Soft: Backspace SoftResetSubtitleOffset Soft → _subtitleOffsetSeconds = 0 Soft → existing _facade.SetSubtitleOffset(0) Soft + existing ShowSubtitleOffsetOsd Soft ("Subtitle +0.0s").
Soft no-op Soft if no facade Soft / offset already 0 Soft / exception Soft. Backspace Soft skipped when OriginalSource is TextBox Soft (no TextBox in MainWindow.xaml at 83e26fc; guard only).
Ctrl+P Playlist Soft · [ ] offset keys Soft · Ctrl+Shift+C/N/D/H/E/L/P/R/T Soft unchanged. Key.Back Soft was unused at 83e26fc.
Shell only Soft: existing _subtitleOffsetSeconds / SetSubtitleOffset / ShowSubtitleOffsetOsd only Soft; no Facade/Engine/Contracts change.
Soft != Windows PASS. KI-022/024 WINDOWS PENDING.
