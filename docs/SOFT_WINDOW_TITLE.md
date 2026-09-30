# S56 Soft Window Title Filename

- Shell only. `OnFirstFrame` calls `SoftUpdateWindowTitle()` last inside the Dispatcher block.
- Title = `<file name> - Desktop Media Player`; reads `_currentPath` only.
- Empty name or exception: title unchanged (no-op). Not reset on Stop.
- Soft != Windows PASS.
