# Plugin Matrix — Desktop Media Player (전문)

| Field | Value |
|---

> **SoT lock (2026-09-05 04:14 KST, 총괄):** 본 파일(`PLUGIN_MATRIX.md` / DEC-PLUGIN-MATRIX-2026-09-05-001)이 **공식 Plugin 정책 SoT**다. Decision Log 포인터·채팅 요약·축약 표는 **참고만** 가능하며, **전문을 대체·축소·삭제하지 않는다.**

----|-------|
| Document ID | DEC-PLUGIN-MATRIX-2026-09-05-001 |
| Updated | 2026-09-06 13:20 KST |
| Authority | 총괄 봇 【SoT 필수 보완】 |
| Status | Active · 축약본으로 대체하지 말 것 |

본 문서는 플러그인/커넥터 **전체 운영 정책**이다. 채팅 요약·짧은 DEC 행만으로 본문을 대체하지 않는다.

---

## 0. 상위 규칙 (모든 역할·단계 공통)

1. **Gate / 역할 경계 > Plugin**  
   플러그인 출력·자동화·리뷰 봇이 Design Gate, Spike Gate, Build Gate, License Compliance Gate, 전문 Bot 역할, SoT를 덮어쓰지 않는다.

2. **공식 Source of Truth**  
   - 코드·커밋·PR·Build/SHA: GitHub `scarlel96-design/desktop-media-player`  
   - 프로젝트 상태·AD·KI·Handoff·Decision·Artifact: 비서 봇 SoT (`docs/` 트리)  
   - **Linear / Notion ≠ SoT** (티켓·노트·협업 참고만). Linear/Notion에만 있는 결정을 완료·Gate 통과로 기록하지 않는다.

3. **GitHub + SoT > Linear / Notion**  
   충돌 시 GitHub 커밋/PR과 비서 SoT가 우선한다.

4. **CodeRabbit autofix = 기본 OFF**  
   Finding ≠ 자동 FAIL. Gate FAIL은 총괄/검증 절차와 증거 기준을 따른다.

5. **Context7 ≠ Architecture**  
   Context7은 문서·API 참고용. Architecture Decision·모듈 경계는 수석/총괄 승인·SoT AD만 권위 있다.

6. **파괴적 Git 작업**  
   Force push, history rewrite, 보호 브랜치 강제 변경 등은 **사용자 명시 승인** 후.

7. **Sentry 외부 telemetry (현 Phase)**  
   **본격 사용 금지.** Privacy scrubbing · 미디어 경로/PII 정책 · **사용자 명시 승인** 전에는 외부로 보내지 않는다.  
   **로컬 계측 ≠ 외부 telemetry** (spike.log, 로컬 메트릭은 허용 범위에서 역할에 맞게 사용).

---

## 1. 역할별 스택 (전체)

### 총괄 봇
- GitHub (레포·PR·이슈 조율 가시성)
- Linear (작업 조율 참고 · ≠SoT)
- Docs Canvas / 필요 시 Notion (보고·요약 참고 · ≠SoT)
- SoT는 비서 기록·총괄 Gate 판정을 따름
- Plugin이 Gate 판정을 대신하지 않음

### 기획 봇
- **Linear** + **Docs Canvas** + **Notion**
- GitHub는 요구사항 추적용 참고 가능 · PRD/AC의 SoT 반영은 비서·총괄 절차
- Linear/Notion 문구만으로 MVP/AC “확정” 처리 금지 (SoT/총괄 승인 필요)

### 기술조사 봇
- **Context7** + **GitHub**
- Context7 조사 ≠ 최종 스택 확정 (수석/총괄 AD)
- 라이선스·배포 제약은 반드시 SoT/Decision에 올릴 수 있게 근거 명시

### 수석 엔지니어 봇
- GitHub (Architecture·Contracts·리뷰)
- Context7 (API/엔진 참고 · Architecture 대체 금지)
- Superpowers / Cursor Team Kit (설계·리뷰 보조 · Gate 대체 금지)
- AD·모듈 경계는 SoT Decision Log에 기록

### 구현 봇
- GitHub (커밋·PR)
- Context7 (API 참고)
- Superpowers / Cursor Team Kit (구현 보조)
- CodeRabbit = 기본 autofix OFF · finding은 참고
- Shell/Engine 경계·Facade 규칙은 수석 SoT가 우선

### 통합·디버그 봇
- GitHub
- Cursor Team Kit / Superpowers (RCA 보조)
- Linear (이슈 추적 참고 · ≠SoT)
- 로컬 로그·재현 증거 우선 · 외부 Sentry 금지(현 Phase)

### 빌드 봇
- GitHub
- 로컬/CI 빌드 도구
- Opsera = **제한적** · build/release 구조에 따라 Phase A 중후반 이후 검토
- SBOM·SHA·Build ID는 비서 SoT/Artifact Manifest에 전달
- 임의 prebuilt·인터넷 바이너리 금지 정책 유지

### 비서 봇
- SoT `docs/` (Project State, Decision Log, KI, Handoff, Manifest, Changelog)
- GitHub (커밋·Build 메타 기록)
- Linear/Notion 내용을 SoT로 **승격할 때만** 총괄 지시 하에 반영
- Plugin Matrix 본문 유지 책임 (축약 대체 금지)

### UI·UX 봇
- **Notion** + **Docs Canvas** + **GitHub 구현 결과 참고**
- UX 명세(SoT `docs/ux/`)가 구현 AC의 권위
- Blur ON 등 Design Gate/DEC에 반하는 플러그인 제안 채택 금지

### 성능 최적화 봇
- **GitHub**
- 규모 확대 시 **Sourcegraph** (제한적→확대는 단계 표 따름)
- **Beta 이후** 조건 충족 시 Sentry (현 Phase 외부 telemetry 금지)
- 로컬 계측·벤치 스크립트 우선

