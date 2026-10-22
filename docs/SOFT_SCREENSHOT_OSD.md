# Soft Screenshot OSD (S62)

- 스크린샷 저장 직후 "Screenshot → 파일명" OSD를 표시한다.
- 기존 저장 대화상자, Facade Screenshot 호출, 상태 텍스트는 그대로이며 표시 전용이다. 저장을 취소하면 OSD는 없다.
- Facade.Screenshot에 반환값이 없어 실제 파일 생성 여부는 Windows 확인 항목이다.
- 자막 OSD 칸을 시크/재생·일시정지/프레임 OSD와 공유하며 마지막 표시가 보인다.
- Facade/Engine/Contracts 변경과 새 API는 없다.
- Soft 검증이며 Windows PASS가 아니다.
