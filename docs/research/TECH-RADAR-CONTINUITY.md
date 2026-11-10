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
- Addendum: TRA-20261106-001, TRA-20261110-001 (ID만 기록, 본문·SHA는 비서 미수령 UNRECEIVED)
- 보안 우선 항목: FFmpeg CVE-2026-125239 영향 확인은 수석 담당(재개 순서 2번).
