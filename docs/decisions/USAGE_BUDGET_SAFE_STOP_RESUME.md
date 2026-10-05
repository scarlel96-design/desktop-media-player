# Usage Budget / Safe Stop / Safe Resume Policy — Desktop Media Player (전문)

| Field | Value |
|-------|-------|
| Document ID | **DEC-USAGE-BUDGET-2026-09-05-001** |
| Updated | 2026-09-05 13:37 KST |
| Authority | 총괄 봇 【SoT 영구 기록】 |
| Status | Active · **축약 대체 금지** (PLUGIN_MATRIX와 동일 등급) |
| Path | `docs/decisions/USAGE_BUDGET_SAFE_STOP_RESUME.md` |

> **SoT lock:** 본 파일이 Usage Budget · Safe Stop · Safe Resume의 **공식 정책 SoT**다. Decision Log 포인터·채팅 요약·축약 표는 참고만 가능하며 **전문을 대체·축소·삭제하지 않는다.**  
> 기존 PRD / Architecture / Gate / SoT / 역할 / Continuity / PLUGIN_MATRIX와 **병행**한다. Usage 이유로 Gate·역할을 깨지 않는다.

---

## 1. Usage 확인

1. **확인 시점:** 작업 시작 · Gate 전후 · 장시간 작업 · 대형 작업(native rebuild, 대량 이동, 대규모 리팩터 등) 전.
2. **추정 / 날조 금지.** per-Bot %를 임의 계산·추정·날조하여 기록하지 않는다.
3. UI에 **명확히 표시된 수치만** 기록한다. 없거나 불명확하면 **`UNKNOWN`**.
4. UNKNOWN일 때는 “여유 있음”으로 가정하지 말고, 보수적으로 YELLOW 이상에 준하는 운영을 검토한다(날조는 여전히 금지).

---

## 2. 등급

| Grade | Remaining | ACTIVE Dashboard 주기 (Sentinel) |
|-------|-----------|----------------------------------|
| **GREEN** | **≥ 50%** | **60분** |
| **YELLOW** | **30–50%** | **30분** |
| **ORANGE** | **5–30%** | **15분** (구 10–29.99%/10분 **폐기**) |
| **RED** | **< 5%** 또는 quota/Usage **명시 경고** | **Safe Stop** (구 &lt;10% RED **폐기**) |

추가:
- 명시 경고만으로도 해당 단계(또는 더 보수적 단계)에 **준한다**.
- **RED 이후** 소폭 회복만으로 **즉시 재개 금지**.  
  **Auto Resume / Safe Resume** 허들: **remaining ≥ 30%** + **Resume Preflight PASS** (또는 reset 확인). §20·§21 정합.

---

## 3. YELLOW (30–50%)

- **우선:** Gate / 진행 중 작업 마무리 / 회귀 / Build · SoT 기록.
- **축소:** 장기 조사 · polish · 중복 조사.
- Evidence **재사용** (재수집·재조사 남발 금지).
- 총괄은 병렬성을 GREEN 대비 **축소** 조절.
- 비서: YELLOW 이상이면 Usage 상태를 SoT/로그에 **기록**.

---

## 4. ORANGE (5–30%)

- **새 큰 일 시작 금지.**
- 진행 중 일은 **Atomic Unit**으로 닫기 (깨진 중간 상태 최소화).
- **시작 금지 예:** 새 native / 대규모 rebuild / 대규모 리팩터 / 대량 파일·디렉터리 이동.
- 비서: **필수 Halt Handoff** 준비·기록 (ORANGE/RED).
- 총괄: 병렬성 **최소** (ORANGE 운영).

---

## 5. RED — Safe Stop (<5% 또는 명시 경고)

- **신규 개발 금지.**
- Task 상태를 다음 중 하나로만 남긴다:
  - `COMPLETED`
  - `SAFE CHECKPOINT`
  - `INCOMPLETE / DO NOT RESUME WITHOUT REVIEW`
- **금지:** 깨진 `main` 병합 · 테스트 삭제 · 기능 제거로 “성공” 위장 · **강제 PASS**.
- 비서: **필수 Halt Handoff** (`USAGE-HALT-YYYYMMDD-NNN`).
- 총괄: 병렬성 **RED** (신규 스트림 금지 · 안전 정지).

---

## 6. Checkpoint 필수 필드

Safe Stop / SAFE CHECKPOINT / Halt Handoff에 **최소한** 포함한다:

