# S59 Soft Seek Jump OSD

- Home/End/숫자키(D0–D9, NumPad0–9) 점프 시크에도 S57 `ShowSeekOsd(target, duration)`의 "위치 / 길이" OSD가 표시됩니다.
- Home: `_facade` null 가드 후 `Seek(0)`과 `ShowSeekOsd(0, duration)`을 호출합니다. 길이가 0 이하여도 OSD를 띄우며 길이는 `--:--`로 표시됩니다.
- End와 숫자키: 기존처럼 `duration <= 0`이면 no-op이라 Seek도 OSD도 없습니다. 그 외에는 `Seek` 직후 OSD를 표시합니다.
- 변경은 `SoftSeekHome`/`SoftSeekEnd`/`SoftSeekFraction` 안에만 있으며 키 매핑, case 순서, 기존 가드는 그대로입니다. 숫자키는 지역 변수 `target`을 쓰고 `n` clamp는 유지합니다.
- Shell만 변경, 새 API 없음. Soft≠Windows PASS, KI-022/024 WINDOWS PENDING.
