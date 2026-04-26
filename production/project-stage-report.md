# Project Stage Analysis

**Date**: 2026-03-30
**Stage**: Pre-Production

---

## Completeness Overview

| 영역 | 상태 | 세부 내용 |
|------|------|-----------|
| **디자인** | 58% | 16개 doc (game-concept, systems-index, 14개 GDD), 10개 시스템 설계 미완 |
| **소스 코드** | 17% | 게임 스크립트 4개 (PlayerController, CameraController, GameManager, InputSystem_Actions) |
| **아키텍처** | 0% | ADR 없음, docs/architecture/ 디렉토리 없음 |
| **프로덕션** | 40% | Sprint-01 존재하나 2026-03-28 종료 — 스프린트 결산 미완 |
| **테스트** | 0% | 테스트 파일 없음 |

---

## Gaps Identified

1. **Sprint-01 결산 미완** — 스프린트 종료일(3/28) 이후 회고 없음. systems-index "구현 완료: 0/24"이나 실제 src/에 PlayerController, CameraController, GameManager 존재. 실제 완료 항목 불일치.
2. **ADR 없음** — docs/architecture/ 디렉토리 자체 없음. 코드가 시작됐음에도 아키텍처 결정 기록 없음.
3. **10개 시스템 설계 문서 없음** — 골드 시스템, 등급 시스템, 캐릭터 선택, 상자/상인, 스테이지 진행, 보스, 피격 피드백, 랭킹, 외부 해금.
4. **테스트 없음** — Unity Test Framework 미설정. 코딩 표준상 게임플레이 시스템 테스트 필요.
5. **프로토타입 연결 불명확** — prototypes/player-controller/ 결과가 src/ PlayerController.cs에 반영됐는지 불명확.

---

## Recommended Next Steps

1. `/retrospective` — Sprint-01 회고 작성, systems-index 완료 수 업데이트
2. `/sprint-plan` — Sprint-02 계획 수립 (3/30~다음 마일스톤)
3. `camera-system.md` + `player-movement.md` 업데이트 — systems-index "수정 필요" 항목 2개 반영
4. 나머지 MVP 시스템 구현 — EnemyAI, WaveSpawner, XPSystem, SkillSystem 등
5. `/architecture-decision` — 핵심 아키텍처 결정 ADR 작성 시작

---

## Design Documents Status

| # | 시스템 | 설계 문서 | 구현 상태 |
|---|--------|-----------|----------|
| 1 | 오빗 카메라 | camera-system.md (수정 필요) | 부분 (CameraController.cs) |
| 2 | 게임 상태 관리 | game-state-manager.md | 부분 (GameManager.cs) |
| 3 | 체력 시스템 | health-system.md | 미구현 |
| 4 | 오브젝트 풀링 | object-pooling.md | 미구현 |
| 5 | 플레이어 이동+점프 | player-movement.md (수정 필요) | 부분 (PlayerController.cs) |
| 6 | 데미지 시스템 | damage-system.md | 미구현 |
| 7 | 자동 공격 | basic-attack.md | 미구현 |
| 8 | 적 AI | enemy-ai.md | 미구현 |
| 9 | 웨이브 스폰 | wave-spawner.md | 미구현 |
| 10 | 경험치 시스템 | xp-system.md | 미구현 |
| 11 | 골드 시스템 | 없음 | 미구현 |
| 12 | 스킬 시스템 | skill-system.md | 미구현 |
| 13 | 레벨업 선택 | levelup-skill-selection.md | 미구현 |
| 14 | 등급 시스템 | 없음 | 미구현 |
| 15 | 스킬 선택 UI | skill-selection-ui.md | 미구현 |
| 16 | HUD 시스템 | hud-system.md | 미구현 |
| 17 | 캐릭터 선택 | 없음 | 미구현 |
| 18 | 상자 시스템 | 없음 | 미구현 |
| 19 | 상인 시스템 | 없음 | 미구현 |
| 20 | 스테이지 진행 | 없음 | 미구현 |
| 21 | 보스 몬스터 | 없음 | 미구현 |
| 22 | 피격 피드백 | 없음 | 미구현 |
| 23 | 랭킹 시스템 | 없음 | 미구현 |
| 24 | 외부 재화·해금 | 없음 | 미구현 |