| Field | Notes |
|-------|-------|
| 시각 | 사용자 TZ 또는 KST 명시 |
| Usage | UI 수치 또는 **UNKNOWN** (추정 금지) |
| Phase / Gate | 예: Phase A · Spike PASS WITH KI · S9 PASS 등 |
| Branch | |
| Commit | tip SHA |
| Build / Native ID | 예: BLD-… · NATIVE-002 |
| SHA | exe/ZIP/DLL 등 해당 시 |
| PASS / FAIL / PENDING | Gate·검증 상태 |
| 검증 | 수행한 검증 요약 |
| Task | COMPLETED / SAFE CHECKPOINT / INCOMPLETE… |
| Bot 상태 | 누가 무엇에서 멈췄는지 |
| 파일 | 핵심 경로 |
| uncommitted | 있으면 명시 |
| KI | Open KI |
| Blocker | |
| 테스트 | unit/통합 결과 |
| 로그 | 경로·키 |
| Artifact | |
| 미판정 Plugin | **UNREVIEWED PLUGIN FINDING** 목록 |
| Next | 재개 시 첫 명령 |
| 명령 / Validation | Resume에 필요한 명령 |
| Windows | 해당 시 **WINDOWS VALIDATION PENDING** + 체크리스트 |

---

## 7. Handoff ID · Continuity · Resume Anchor

- Handoff ID 예: **`USAGE-HALT-YYYYMMDD-NNN`**
- 위치: **`docs/continuity/`** (또는 기존 Continuity 위치; 신설 시 본 경로 사용)
- **Resume Anchor:** Halt Handoff에 재개 기준 Commit/Build/Next를 고정
- Resume 시 비서가 **`RESUMED`** + 기준 Commit/Build를 SoT에 기록

---

## 8. GitHub 보존

Usage를 이유로도 금지:
- force push
- history rewrite
- 대량 삭제
- visibility 변경
- secret 변경  

(사용자 명시 승인이 있어도 Usage 압박만으로 비서/Bot이 단독 실행하지 않음 · 기존 파괴 작업 승인 규칙과 병행)

---

## 9. 실행 중 빌드

- Usage만으로 **무조건 kill 금지**.
- 가능하면 **완료·기록**.
- hang이면 **log / 명령 보존** 후 안전 중단 절차.

---

## 10. Plugin

- Usage 압박 시 Plugin 호출 **축소** · **재조사 금지** (Evidence 재사용).
- 미판정 결과 = **`UNREVIEWED PLUGIN FINDING`** 으로 Checkpoint/Handoff에 남김.
- **Sentry 외부 telemetry 금지**는 Usage와 **무관하게** 유지 (PLUGIN_MATRIX).

---

## 11. 완전 소진 후 첫 작업

- 첫 작업 = **Recovery** (신규 개발 아님).
- 우선순위: **GitHub + SoT + Artifact/Log** 복구·정합.

---

## 12. Safe Resume

순서:
1. **RESUME PREFLIGHT** (Usage 허들·Checkpoint·Git 상태)
2. smoke / hash / unit
3. **RESUME READY**

RED 직후 소폭 회복만으로 PREFLIGHT 생략 금지.  
허들: **remaining ≥ 30%** + Resume Preflight PASS (또는 reset 확인). §20·§21.

---

## 13. Task 상태별 재개

| Task label | Resume rule |
|------------|-------------|
| `COMPLETED` | **재실행 금지** |
| `SAFE CHECKPOINT` | **Next**부터 |
| `INCOMPLETE / DO NOT RESUME WITHOUT REVIEW` | **수석 / 통합** 판단 후 |

---

## 14. 재개 순서 (Bot)

1. 비서  
2. 총괄  
3. 수석 / 통합  
4. 빌드 / QA  
5. 구현 · 나머지  

---

## 15. 사용자 부재

- **자율 Safe Stop** 허용·권장 (ORANGE/RED·명시 경고).
- 깨진 상태면 **단일 Resume Brief**로만 재개 안내 (산발 재개 금지).

---

## 16. 절대 금지

- 테스트 생략 PASS  
- 기능 삭제·범위 축소로 성공 위장  
- 위험 대규모 신규 (ORANGE/RED)  
- 가짜 Usage %  
- Handoff 무시 재개  
- 강제 PASS · 깨진 main 병합  

---

## 17. 역할 요약

| Role | Duty |
|------|------|
| **비서** | YELLOW+ Usage 기록 · ORANGE/RED **필수 Halt Handoff** · Resume 시 **RESUMED**+기준 Commit/Build |
| **총괄** | 병렬성 GREEN/YELLOW/ORANGE/RED 조절 · Gate와 Usage 정책 동시 준수 |

---

## 18. Continuity 디렉터리

Halt Handoff 파일 예:
`docs/continuity/USAGE-HALT-YYYYMMDD-NNN.md`

---


