# Soft Stopped OSD (S64)

- 정지 직후 OSD에 `Stopped`를 표시합니다. 표시 전용입니다.
- 버튼, Ctrl+S, MediaStop이 모두 `Stop_Click`을 거치므로 키와 버튼 모두 표시됩니다.
- 미디어가 없을 때 정지해도 `Stopped`가 표시됩니다(S60 `Play`와 같은 설계 특성).
- OSD 칸은 다른 OSD와 공유하며 마지막 표시가 보입니다.
- Windows 확인 항목(Known Issue 아님): 미디어 없을 때 정지 시 `Stopped` 표시, 정지 직후 시크 바·시간 레이블 0과 함께 표시, 다른 OSD와 겹침.
- Soft≠Windows PASS.
