# WINDOWS VALIDATION POLICY (사용자 지시 2026-10-03 03:18 KST, 총괄 전달 전문) — SoT / Decision Log 등재

출처: 총괄 → 비서 기록 요청(03:18 사용자 지시). 압축 없이 전문 기록. 이전의 "변경마다 직렬 Windows 확인" 방식을 정정하는 정책.

## 정책 전문
(1) CRITICAL / HIGH-RISK (재생 불가, Seek 오작동, Crash/Freeze, A/V Sync, Frame Drop, 영상 출력, Track 전환 실패, 핵심 기능 Regression): 가능한 한 빨리 Windows 실기 검증.
(2) 관련 사용자-visible 변경 (같은 Control/Flow, Timeline/Seek, Button/OSD/Playback feedback, 낮은 위험 polish): 논리적으로 연관된 2~5개를 하나의 Validation Batch로 묶고 사용자에게 한 번에 Windows Validation Snapshot 제공.
(3) LOG-ONLY / DIAGNOSTIC: 최대 3개 묶음.
CONTINUE UNTIL REAL BLOCK 유지.
기본 흐름: 개발 → 내부 Build/Test/Review → 관련 변경 Batch → Windows Validation → 결과 반영.
사용자 테스트가 필요할 때는 'USER WINDOWS TEST REQUIRED' 형식으로 다음을 함께 제공한다: Build ID, Commit, Artifact, SHA-256, Test Scope, Repro Scenario, Expected Behavior, Failure Evidence/Log Path, PASS/FAIL Criteria.
목적: 검증 없는 Soft 누적 방지. 사용자를 승인 병목으로 만드는 것이 아님.

## 함께 등재할 맥락 (총괄 전달)
- 투자자 봇 REVIEW #1 회신 맥락: 신규 편의 Atomic 동결, 문서 5종. (비서는 REVIEW #1 원문을 수신하지 못함 — 문서 5종의 구체 목록은 GAP, 총괄 전달 시 추가 기록.)

## 비서 메모 (사실만)
- 이 정책은 Soft≠Windows PASS, Gate, 사용량 규칙, Design Gate, 라이선스 LEVEL 1을 해제하지 않는다.
- 'PASS/FAIL Criteria'는 USER WINDOWS TEST REQUIRED 패키지에 포함되는 항목이며, 기준 없는 PASS 기록은 계속 금지.
- 현재 미검증 누적(Windows NOT TESTED): S58 Right-clamp, S60~S66 OSD/playlist/selection, S67 seek_latency 사용자 seek, S68 end_file EOF/error, S69 mpv_event g, S70 c/d/e/EOF/error, S71 중 로그 증거 없는 항목(진술만). 이 정책 하에서 Batch 구성은 총괄 판단.

> 03:48 총괄 확정: 03:45 지시가 03:18 '배치 Windows 스냅샷 요청'을 대체(WINDOWS-VALIDATION-POLICY-20261003-0318.md 해당 부분 SUPERSEDED, 문서 보존).
