# Soft Fine Volume (S54 Soft)

Shell Soft: Shift+Up → existing AdjustVolumeBySteps(+1) Soft · Shift+Down → existing AdjustVolumeBySteps(-1) Soft. Slider Soft → Facade SetVolume Soft → existing Volume OSD Soft; clamp 0..100 stays in existing logic Soft.
Condition Soft: Keyboard.Modifiers == ModifierKeys.Shift (Shift only Soft). Ctrl+Up/Down and Ctrl+Shift+Up/Down Soft still take plain Key.Up/Key.Down (±5) Soft unchanged.
New cases Soft placed directly before plain Key.Up Soft. Existing Shift Left/Right Soft · Ctrl+P · [ ] · Backspace · Ctrl+Shift+C/N/D/H/E/L/P/R/T Soft unchanged. ListBox Soft keeps its own Up/Down handling Soft (not bypassed).
Shell only Soft: existing AdjustVolumeBySteps only Soft; no Facade/Engine/Contracts change.
Soft != Windows PASS. KI-022/024 WINDOWS PENDING.
