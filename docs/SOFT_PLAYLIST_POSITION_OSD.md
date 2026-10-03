# Soft Playlist Position OSD (S63)

- Previous/Next 이동 직후 OSD에 `{CurrentIndex+1} / {Items.Count}`를 표시합니다(파일명 없음). 표시 전용입니다.
- `Prev_Click`/`Next_Click`이 키(Ctrl+Left/Right, MediaPrevious/NextTrack)와 버튼에서 공유되므로 키와 버튼 모두 표시됩니다.
- 끝에서는 wrap 없이 인덱스가 유지되어 현재 위치(예: 3 / 3)를 그대로 표시합니다.
- `_playlist`가 null이거나 `CurrentIndex`가 범위 밖이면 OSD 없이 return합니다.
- OSD 칸은 다른 OSD와 공유하며 마지막 표시가 보입니다.
- Windows 확인 항목(Known Issue 아님): 빈 재생목록에서 이전/다음 시 OSD 없음, 끝에서 표시, 다른 OSD와 겹침.
- Soft≠Windows PASS.
