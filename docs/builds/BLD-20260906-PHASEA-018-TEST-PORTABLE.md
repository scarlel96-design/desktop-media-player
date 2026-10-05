# TEST BUILD — BLD-20260906-PHASEA-018 Portable (Windows user snapshot)

| Field | Value |
|-------|--------|
| Kind | TEST BUILD / Portable ZIP (Installer 금지) |
| Latest Commit/tip | `e86f8d8` / `e86f8d8bcb3715b956dbff7a6953bc605242f3d2` |
| Build ID | `BLD-20260906-PHASEA-018` (동일 tip 재패키지 · 소스 변경 없음) |
| Config | Release \| x64 \| `-r win-x64` framework-dependent |
| Host | Linux box · .NET 10.0.400 · EnableWindowsTargeting |
| Native | `BLD-20260905-NATIVE-002` · 19 DLLs under `native/win-x64/` (+ beside exe) |
| Native ZIP SHA-256 | `85d84e09ff1c66eeb6daad27a0a7262eca8dc098865fe456ca147a587b413878` |
| Portable ZIP | `/workspace/artifacts/dmp-windows-test-BLD-20260906-PHASEA-018-portable.zip` (~57 MB) |
| ZIP SHA-256 | `bf8f21a2bec8ad8e8ee97bcda8b6f359685bf4ae026c39dbaceedf000ee4d236` |
| Shell.exe SHA-256 | `3a2f1ee381a2a72cd2e36f1d624fc114ea87023c581d5a0cf3e81bd9d6589cae` |
| Shell.dll SHA-256 | `6a92c886a08a844df4eb24fa7ab398b43080f7db2c9c98589f932dabade0e1ba` |
| Atomic scope | S0–S18 Soft (S19 / 신규 Atomic 미포함) |
| Windows UI | **NOT TESTED** |
| Spike PASS | not claimed |
| CI GREEN | not claimed |

## Run
Unzip → need .NET 10 Desktop Runtime win-x64 → `DesktopMediaPlayer.Shell.exe`

## Prefs / logs
- `%LocalAppData%\desktop-media-player\window-bounds.json`
- `%LocalAppData%\desktop-media-player\volume-prefs.json`

## Final release asset (2026-09-09 22:44 KST)

| Field | Value |
|-------|-------|
| Tag | `test-BLD-20260906-PHASEA-018` |
| URL | https://github.com/scarlel96-design/desktop-media-player/releases/tag/test-BLD-20260906-PHASEA-018 |
| ZIP SHA-256 **최종** | `08712d7329360cf24c68eeb256f12cd4dc65bf95d69e27c1ce4f72fb3b297134` |
| Prior ZIP SHA | `bf8f21a2bec8ad8e8ee97bcda8b6f359685bf4ae026c39dbaceedf000ee4d236` **폐기** |
