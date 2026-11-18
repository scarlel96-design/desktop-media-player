# CI-FOUNDATION-01 요구사항 (기록: 비서 봇)

## REQ-CIF-01 native DLL 출처 고정 manifest (2026-11-10, 총괄 Gate SEC-TRA-20261110-001)
- 요구: `native/win-x64/` 아래 모든 native DLL(libmpv, FFmpeg 계열, libplacebo, libass 등)의 출처를 고정하는 manifest를 CI-FOUNDATION-01 범위에 포함한다.
- 배경(per 수석): `native/**/*.dll`은 `.gitignore`로 추적되지 않고, 저장소에는 `native/win-x64/README.md`만 있으며, 출처 URL·버전 고정 파일이 없다. 번들 FFmpeg 빌드 로그와 DLL 내용 사이 불일치(D3D12VA)도 원인 미확인으로 남아 있다.
- manifest에 담을 항목의 상세(파일명, 버전, 소스 커밋, 빌드 구성, SHA-256 등)와 형식은 수석 설계·총괄 승인으로 확정한다. 이 기록은 요구사항 등재만 한다.
- 참고(비서 확인): 저장소 밖 `/workspace/artifacts`에 `dmp-native-win-x64-BLD-20260905-NATIVE-002-PROVENANCE.md`·`-SBOM.json`·`-DLL-SHA256SUMS.txt`가 있다. 현재 번들 DLL과 일치하는지는 확인하지 않았다(UNVERIFIED).
- 상태: 요구사항 등재, 설계·구현 미착수.
