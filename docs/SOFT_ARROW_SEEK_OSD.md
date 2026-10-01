# S58 Soft Arrow Seek OSD

- 일반 Left/Right(±5초) 시크도 `SoftSeekBySeconds(-5)` / `SoftSeekBySeconds(5)`를 거쳐 S57의 "위치 / 길이" OSD가 표시됩니다.
- Right는 기존에 상한 clamp가 없었고 이제 길이에서 clamp됩니다. 길이가 0 이하이면 기존처럼 하한만 적용됩니다.
- Shell만 변경: `case Key.Left:` / `case Key.Right:` 본문 한 줄씩 교체. 키 매핑, case 순서, 수정자 분기는 그대로입니다.
- Facade/Engine/Contracts 변경과 새 API는 없습니다. Home/End/숫자키는 제외입니다.
- Soft≠Windows PASS. KI-022/024는 WINDOWS PENDING.
