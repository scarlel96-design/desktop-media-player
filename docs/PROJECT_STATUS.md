# Project status (sync to repo README as needed)

> ## DOC DRIFT — 2026-09-24 03:47 KST (비서 · tip 포인터 LITE sync after S49 Soft Closed)
>
> **This file is STALE.** Body still reflects tip **`9b09e10`** / `BLD-20260905-PHASEA-006` / S7–S8 era (Last sync SoT: 2026-09-05).
>
> **Do not treat the “Now” section below as current Source of Truth.**
>
> | Authoritative now | Value |
> |-------------------|-------|
> | Continuity NOW | `docs/continuity/CONTINUITY-STATUS-NOW.md` |
| Canonical tip | **`96b9175129bd1b75bc2a75501178411960d6f3b4`** (S97 Soft Closed) |
| Build | **BLD-20261002-PHASEA-S97-001** |
| Soft stage | S97 Closed, Windows 점검 1회 대기 |
> | GitHub | Soft `integrate/s19-ki022-ki024` Soft HEAD=`5512d7b` (STALE, 2026-10-03 정정: 현행 Canonical 963bdb9 · git tip 6f5a69f S73 진행 중 · `continuity/CONTINUITY-STATUS-NOW.md` 참조) · Soft **`main`=`8d9494d` LAG** Soft |
> | Soft≠PASS | Soft ≠ Windows PASS · Windows OPEN / KI-022·024 WINDOWS PENDING |
> | Standing Product Requirement | Benchmark Policy (user-confirmed 2026-10-02 03:16 KST): `docs/continuity/STANDING-PRODUCT-REQUIREMENT-BENCHMARK-POLICY-20261002.md` · applies to all phases · does not change Soft flow |
>
> ### Update plan (LITE — no large README rewrite)
> 1. Keep banner until a dedicated Soft status sync replaces the “Now” block. **유지**
> 2. Soft Closure tip/Build/Soft stage pointers → Continuity NOW. **DONE S39–S46** · **DONE S47 01:05** (Canonical `30a62ca` · BLD-…-S47-001) · **DONE S48 03:30** (Canonical `8e4ceb7` · BLD-…-S48-001) · **DONE S49 03:47** (Canonical `bec19f7` · BLD-…-S49-001)
> 3. Do **not** bulk-rewrite README or historical Done lists in this pass. **유지**
> 4. Recheck index: `docs/continuity/CONTINUITY-RECHECK-LITE-20260924.md`

---

## Historical body (STALE — tip `9b09e10` era)

Last sync SoT: 2026-09-05 04:58 KST · PS-2026-09-05-043

## Now

- Phase: Core Playback MVP (Phase A)
- GitHub tip: `9b09e10`
- Build ID: `BLD-20260905-PHASEA-006` (Release\|x64, tests 17/17)
- Spike Gate: PASS WITH KI (NATIVE-002 baseline)
- S7: build SUCCESS — awaiting Arch re-confirm + integration regression
- S8: blocked until approvals/Gate

## Verified vs not

| Check | Status |
|-------|--------|
| Release\|x64 build | PASS (`BLD-20260905-PHASEA-006`) |
| Unit tests | PASS 17/17 |
| Native staging this tip | NOT RUN (Windows host offline) |
| Windows runtime new UI | NOT TESTED |
| GitHub CI status | empty — do not say CI GREEN |

## Done vs not

**Done (S0–S7):** playback facade, chrome, timeline/seek, transport, volume, FS, sub/audio, playlist L2, theme, hotkeys, resume, screenshot, media info (engine props), frame step.  
**Not in scope yet:** buffer/hover polish, Blur/Acrylic on, P1 features, settings search, overlay subtitles.

## Known Issues (open)

KI-013 UX debt · KI-014 Soft frame-drop · KI-016 Soft CS0108
[복원 2026-10-03: 09-24 오염 'Soft' 토큰 제거, 원본은 contaminated-originals-20260924/ 보존, 원인 UNKNOWN]
