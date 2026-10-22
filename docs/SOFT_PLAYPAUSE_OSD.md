# S60 Soft Play/Pause OSD

- Space/MediaPlayPause 키로 재생·일시정지를 토글하면 기존 OSD 칸(`SubtitleOsdText`)에 "Paused" 또는 "Play"가 표시됩니다.
- 새 헬퍼 `ShowPlayPauseOsd(bool paused)` 1개이며, 기존 `_osdShownUtc`/`_osdFadeTimer` Stop→Start 패턴을 그대로 씁니다.
- 호출은 `Window_KeyDown`의 Space/MediaPlayPause 분기에서 `Pause_Click`/`Play_Click` 직후 한 번씩만 합니다.
- 버튼 클릭에는 OSD 없음: `Play_Click`/`Pause_Click` 본문은 바뀌지 않았습니다.
- 키 매핑, case 순서, 분기 조건은 그대로입니다. 다른 OSD와 같은 칸을 공유해 마지막에 표시한 쪽이 보입니다.
- Shell만 변경, 새 API 없음. Soft≠Windows PASS, KI-022/024 WINDOWS PENDING.
