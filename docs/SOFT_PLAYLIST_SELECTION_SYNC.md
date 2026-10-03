# Soft Playlist Selection Sync (S66)

- `RefreshPlaylistUi()`가 목록을 다시 채운 뒤 현재 항목을 선택하고 스크롤합니다. 재생 후 현재 항목이 선택됩니다.
- 다음/이전 곡으로 바뀌면 선택과 스크롤이 따라갑니다.
- 현재 항목이 없거나 범위 밖이면 선택 없음이 유지됩니다.
- 방향키로 다른 항목을 고르던 중 `RefreshPlaylistUi`가 불리면 선택이 현재 항목으로 돌아가며, 이는 의도된 동작입니다.
- Windows 확인 항목(Known Issue 아님): Enter 재생 후 선택 유지, 긴 목록에서 다음 곡 시 현재 항목이 보임, 패널을 열 때 현재 항목 선택과 스크롤.
- Soft≠Windows PASS.
