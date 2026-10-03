# Soft Playlist Enter Play (S65)

- 재생목록 목록(`PlaylistList`)에 포커스가 있을 때만 Enter로 선택 항목을 재생합니다.
- 더블클릭과 동일 동작입니다. 더블클릭 본문을 `PlaySelectedPlaylistItem()`으로 옮겼고 호출 순서와 가드는 그대로입니다.
- 선택이 없거나 빈 목록이면 아무 동작도 하지 않습니다.
- 다른 컨트롤에서 누른 Enter는 처리하지 않습니다(`Handled` 미설정).
- Windows 확인 항목(Known Issue 아님): 키보드로 항목 선택 후 Enter 재생, 다른 버튼 포커스에서 Enter가 가로채이지 않음, 재생 후 목록 선택·현재 항목 표시 유지.
- Soft≠Windows PASS.
