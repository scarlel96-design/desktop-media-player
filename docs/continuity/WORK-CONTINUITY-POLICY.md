# Work Continuity Policy — Phase A

| Field | Value |
|-------|-------|
| Written | 2026-09-06 13:16 KST |
| Authority | 총괄 【필수 Continuity】 |
| Status | Active |

## Rules

1. **Atomic SUCCESS 기본 후속** = 다음 Atomic **1개**만 즉시 요청 (고정). idle 금지.
2. **Unexpected Idle** → 원인 분류 + 자가 복구 시도.
3. **STOP-CAUSE / RECOVERY** 간단 기록 (Continuity 또는 PS).
4. **Preflight** (재개·다음 Atomic 전): HEAD · Branch · Build ID · Atomic · Next Exact Step · Usage · Hard Block · Windows req.
5. **사용자 개입** 필요 시 **묶음 요청** — 조용한 idle 금지.
6. **Usage Safe Stop ≠ 작업 오류** — Halt는 Usage 정책; 작업 실패와 혼동 금지.

## STOP-CAUSE taxonomy (간단)

| Code | Meaning |
|------|---------|
| NEXT_ATOMIC_NOT_DISPATCHED | Atomic SUCCESS 후 다음 Atomic 미배정 |
| AWAITING_ARCH_BOUNDARY | 수석 경계/권고 대기 |
| AWAITING_BUILD_SMOKE | 스모크/Build ID 대기 |
| AWAITING_USER_BUNDLE | 사용자 묶음 승인·실기 등 |
| USAGE_HALT | Usage Safe Stop (≠ 작업 오류) |
| HARD_BLOCK | 구매·라이선스·제품방향 등 |

## RECOVERY (간단)

| Field | Fill |
|-------|------|
| Cause | STOP-CAUSE code |
| Action | 자가 복구 조치 한 줄 |
| Owner | 담당 Bot |
| Next Exact Step | 구체 다음 행동 |

Recorded by: 비서 봇

## Named Bot invocation (2026-09-06 13:20 KST)

See `docs/continuity/NAMED-BOT-INVOCATION-RULES.md`.

| Code | Meaning |
|------|---------|
| BOT_INVOCATION_FAILED | Named Bot 호출 실패 |
| BOT_NAME_NOT_RESOLVED | 등록 명칭 미해석 |
| GENERIC_CONTEXT_USED_INSTEAD_OF_NAMED_BOT | Named Bot 대신 generic context |

Atomic Chain = Named Bot 호출 체인. 별칭만 ≠ 호출 성공. 실패 시 SoT 유지 + `@등록명칭` 재전달.
