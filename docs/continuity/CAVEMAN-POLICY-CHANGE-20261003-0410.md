# CAVEMAN 정책 변경 — 공식 기록 (2026-10-03 04:10 KST)

| 항목 | 값 |
|---|---|
| 기록 시각 | 2026-10-03 약 04:20 KST (비서 봇 기록) |
| 지시 출처 | 사용자 지시 2026-10-03 04:10 KST, 총괄 봇 relay 를 비서 봇이 기록 |
| 기록 방식 | 공식 SoT 기록. 전문 기록, Caveman 압축 없음 (이 문서는 Caveman OFF) |
| 대체/정련 대상 | 2026-09-18 03:43 KST `CAVEMAN-ROLLOUT-BROAD-MEASURED-20260918.md` 의 FULL/LITE/OFF 매핑 (해당 파일 끝에 SUPERSEDED-BY 줄을 덧붙였고 본문은 수정하지 않음) |
| 근거 조사 | `/workspace/artifacts/CAVEMAN-EFFECTIVENESS-EVIDENCE-20261003.md` (읽기 전용 조사. 사용량 절감 INCONCLUSIVE, 품질 CONFIRMED 0건 / SUSPECTED 1건) |
| 연관 기록 | `CAVEMAN-MATCHED-COMPARISON-LEDGER.md`, `CAVEMAN-MODE-HISTORY-LEDGER.md`, `RECORD-CONTAMINATION-SOFT-TOKEN-20260924.md` |

## 1. 새 CAVEMAN 정책 (사용자 지시 내용)

### 1.1 기본 모드 매핑

| 모드 | 적용 대상 |
|---|---|
| **OFF (기본)** | SoT, Handoff, Gate Report, Build/Test, QA/Verification, Red Team, Root Cause, Known Issues, Decision Log, CHANGELOG/Release Notes, Artifact Manifest, Commit/Build ID/SHA/PASS-FAIL/Evidence |
| **LITE** | 봇 간 전달(bot-to-bot delivery), 상태 보고(status reports), 총괄의 짧은 작업 지시, 비서의 내부 relay |
| **FULL** | **기본 적용은 중단(STOPPED).** 재검증 Pilot 에서만, 저위험 내부 작업에 한해 허용 |

- 즉 2026-09-18 BROAD MEASURED 의 "내부 작업 FULL" 기본 적용은 이 정책으로 중단된다.
- 이 문서가 열거한 OFF 대상은 압축·요약하지 않고 전문 기록한다.

### 1.2 비교 방법

- **단순 시간순 교대 비교(simple chronological alternating comparison)는 금지**한다.
- 대신 **유형별 matched comparison** 을 사용한다. 유형: Shell-only 소형 구현, 내부 분석, 기술조사, 디버깅, 수석 구조 리뷰. 같은 유형 안에서 **난이도가 비슷한 Atomic** 끼리 짝지어 비교한다.
- 최소 표본 **8개**. 대시보드 해상도가 부족하면 **최대 20개**까지 확장한다.

### 1.3 Ledger 필드

Mode, Atomic/Scope, Start/End Remaining, Dashboard delta, Bot turns, Response/instruction length, Re-ask, Rework, Build/Test result, Information Loss, Requirement Omission, Named Bot invocation issue, First-pass success, Quality Regression.

Ledger 파일: `CAVEMAN-MATCHED-COMPARISON-LEDGER.md`

### 1.4 판정 기준

| 조건 | 판정/조치 |
|---|---|
| 절감 ≥ 20% 이고 품질 하락 없음 | FULL 확대 **검토** |
| 절감 ≥ 10% 이고 품질 안정 | 제한적 확대 **검토** |
| 절감 0~10% 이거나 해상도 부족 | **INCONCLUSIVE 유지** |
| 절감 없음 / 사용량 증가 / 정보 손실 / 재작업 증가 | 해당 모드 **롤백** |

### 1.5 항목 5 (오염) 처리