### 검증 봇 (QA)
- **GitHub** + **Cursor Team Kit** + **Linear** + **Superpowers**
- **Beta 이후** 필요 시 **Sentry** (승인·PII 정책 후)
- Finding ≠ 자동 Gate FAIL

### 검수 / Red Team (검수 봇)
- **GitHub** + **PR Review Canvas** + **CodeRabbit**
- 필요 시 **Sourcegraph** / **Opsera** (제한적)
- CodeRabbit autofix OFF · 보안/라이선스 Gate는 증거·총괄 절차

---

## 2. 단계별 조건 (원문 유지)

### 현재 — Step 0 / Phase A 초반 — 적극
- GitHub  
- Context7  
- Superpowers  
- Cursor Team Kit  
- Docs Canvas  
- Linear  

### 필요 시
- PR Review Canvas  
- CodeRabbit  
- Notion  

### 제한적
- Sourcegraph  
- Opsera  

### 본격 사용 금지 (현)
- Sentry **외부 telemetry**  
  - 전제: Privacy scrubbing · 미디어 경로/PII 정책 · **사용자 명시 승인**  
  - 로컬 계측 ≠ 외부 telemetry  

### Phase A 중후반
- 위 적극 세트 **+** PR Review Canvas, CodeRabbit, Sourcegraph **확대**  
- Opsera는 build/release 구조에 따라  

### Beta / RC 이후
- 조건·개인정보 검토 후 Sentry Crash/Perf  
- Opsera Release/Compliance  

---

## 3. Finding · Gate · SoT

| Rule | Meaning |
|------|---------|
| Finding ≠ 자동 FAIL | CodeRabbit/Sentry/스캐너 이슈만으로 Spike/Build/License FAIL 확정 금지 |
| Context7 ≠ Architecture | 조사 스니펫을 AD로 승격하려면 수석+총괄+SoT |
| Linear/Notion ≠ SoT | 티켓 완료 ≠ 제품 완료 |
| Autofix OFF | CodeRabbit 자동 수정 PR 기본 비활성 |
| Force push 등 | 사용자 승인 |

---

## 4. 개정

본 Matrix 개정은 총괄 지시 + 비서 SoT 갱신(DEC ID·본 파일)으로만 한다. 채팅 한 줄 요약으로 조항을 삭제·축소하지 않는다.

## Caveman Grok Bot Compatibility — FINAL (2026-09-06 12:20 KST)

| Item | Result |
|------|--------|
| Official Marketplace package | **NO** |
| Private Skill visible | **YES** |
| Slash invocation | **PARTIAL** |
| Official Agent Skill install | **PASS** (Named Bot auto-load **NOT PROVEN**) |
| Response compression | **PASS** (Pilot1) |
| Multi-turn persistence | **PASS** |
| New conversation reuse | **PASS (simulated)** (same-thread caveat) |
| Second Bot reuse | **PASS** (per-Bot Enable) |
| Code / Commands / Errors / Critical warnings | **PASS** |
| Official or Ported | **PORTED** |
| Proxy | **UNPROVEN** |
| Usage saving | **MEASURED_INCONCLUSIVE / NOT PROVEN** |
| Quality regression | **NONE** (pilot) |
| Rollout | **LIMITED** — Sentinel Full/Ultra 후보 · 총괄 Lite · 비서 대화만 Lite · SoT/Handoff/증거 비압축 · QA/Arch/Build/PERF 보류 |
| Bulk 13-Bot | **금지** |

Continuity: `docs/continuity/CAVEMAN-GROK-COMPAT-RESULT-2026-09-06.md`. Phase A Resume Point tip `ff00609` S8 Soft — Caveman 선점 없음.

### LIMITED ops (2026-09-06 12:22 KST)

일반 대화·상태만 Lite · SoT/Handoff/증거 **비압축**. Usage-saving A/B: `docs/continuity/CAVEMAN-USAGE-SAVING-AB-SHEET.md`. tip `ff00609` 지연 금지.

Amendment (2026-09-06 12:24 KST): New conversation reuse PASS (simulated). Roles not expanded.

### 총괄 판정 FINAL (2026-09-06 12:29 KST)

USAGE_SAVING=**MEASURED_INCONCLUSIVE**. chars↓ MEASURED · 절감 NOT PROVEN · PASS 금지 · LIMITED.

## 5. Profile Revision (2026-09-06 12:42 KST)

**사실:** 전문 Bot Profile이 도구·우선순위·Caveman Lite·Evidence 규칙을 명시하도록 개정됨.

| Rule | Meaning |
|------|---------|
| 배분 기준 | **최신 Profile** + 본 Matrix + Continuity `PROFILE-REVISION-2026-09-06.md` |
| 금지 | 오래된 Profile 기억으로 도구/역할 배분 |
| Priority | 요구 → PRD/Arch/Gate → SoT → 역할 → Plugin/Skill |
| Caveman | Lite 대화만 · SoT/Evidence 비압축 · 확대 금지 |
| Phase A | tip **`ff8ccbb`** 본선 유지 |

전문 축소 금지. 상세 Continuity 한 장: `docs/continuity/PROFILE-REVISION-2026-09-06.md`

### Caveman LIMITED accumulation (2026-09-06 12:48 KST)

Phase A 미중단 실전 누적 · 확대 금지 · 억지 PASS 금지. `docs/continuity/CAVEMAN-LIMITED-ACCUMULATION.md`

### Named Bot invocation (2026-09-06 13:20 KST)

Operations: `docs/continuity/NAMED-BOT-INVOCATION-RULES.md` · Atomic Chain = Named Bot `@` 호출 체인.
