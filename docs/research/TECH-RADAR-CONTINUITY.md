> 이관 기록: 2026-11-10 STEP 1(SoT 정합)에서 비서 봇이 컴퓨터 로컬 문서 폴더(git 저장소 아님)에서 옮겼다. 원문은 그대로 두고 사용자 경로만 `C:\Users\<사용자>`로 가렸다. 지난 상태 표기는 아래 원문에 남아 있으며, 최신 항목은 파일 끝에 있다.

# Technology Radar Continuity (from 기술조사, recorded 2026-10-23 10:50 KST)
- Status ACTIVE; last success 2026-10-23 10:12 KST; next 2026-10-27 10:09 KST
- Baseline: mpv/FFmpeg shinchiro 20261021 (mpv git e471496c56 / ffmpeg git 9a861e968), mpv v0.41.0, FFmpeg 9.0.2/8.1.3; .NET 10.0.13 / SDK 10.0.402; NVIDIA 621.01, AMD 26.10.1, Intel 32.0.101.9150; PotPlayer 261021 (reference only), VLC 3.0.24, MPC-BE 1.9.2, MPC-HC 2.8.4; libass 0.17.5
- Material Change: Addendum TRA-20261023-001; watching libass 0.17.6 (security fix PR pending)
- Escalation: none. Detailed search logs not stored (per 기술조사).

## Update (from 기술조사, recorded 2026-10-28 09:07 KST; during SAFE STOP)
- Last success 2026-10-27 10:17 KST; next 2026-10-30 10:09 KST
- Baseline: mpv v0.41.0, FFmpeg 9.0.2/8.1.3, libplacebo v7.387.0 (project uses v7.349.0), .NET 10.0.14 / SDK 10.0.403, shinchiro 20261021
- Addendum TRA-20261027-001: libass CVE-2026-95127 raised to HIGH 7.5 (security escalation, watch kept); .NET OOB Brotli CVE-2026-111497 (exposure low)
- Note: project build uses SDK 10.0.402 (PR1MERGE build). Action decision belongs to 총괄/수석 after reset.

## 2026-10-30 Addendum TRA-20261030-001 (SHA 226620c2…b38, 비서 검증)
libass #961 merged; no CVE update / no release; urgency LOW. RC License/Security Gate item: libass ≥0.17.6 bump or #961/#955/#962 backport. Radar ACTIVE; next run 2026-11-03 10:09 KST.

## 2026-11-10 최소 갱신 (총괄 지시, STEP 1)
- 상태: ACTIVE
- 마지막 실행: 2026-11-10 10:25 KST, 다음 실행: 2026-11-13 10:09 KST (per 총괄)
- 기준선: mpv 0.42.0, FFmpeg 9.0.2/8.1.3, libplacebo 7.387.0 (per 총괄). 이전 기준선 mpv v0.41.0은 SUPERSEDED.
- Addendum: TRA-20261106-001, TRA-20261110-001 — 최초 기록 시 UNRECEIVED였으나 2026-11-10 10:2x KST 비서가 본문과 SHA-256을 직접 확인함(아래). 위 UNRECEIVED 표기는 SUPERSEDED.
  - TRA-20261106-001 (SHA-256 `ffb4cd1b50e74f2e0eed6aa37bda81e7b89a958b9a4cf1b43bc70cb52828cd5d`, 총괄 제시값 `ffb4cd1b…cd5d`와 일치): mpv v0.42.0 안정판 출시(libmpv D3D11 render API, libplacebo ≥ v7.387.0 요구, 프로젝트 번들 v7.349.0과 차이). 공식 LGPL Windows libmpv 자산 있음. shinchiro 바이너리는 여전히 GPLv3 FFmpeg로 제품 번들 불가. 드라이버 기준 후보 NVIDIA 621.14, AMD 26.10.2, Intel 32.0.101.9175. 신규 HIGH 보안 없음(per 기술조사). 채택 시점·영향 평가는 수석, 결정은 총괄.
  - TRA-20261110-001 (SHA-256 `963fba4e82e98529d421054cf7f05d1deb2be1357cc71392377a8fee7d24eb0d`, 총괄 제시값 `963fba4e…eb0d`와 일치): 우선(PRIORITY) 보안 에스컬레이션. FFmpeg CVE-2026-125239(HIGH 7.3, CNA 점수)는 HEVC DXVA2/D3D11VA 디코드 힙 버퍼 오버플로로 "through 9.0.2" 영향, 수정 릴리스 없음, 상류 PR #25136 미병합. 프로젝트 D3D11VA 경로에 직접 해당(per 기술조사). 번들 FFmpeg 버전·구성은 미확인(EVIDENCE INSUFFICIENT). 부수 항목: mpv CVE-2026-123095(ytdl_hook, 0.42.0에서 수정, 프로젝트 해당 여부 UNVERIFIED). 영향 확인은 수석 담당.
- 보안 우선 항목: FFmpeg CVE-2026-125239 영향 확인은 수석 담당(재개 순서 2번).

## 2026-11-10 정정: 프로젝트 번들 mpv 기준선 (총괄 Gate SEC-TRA-20261110-001)
- 프로젝트가 실제로 번들한 libmpv는 mpv **v0.40.0** 계열이다(per 수석, `libmpv-2.dll` 문자열 `mpv v0.40.0`, meson 로그 `Project version: 0.40.0`). clone 커밋 `e48ac7ce08462f5e33af6ef9deeac6fa87eef01e`가 정확히 v0.40.0 태그인지는 미확인(UNVERIFIED, per 수석).
- TRA-20261106-001의 "was v0.41.0" 서술과, 이 문서 2026-10-23·10-28 항목의 "mpv v0.41.0" 기준선은 프로젝트 번들 기준으로는 사실과 달라 SUPERSEDED. 원문은 이력으로 보존하며, TRA 원본 문서는 기술조사 봇 소관이라 수정하지 않았다.
- 위 11-10 최소 갱신의 "mpv 0.42.0"은 상류 최신 안정판 기준선이며 프로젝트 번들 값이 아니다. 번들 FFmpeg는 n7.1.1(per 수석)이다.
- 근거: `/workspace/rca/SEC-TRA-20261110-001-ARCH.md` SHA-256 `45180c528ee8ae8102689e673885e272eb0045df4db432ad268c15f4f8308c26`(비서 직접 확인, 총괄 제시값 `45180c52…8c26`과 일치).
- TRA-20261106(mpv 0.42 D3D11 render API)은 INVESTIGATE 유지(총괄).
