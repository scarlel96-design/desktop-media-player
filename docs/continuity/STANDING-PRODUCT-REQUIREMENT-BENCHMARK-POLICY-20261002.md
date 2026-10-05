# STANDING PRODUCT REQUIREMENT — PRODUCT BENCHMARK POLICY

- Confirmed by: user (시온 바라), 2026-10-02 03:16 KST
- Relayed by: 총괄 봇 (Active Chief) to 비서 봇, 2026-10-02 ~03:17 KST
- Recorded by: 비서 봇, 2026-10-02 KST. Full text, no compression.
- Scope: whole project. Stays in force through Phase A, P1, P2, Release Candidate and after Release.
- Effect on Soft flow: none. Soft S60 continues with concurrency 1. Canonical stays S59 `0848690d29c09a1fb6f03c0dd79024e88335f549`. This policy does not change the Soft Atomic flow.
- Status of this record: SoT RECORDED. Per-bot ACKs: PENDING (see "ACK / Reference ledger" below).

## 1. UI/UX AUTHORITY
- UI/UX authority is the user-defined Lumen requirement: Lumen independent design, Apple-level polish, motion, micro-detail and consistency, AC-UI, D1-D5, and the Apple Visual Quality Gate.
- OTHER PLAYER UI is reference only. Copying the UI of PotPlayer, VLC, KMPlayer or MPC, imitating their layouts, or borrowing their design language is FORBIDDEN.
- Judgment priority: (1) user requirements, (2) Lumen Design System, (3) Visual Quality Gate, (4) competitor player UX reference.

## 2. USABILITY REFERENCE
- PotPlayer is reflected heavily as a usability reference.
- PotPlayer is a Familiarity / Workflow / Feature Coverage Reference only. It is NOT a UI Design Floor and NOT a Visual Target.

## 3. FEATURE BENCHMARK
- Targets: PotPlayer, VLC, KMPlayer, the MPC-BE / MPC-HC family, mpv, and other major players.
- STABILITY FLOOR and PERFORMANCE FLOOR: there must be no clear inferiority against major desktop players.

## 4. VISUAL TARGET
- Lumen independent design plus Apple-level detail, motion and consistency.
- A feature Benchmark PASS is NOT a Visual PASS. Visual quality is verified separately in Windows Validation.

## 5. RELEASE REQUIREMENT
- The release must have all of: competitive feature scope, stability, performance, usability, and independent visual quality.
- This policy is maintained through Phase A, P1, P2, Release Candidate, and after Release.

## 6. FEATURE ADOPTION RULE
- Evaluate each candidate on: NEED, USER VALUE, usage frequency, implementation complexity, performance and maintenance cost, License/Security risk, UI complexity, Architecture impact, PHASE.
- Classify as MUST / SHOULD / DIFFERENTIATOR / OPTIONAL / REJECT.
- A feature is NOT implemented automatically just because a competitor has it.
- Dynamic Sound is a DIFFERENTIATOR and must not conflict with DS-0 / DS-1.

## 7. ARCHITECTURE (maintained)
- Shell -> Application/Playback -> Facade -> Engine Host -> libmpv.
- FORBIDDEN: Shell calling mpv/FFmpeg directly; the UI building DSP strings; temporary hacks made for benchmarks.

## 8. PLAYER BENCHMARK GATE (before Release Candidate)
- A separate PLAYER BENCHMARK GATE is placed before the Release Candidate. Players: Our Player, PotPlayer, VLC, KMPlayer, MPC-BE, mpv.
- Items to record: Build ID; Commit; Test PC / CPU / GPU / Driver / RAM / OS; Media file / Codec / Container / Resolution / FPS / Bitrate / Audio format; HW Decode status; Startup / First Frame / Seek / CPU / GPU / RAM / VRAM / Dropped Frames / A-V Sync; Subjective Responsiveness; Stability.
- Performance judgment:
  - Clearly slower = FAIL candidate.
  - Abnormally high usage = Optimization Issue.
  - Similar to competitors = PASS range.
  - Faster, lighter or more stable = TARGET.

## 9. COMPETITOR REFERENCE ACQUISITION
- USER UPLOAD REQUIRED = NO. Uploading installers is not needed from the user.
- Official reference downloads are the Bots' responsibility.
- Targets: PotPlayer Win x64, VLC Win x64, KMPlayer 64X, MPC-BE Win x64, mpv Win x86_64.
- Source order: official Vendor page, official CDN, official GitHub Release, official distribution page, WinGet. If none is possible, report SOURCE UNVERIFIED.
- THIRD-PARTY BINARY IN PROJECT REPO = FORBIDDEN. GitHub records only: product name, version, Arch, official Source URL, Download date, SHA-256, Benchmark date, License note.
- Install and run only in an isolated Benchmark environment (Portable preferred).
- Apply a REFERENCE VERSION LOCK before starting. A newer version is registered separately as a NEW BENCHMARK BASELINE.
- PotPlayer and KMPlayer: Black-box Test only. VLC, MPC-BE, mpv: Black-box Benchmark and License-aware Source Research are kept separate. Code copying is FORBIDDEN.
- USER ACTION REQUIRED is used only for: login on an official download, CAPTCHA, region restriction, license-agreement judgment, payment, reproduction only possible on the user's PC, or source trustworthiness that cannot be confirmed.

## 10. REFERENCE RECORD FIELDS
- Per Reference: Product, Version, Arch, Filename, Official Source, Downloaded KST, File Size, SHA-256, Digital Signature, Publisher, Signing status.
- The installer file itself is NOT SoT. SoT keeps: Reference info, Test Environment, Test Corpus, Benchmark Method, Measured Result, Known Limitation, Test Date.

## ACK / Reference ledger (to be maintained by 비서 봇)
- Bot ACKs: none received yet (as of recording).
- Benchmark Reference baselines: 5 references acquired by 기술조사 봇 (2026-10-02 03:22~03:27 KST), VERSION LOCK, recorded verbatim in BENCHMARK-REFERENCE-LEDGER-20261002.md (PotPlayer 26.08.19.0, VLC 3.0.24, KMPlayer 64X 2026.7.24.12 [no hash cross-check], MPC-BE 1.9.1, mpv shinchiro 20261001 git 3186d369f9). Test Environment / Results: none yet. Do not fabricate entries; add only from real Bot reports.