## 개정 이력

| When (KST) | Memo |
|------------|------|
| 2026-09-05 04:15 | 초판 DEC-USAGE-BUDGET-2026-09-05-001 |
| 2026-09-05 04:17 KST | **§20–22** 초판 추가 (총괄). §1–19 유지. |
| 2026-09-05 04:28 KST | **§20·§21 전문 갱신**: Dashboard Browser Monitor Primary · API unavailable · Auto Resume remaining≥30%+Preflight · 확인 주기 · INCONSISTENT · §21 Dashboard-first. §1–19·§22 유지. |



---

---

## 20. Capability Reality (Grok Bot 실제 범위) — 2026-09-05 04:28 KST 전문 갱신

### 20.1 Usage 신호원

| Source | Status | Role |
|--------|--------|------|
| Official API Usage Monitor | **unavailable** | Bot/Routine이 Usage & Billing 잔여 %를 API/도구로 직접 읽을 수 없음 |
| **Cursor Dashboard Browser Monitor** | **available while authenticated** | **Primary runtime signal** (읽기 전용) |
| Fallback | — | Dashboard 실패·미인증·불명확 → **`UNKNOWN`** + **Resume Probe** (이전 Dashboard 값 **재사용 금지**) |

### 20.2 Dashboard Monitor (읽기 전용)

- **URL:** `https://cursor.com/dashboard/spending?for=grok%7Cuser_01M1H2J87PT1Z3988W02YF4HK0`
- 표시가 **used %** 이면 **remaining = 100 − used** (used와 remaining **혼동 금지**).
- **보안:** 쿠키 / 토큰 / 비밀번호를 SoT·채팅·Handoff에 **기록 금지**. 결제·플랜 변경 **금지**. 로그인 만료 = **`USER ACTION REQUIRED`** (Bot이 우회 로그인하지 않음).

### 20.3 Auto Resume threshold

- **Auto Resume** = **remaining ≥ 30%** **그리고** **Resume Preflight PASS**.
- Dashboard **GREEN으로 보여도** 실제 quota/Usage 오류가 나면 → 상태 **`INCONSISTENT` / `UNKNOWN`**, **실제 실패 우선** (Dashboard 낙관 수치로 재개하지 않음).
- Dashboard failure / 미인증 / 파싱 실패 → **`UNKNOWN`**. **이전 측정값 재사용 금지.**

### 20.4 Trigger · Routine

1. **Usage 회복 이벤트 Trigger는 없다.** (cron / Slack / GitHub / Origin / … 만 지원.)
2. 복구 감시는 **일정 기반 Routine**으로 대체 (§21).
3. Routine 실행 중 총괄은 **SendToAgent / 그룹 회의실**로 비서·수석·통합·빌드·QA·구현 등 재기동 **가능**.

### 20.5 Usage 확인 시점 (Dashboard Primary)

다음 시점에 Dashboard Browser Monitor(인증 유지 시) 또는 UNKNOWN 처리를 한다:

- 자율 작업 **시작**
- 약 **1시간** 경과 시 (장기 자율)
- **Gate 전**
- **대형** build / refactor 전
- **추가 병렬** Bot 투입 전
- quota **이상 징후**

운영:
- **ORANGE / RED** 이면 반복 조회보다 **Safe Stop 우선**.
- **GREEN** 에서 매시간 장황 로그 **금지** — Continuity / Decision에는 **상태 전환**만 짧게 기록.
- 비서: **YELLOW+** 전환·실측은 Continuity/상태 로그에 짧게. GREEN 장황 누적 금지.

### 20.6 사용자 부재 시 자동화 가능 범위

- **가능:** SoT `USAGE-HALT-*` 존재 확인 · Hard Block 여부 · GitHub HEAD/branch/commit · SoT·PR·Build ID 정합 · 최소 restore/build/smoke · (인증 시) Dashboard remaining · RESUME READY 시 Bot 순서 복원 (§14).
- **불가능:** Official API Usage % · Usage-recovery trigger · Windows 실기(사용자 PC) · Hard Block(구매·라이선스·제품방향·파괴적 git) 해제 · 미인증 Dashboard를 성공으로 위장.

### 20.7 Fallback = UNKNOWN + Resume Probe

Dashboard를 쓸 수 없으면 Usage=**UNKNOWN**. Resume는 §12 Preflight + §21 UNKNOWN 규칙(a)(b)를 따른다. 추정 %·날조·per-Bot 임의 계산 **금지**.

---

## 21. Project Usage Recovery Watch (Auto-Resume Routine) — 2026-09-05 04:28 KST 전문 갱신