- 09-24 "Soft" 토큰 오염은 **별도 기록**으로 처리한다: `RECORD-CONTAMINATION-SOFT-TOKEN-20260924.md` (라벨 RECORD CONTAMINATION / ROOT CAUSE UNKNOWN).
- **이 기록은 Caveman 을 원인으로 단정하지 않는다(NOT asserting Caveman as cause).** 원인은 UNKNOWN 이다.

## 2. 기존 정책과의 관계

- 2026-09-18 BROAD MEASURED(공식 기록 OFF · Bot 지시 LITE · 내부 작업 FULL · 사용량 절감 RE-MEASURING · 최소 표본 8/불확실 시 20)를 이 정책이 **대체·정련**한다. 구 문서 본문은 삭제·수정하지 않고 끝에 `SUPERSEDED-BY` 줄만 덧붙였다.
- 유지되는 것: 공식 기록 OFF, 최소 8/최대 20 표본, 품질을 사용량과 동등하게 보는 원칙(`CAVEMAN-QUALITY-COEQUAL-20260918.md`), 품질 라벨 NONE/SUSPECTED/CONFIRMED.
- 바뀌는 것: FULL 의 기본 적용 중단, 시간순 교대 비교 금지, 유형별 matched comparison, 판정 기준 임계값(≥20%/≥10%/0~10%/롤백 조건)의 재정의.

## 3. 현재 증거 상태 (이 정책이 시행되는 시점의 사실, 기록된 값만)

- 09-24 03:19 이후 Caveman 모드를 적은 문서가 없다. 현재 각 봇의 실제 모드는 **UNKNOWN** 이다. 봇별 활성화는 PROVEN 되지 않았다(`CAVEMAN-GROK-COMPAT-RESULT-2026-09-06.md`: Bot마다 Enable 필요, Named Bot 자동 로드 NOT PROVEN).
- 사용량 절감 판정: **INCONCLUSIVE** (증거 조사 §0). matched pair 수: **0쌍**.
- 품질: CONFIRMED 0건, SUSPECTED 1건(LOCK 원문 비서 미도달 S67·S69·S70·S73, 원인 미확정).
- 같은 날 04:11 사용량 기록(`USAGE-STATUS-20261003-0411.md`)의 소모는 Caveman 모드와 연결하지 않는다(모드 기록 UNKNOWN).

## 4. 즉시 적용 사항 (비서 관점)

- 비서의 공식 기록은 OFF(전문 기록)를 유지한다. 비서의 내부 relay 는 LITE 허용 대상이나, 의미가 모호하면 비압축한다.
- 장부 2개를 신설했다: `CAVEMAN-MATCHED-COMPARISON-LEDGER.md`, `CAVEMAN-MODE-HISTORY-LEDGER.md`. 모드를 전환할 때마다 모드 이력 장부에 1줄을 추가한다.
- Decision Log 에 04:10 항목을 추가했다(추가만, 기존 항목 수정 없음).

---

## 5. 추가 지시 (2026-10-03 04:13 KST, 사용자 지시, 총괄 relay) — 사전 고정(pre-registration) 규칙

(이 절은 04:13 에 뒤에 덧붙인 것이며 위 본문은 수정하지 않았다.)

1. **FULL Pilot 은 결과를 본 뒤 표본을 고르지 않는다.** Atomic 시작 **전에** Mode 를 장부에 **사전 고정**한다(selection bias 방지). 장부에 `Mode 고정 시각`, `고정 근거` 열을 둔다.
2. **시작 후 Mode 를 바꾸면** 변경 사실은 기록하되, 해당 표본은 비교에서 **제외(Excluded)** 로 표시한다.
3. **소급 항목(S73~S75 등)** 은 사전 고정이 되지 않았으므로 `Mode 고정 시각 = UNKNOWN`, 표시는 **`retro-only, not eligible for verdict`** 이다. 판정(verdict) 표본 수에 넣지 않는다.
4. 이 규칙은 `CAVEMAN-MATCHED-COMPARISON-LEDGER.md` 헤더에도 같은 내용으로 기재했다.
