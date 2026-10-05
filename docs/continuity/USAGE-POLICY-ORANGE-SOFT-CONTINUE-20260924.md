# Usage Policy — OFFICIAL SoT (user corrected)

| Field | Value |
|-------|-------|
| Written | 2026-09-24 03:19 KST |
| Status | **AUTHORITATIVE** · supersedes ORANGE=Soft Atomic LOCK HOLD misread |
| Source | 사용자 관제실 정책 해석 수정 · Sentinel ACK |

## Band table
| Band | Remaining | Mode | Cadence |
|------|-----------|------|---------|
| GREEN | ≥50% | 정상 진행 | 60m |
| YELLOW | 30~<50% | 효율 진행 | 30m |
| ORANGE | >5~<30% | 절약·순차 진행 | 10m |
| RED | ≤5% | **SAFE STOP** | — |

## ORANGE policy (CURRENT LIVE)
| Key | Value |
|-----|-------|
| CONTINUE WORK | **YES** |
| CONCURRENCY | **1** |
| CORE WORK ONLY | YES |
| NEW SMALL ATOMIC | **ALLOWED** |
| HEAVY PARALLEL WORK | HOLD |
| DUPLICATE/RERUN | **FORBIDDEN** |
| CAVEMAN | **ACTIVE** |
| USAGE CHECK | ≤10 min |

## Soft Atomic LOCK HOLD
**REMOVED** as ORANGE-only rule. Soft Atomic 전면 HOLD 해석 **폐기**.

## SAFE STOP only when
1. Remaining **≤5%** (RED)
2. 실제 Hard Block
3. 사용자 명시 STOP

Burn rate 급악화 → Sentinel 즉시 보고 · 총괄이 작업량 축소 가능. ORANGE alone ≠ Soft stop.

## LIVE snapshot at policy restore
| Field | Value |
|-------|-------|
| Remaining | **27% ORANGE** |
| Soft | Soft 본선 **CONTINUE** · concurrency **1** · S48 Soft PARK **RELEASED** |
| Soft tip / Soft Closed Canonical | `30a62ca` (S47 Soft Closed) Soft≠PASS |
| Soft HOLD (prior misread) | **SUPERSEDED** |