| Field | Value |
|-------|-------|
| 소유 | **총괄 봇** 전역 Routine |
| 이름 | **Project Usage Recovery Watch** |
| id / folder | `project-usage-recovery-watch` (**유지**) |
| 주기 | `0 */3 * * *` (매 3시간 · 전일) |
| 목적 | USAGE-HALT 점검 · Safe Resume 판정 (개발 반복 아님) |

### 실행 절차

1. 열린 `USAGE-HALT-*` **없으면** → **무작업 종료(조용히)**.
2. **있으면 먼저 Cursor Dashboard Browser Monitor** (인증 시 Primary):
   - used % → remaining = 100 − used
   - failure → **UNKNOWN** (이전 값 재사용 금지)
3. **remaining ≥ 30%** 일 때만 Auto Resume 후보. 그 다음 **Resume Preflight**.
4. Preflight PASS · (필요 시) 최소 Restore/Build/Smoke · GitHub/SoT/PR/Build ID 정합 · Hard Block 아님 → **RESUME READY** → Bot 순서 **§14**.
5. Dashboard GREEN처럼 보여도 실제 quota 오류 → **INCONSISTENT/UNKNOWN**, **실제 실패 우선**, 재개 금지.
6. Routine 실행만으로 Usage 충분 **단정 금지**.
7. **Usage = UNKNOWN** 재개 조건:
   - (a) Handoff가 preflight-pass 시 auto-resume 허용으로 **명시**, 또는
   - (b) 사용자가 해당 정책으로 **이미 승인**
   - 아니면 Halt 유지 · 조용히
8. Usage 재부족 · Routine 제한 → 재개 시도 금지 · USAGE-HALT 유지.
9. `COMPLETED` 재실행 금지 / `SAFE CHECKPOINT`=Next / `INCOMPLETE`=수석·통합 검토.

---

## 22. Resume 성공 시 비서 기록

기존 해당 `USAGE-HALT-*` Continuity 문서에:

| Field | Value |
|-------|-------|
| Status | **RESUMED** |
| Resume timestamp | **KST** |
| Resume commit | tip SHA |
| Build ID | 해당 시 |
| Preflight result | PASS / 요약 |
| Usage at resume | Dashboard remaining 또는 UNKNOWN (추정 금지) |


---



---

## 20.8 / 21.x 운영 개정 포인터 (2026-09-05 13:35 KST) — 총괄 · 축약 본문 대체 금지 · 아래만 추가

| Topic | Rule |
|-------|------|
| **Usage Sentinel** | Usage 감시·등급·Halt 기록은 **전담(Usage Sentinel / 비서 Continuity)** · 장황 GREEN 로그 금지(상태 **전환**만) |
| **총괄** | **이벤트 기반** 조율(SendToAgent/회의실) · 상시 Usage 폴링 주체가 아님 |
| **ACTIVE 확인 주기** | **GREEN≥50% → 60분 · YELLOW 30–50% → 30분 · ORANGE 5–30% → 15분 · RED&lt;5% → Safe Stop**. HALT 중 ACTIVE 15분 polling **금지** · Recovery Watch **1–3h/2h** 유지 |
| **HALT 시 Recovery Watch** | `project-usage-recovery-watch` · HALT 중 **1–3h** 점검 리듬(기존 `0 */3 * * *`와 정합) |
| **Auto Resume** | **remaining ≥ 30%** + **Resume Preflight PASS** (§20.3·§21 유지) |
| **Dashboard** | Primary runtime signal while authenticated · API unavailable · failure→UNKNOWN · 이전 값 재사용 금지 |



### 임계값 개정 (2026-09-05 13:37 KST) — 총괄

- remaining: **≥50% GREEN/60분** · **30–50% YELLOW/30분** · **5–30% ORANGE/15분** · **&lt;5% RED Safe Stop**.
- 구 **10–29.99%/10분** ORANGE·구 **&lt;10% RED** **폐기**.
- **Auto-Resume:** remaining **≥30%** + Preflight PASS (hysteresis 유지).
- **HALT 중:** ACTIVE **15분 polling 금지** · Recovery Watch **1–3h / 2h** 유지.
- 현재 remaining **8%** = 새 정책 **ORANGE**이나 사용자 **MANUAL HALT** 유지(재개 금지) — `2026-09-05 13:37 KST` Continuity.

실측 Halt: Continuity `USAGE-HALT-20260905-001` — remaining **8% ORANGE** + **MANUAL HALT** (재개 금지) · 정책개정 2026-09-05 13:37 KST.

## 19. 개정

총괄 지시 + 비서 SoT 갱신(DEC ID·본 파일)으로만 개정. 채팅 한 줄로 조항 삭제·축소 금지.
