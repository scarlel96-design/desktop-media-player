# USAGE-BUDGET-POLICY (Permanent)

| Field | Value |
|-------|-------|
| Written | 2026-09-09 22:25 KST |
| Status | **ACTIVE · Permanent SoT** (총괄 P0) |
| Supersedes | GREEN≥40% / YELLOW20–40% / ORANGE10–20% / RED<10% (**폐기**) |

## Bands (remaining R)

| Band | Remaining | Next check | Action |
|------|-----------|------------|--------|
| **GREEN** | R ≥ **50%** | ≤60m | 정상 작업 |
| **YELLOW** | **30% ≤ R < 50%** | ≤30m | 감시 강화 · 대형 신규 신중 |
| **ORANGE** | **5% < R < 30%** | ≤10m | **새 대형 작업 금지** (기존 ORANGE 규칙 유지) |
| **RED** | R ≤ **5%** | — | **SAFE STOP NOW** + USAGE-HALT |

## Ops rules

| Rule | Value |
|------|-------|
| Missed Next check | **immediate recheck** |
| Dashboard fail | band = **UNKNOWN** |
| Resume Gate | Preflight + remaining이 **GREEN 복귀**(권장 R ≥ **50%**) + 총괄 지시 |

## Notes

- 가짜 %·추정 금지 · Dashboard 실측만
- Caveman LIMITED 정책과 독립 · Usage band만 본 문서

## Clarification (2026-09-10 03:11 KST)

| Band | Meaning |
|------|---------|
| **ORANGE** | **새 대형 작업 금지** · **전부 freeze 아님** · Soft/throttle 가능 |
| **RED** (R≤5%) | **SAFE STOP NOW** + USAGE-HALT |

Prior “hard freeze ON” at ORANGE was **over-applied** · corrected 2026-09-10 03:11 KST.

## Boundary clarification (2026-09-18 01:56 KST)

| Case | Band |
|------|------|
| R **= 50%** | **GREEN** (R ≥ 50%) |
| R **= 30%** | **YELLOW** (30% ≤ R < 50%) |
| R **= 5%** | **RED** · **SAFE STOP** (R ≤ 5%) — not ORANGE |
| **RESUME CANDIDATE** | Remaining **≥ 30%** (Recovery Watch → Preflight) |

Routines (사용량 감시 P0 Usage Monitoring · 총괄 Project Usage Recovery Watch) must use these exact bounds.
