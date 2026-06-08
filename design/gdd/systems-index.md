# Systems Index: BladeSurge (Bonk)

> **Status**: MVP 18 시스템 구현 완료 — 자산 통합/폴리싱 단계
> **Created**: 2026-03-26
> **Last Updated**: 2026-06-09 (신규 시스템 GDD 7종 작성: 스테이지 진행/보스/랭킹/온라인/캐릭터선택/미니맵/컷신)
> **Source Concept**: design/gdd/game-concept.md

---

## Overview

BladeSurge(Bonk)는 귀엽고 밝은 중세판타지 세계관의 3D 뱀파이어 서바이벌라이크다.
플레이어는 검사·마법사·거너 중 하나를 선택해 3개 스테이지(3분/5분/7분)를 생존한다.
코어 루프는 "이동·회피·점프 → 자동공격 → 적 처치 → 경험치·골드 획득 → 무기 획득·강화 → 반복"으로 구성된다.
MVP는 검사 1캐릭터 + Stage 1 로직 완성을 목표로 했고, 2026-05-02 기준 18 MVP 시스템 모두 구현 완료.

---

## Status 범례

- **Designed** — 설계 문서만 존재
- **Implemented** — 코드 + 설계 문서 모두 존재
- **Implemented (no doc)** — 코드만 존재, 설계 문서 미작성
- **신규 필요** — 코드/문서 둘 다 없음

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Code | Depends On |
|---|-------------|----------|----------|--------|------------|------|------------|
| 1 | 오빗 카메라 | Core | MVP 🔴 | Implemented | design/gdd/camera-system.md | CameraController.cs | 없음 |
| 2 | 게임 상태 관리 | Core | MVP 🔴 | Implemented | design/gdd/game-state-manager.md | GameManager.cs | 없음 |
| 3 | 체력 시스템 | Core | MVP 🔴 | Implemented | design/gdd/health-system.md | HealthComponent.cs | 없음 |
| 4 | 오브젝트 풀링 | Core | MVP 🟢 | Implemented | design/gdd/object-pooling.md | ObjectPool.cs, PickupPool.cs | 없음 |
| 5 | 플레이어 이동+점프 | Core | MVP 🔴 | Implemented | design/gdd/player-movement.md | PlayerController.cs | 오빗 카메라, 게임 상태 관리 |
| 6 | 데미지 시스템 | Gameplay | MVP 🔴 | Implemented | design/gdd/damage-system.md | DamageDealer.cs | 체력 시스템 |
| 7 | 기본 자동 공격 | Gameplay | MVP 🔴 | Implemented | design/gdd/basic-attack.md | BasicAttack.cs | 데미지 시스템 |
| 8 | 적 AI | Gameplay | MVP 🔴 | Implemented | design/gdd/enemy-ai.md | EnemyBase.cs, ChaserAI.cs, RusherAI.cs | 체력 시스템, 플레이어 이동 |
| 9 | 웨이브 스폰 | Gameplay | MVP 🔴 | Implemented | design/gdd/wave-spawner.md | WaveSpawner.cs | 적 AI, 오브젝트 풀링 |
| 10 | 경험치 시스템 | Progression | MVP 🔴 | Implemented | design/gdd/xp-system.md | XPSystem.cs, XPOrb.cs | 적 AI |
| 11 | 골드 시스템 | Progression | MVP 🟡 | Implemented | design/gdd/gold-system.md | GoldSystem.cs, GoldOrb.cs | 적 AI |
| 12 | 무기 획득 시스템 | Gameplay | MVP 🔴 | Implemented | design/gdd/weapon-system.md | WeaponSystem.cs, WeaponSlot.cs, WeaponDataSO.cs, SwordAttack/GunAttack/MagicAttack.cs, Projectile.cs | 데미지 시스템 |
| 13 | 스펙 시스템 | Progression | MVP 🟡 | Implemented | design/gdd/spec-system.md | PlayerStats.cs, PlayerStatsBinder.cs | 무기 획득 시스템 |
| 14 | 레벨업·상자 선택 | Progression | MVP 🔴 | Implemented | design/gdd/levelup-selection.md | LevelupWeaponSelection.cs | 경험치 시스템, 무기 획득, 스펙 |
| 15 | 등급 시스템 | Progression | MVP 🟡 | Implemented | design/gdd/grade-system.md | CardGrade.cs, CardGradeRoller.cs | 무기·스펙 시스템 |
| 16 | 선택 UI (레벨업·상자) | UI | MVP 🟡 | Implemented | design/gdd/weapon-selection-ui.md | WeaponSelectionUI.cs | 무기 획득, 레벨업 선택 |
| 17 | 바닥 드롭 아이템 | Gameplay | MVP 🟡 | Implemented | design/gdd/drop-items.md | DropItem.cs, DropItemSpawner.cs, DropItemEffects.cs | 적 AI |
| 18 | HUD 시스템 | UI | MVP 🟡 | Implemented | design/gdd/hud-system.md | HpBarUI/XpBarUI/TopBarUI/DashCooldownUI/WeaponSlotsHUD/PassiveSlotsHUD/StatsPanelUI/ChestPromptUI.cs | 체력, XP, 무기, 골드, 게임 상태 |
| 19 | 피격 피드백 | UI | MVP 🟡 | Implemented | design/gdd/hit-feedback.md | HitFlash.cs, IHitFeedback.cs | 데미지 시스템 |
| 20 | 상자 기본 | Gameplay | MVP 🟡 | Implemented | design/gdd/chest-system.md | ChestSystem.cs, ChestSpawner.cs, Chest.cs | 골드, 등급 시스템 |
| 21 | 캐릭터 선택 | UI | Vertical 🔵 | Implemented | design/gdd/character-select.md | CharacterSelectController.cs, CharacterSelectPreview.cs, MapSelectController.cs, GameSession.cs | 게임 상태 관리, 스테이지 진행 |
| 22 | 상인 시스템 | Gameplay | Vertical 🔵 | 신규 필요 | — | — | 골드, 등급 시스템 |
| 23 | 스테이지 진행 | Core | Vertical 🔵 | Implemented | design/gdd/stage-progression.md | StageGate.cs, StageProgress.cs, StageDifficulty.cs, ArenaPhaseManager.cs, ArenaEncounterTrigger.cs, DebugStageSelector.cs | 게임 상태, 웨이브 스폰, 보스 몬스터, 랭킹, 미니맵 |
| 24 | 보스 몬스터 | Gameplay | Vertical 🔵 | Implemented | design/gdd/boss-monster.md | BossEnemy.cs, BossAI.cs, BossAttackBase.cs, BossAttackOrchestrator.cs, GroundSlamAttack/AreaBarrageAttack/SummonAttack/ChargeAttack.cs, TelegraphIndicator.cs, ImpactBurst.cs, BossHpBarUI.cs, BossBanner.cs | 적 AI, 체력 시스템, 웨이브 스폰, 미니맵 |
| 25 | 랭킹 시스템 | Meta | Alpha 🟣 | Implemented | design/gdd/ranking-scoring.md | RunStats.cs, RunTotals.cs, GameClearUI.cs | 게임 상태 관리, 스테이지 진행 |
| 26 | 외부 재화·해금 | Meta | Alpha 🟣 | 부분 (해금만) | design/gdd/stage-progression.md (해금) | StageProgress.cs | 랭킹, 게임 상태 — 외부 재화는 미구현 |
| 32 | 온라인 리더보드 | Infra | Alpha 🟣 | Implemented | design/gdd/online-leaderboard.md | OnlineLeaderboard.cs, LeaderboardPanelUI.cs | 랭킹 시스템 (점수), 게임 상태 |
| 33 | 미니맵 | UI | Alpha 🟣 | Implemented | design/gdd/minimap.md | MinimapManager.cs, MinimapTracker.cs, MinimapObjective.cs | 스테이지 진행, 보스, 적 AI |
| 34 | 인트로 컷신 | UI | Vertical 🔵 | Implemented | design/gdd/intro-cutscene.md | IntroCutscene.cs | 게임 상태 관리, 스테이지 진행 |
| 27 | 결과 화면 (GameOver/Win) | UI | MVP 🔴 | Implemented (no doc) | — | GameOverUI.cs, RunStats.cs | 게임 상태 관리, 경험치/골드, 적 AI |
| 28 | 메인 메뉴 | UI | MVP 🔴 | In Progress | — | MainMenuUI.cs | 게임 상태 관리 |
| 29 | 옵션 영속화 | Infra | MVP 🟡 | Implemented (no doc) | — | SettingsService.cs | 없음 (PlayerPrefs 래퍼) |
| 30 | 오디오 시스템 | Infra | MVP 🔴 | In Progress | — | AudioManager.cs | 옵션 영속화 |
| 31 | Editor 자동화 | Editor 🛠 | — | Implemented (no doc) | — | PlayModeStartScene.cs | 없음 (개발 편의) |

---

## Categories

| Category | Description |
|----------|-------------|
| **Core** | 모든 것의 기반 — 카메라, 상태 관리, 오브젝트 풀링 |
| **Gameplay** | 게임을 재미있게 만드는 시스템 — 전투, AI, 이동 |
| **Progression** | 플레이어 성장 — XP, 레벨업, 골드, 무기 선택 |
| **UI** | 플레이어 정보 표시 — HUD, 무기 선택, 피격 피드백, 화면 흐름 |
| **Meta** | 런 간 영구 진행 — 랭킹, 해금 |
| **Infra** | 게임 외 영속/외부 연결 — 옵션 저장, 오디오 채널 |
| **Editor** 🛠 | 개발자 편의 도구 — 빌드 설정, Play 자동화 |

---

## Priority Tiers

| Tier | Definition | 목표 타임라인 |
|------|------------|--------------|
| **MVP 🔴 Critical** | 없으면 게임이 동작하지 않음 | MVP |
| **MVP 🟡 Minimal** | 필요하지만 단순하게 구현 가능 | MVP |
| **MVP 🟢 Defer OK** | 성능 문제 시 추가 | 필요 시 |
| **Vertical 🔵** | Stage 2-3, 다캐릭터, 상점 | Vertical Slice |
| **Alpha 🟣** | 랭킹, 해금, 미니맵 | Alpha |

---

## Dependency Map

### Foundation Layer (의존성 없음)

1. **오빗 카메라** — 마우스 회전 기준축. 모든 이동의 기준
2. **게임 상태 관리** — 시작/플레이/게임오버/스테이지클리어 전환
3. **체력 시스템** — 플레이어·적 체력 데이터 컨테이너
4. **오브젝트 풀링** — 50+ 적 처리 인프라

### Core Layer (Foundation에 의존)

1. **플레이어 이동+점프** — depends on: 오빗 카메라, 게임 상태 관리
2. **데미지 시스템** — depends on: 체력 시스템
3. **적 AI** — depends on: 체력 시스템, 플레이어 이동
4. **웨이브 스폰** — depends on: 적 AI, 오브젝트 풀링
5. **경험치 시스템** — depends on: 적 AI (킬 이벤트)
6. **골드 시스템** — depends on: 적 AI (킬 이벤트)

### Feature Layer (Core에 의존)

1. **기본 자동 공격** — depends on: 데미지 시스템, 플레이어 이동
2. **무기 획득 시스템** — depends on: 데미지 시스템
3. **등급 시스템** — depends on: 무기 획득 시스템
4. **레벨업 무기 선택** — depends on: 경험치 시스템, 무기 획득 시스템
5. **피격 피드백** — depends on: 데미지 시스템

### Presentation Layer (Feature에 의존)

1. **무기 선택 UI** — depends on: 무기 획득 시스템, 레벨업 무기 선택
2. **HUD 시스템** — depends on: 체력, XP, 무기, 게임 상태

### Vertical Slice Layer

1. **캐릭터 선택** — depends on: 게임 상태 관리
2. **상자 시스템** — depends on: 골드, 등급 시스템
3. **상인 시스템** — depends on: 골드, 등급 시스템
4. **스테이지 진행** — depends on: 게임 상태, 웨이브 스폰
5. **보스 몬스터** — depends on: 적 AI, 체력 시스템

---

## Recommended Implementation Order

| 순서 | 시스템 | 우선순위 | 비고 |
|------|--------|----------|------|
| 1 | 오빗 카메라 | MVP 🔴 | CameraController.cs |
| 2 | 게임 상태 관리 | MVP 🔴 | GameManager.cs |
| 3 | 체력 시스템 | MVP 🔴 | HealthComponent.cs |
| 4 | 플레이어 이동+점프 | MVP 🔴 | PlayerController.cs |
| 5 | 데미지 시스템 | MVP 🔴 | DamageDealer.cs |
| 6 | 기본 자동 공격 | MVP 🔴 | BasicAttack.cs |
| 7 | 적 AI | MVP 🔴 | ChaserAI.cs, RusherAI.cs |
| 8 | 웨이브 스폰 | MVP 🔴 | WaveSpawner.cs |
| 9 | 오브젝트 풀링 | MVP 🟢 | ObjectPool.cs |
| 10 | 경험치 시스템 | MVP 🔴 | XPSystem.cs |
| 11 | 골드 시스템 | MVP 🟡 | GoldSystem.cs |
| 12 | 무기 획득 시스템 | MVP 🔴 | WeaponSystem.cs |
| 13 | 등급 시스템 | MVP 🟡 | RaritySystem.cs |
| 14 | 레벨업 무기 선택 | MVP 🔴 | LevelupWeaponSelection.cs |
| 15 | 피격 피드백 | MVP 🟡 | HitFlash.cs ✅ |
| 16 | 무기 선택 UI | MVP 🟡 | WeaponSelectionUI.cs |
| 17 | HUD 시스템 | MVP 🟡 | HUDController.cs |
| 18 | 상자 기본 | MVP 🟡 | ChestInteraction.cs |

---

## High-Risk Systems

| 시스템 | 리스크 | 대응 |
|--------|--------|------|
| 오빗 카메라 | 이동 방향·카메라 회전 조합 구현 복잡도 | **최우선 구현** — 나머지 시스템의 기준축 |
| 적 AI (50+) | 동시 처리 성능 | Object Pooling + 단순 벡터 추적으로 시작 |
| 무기 획득 시스템 | 다수 무기 동시 발동 성능 | 무기당 타이머 단순화, 과도한 물리 판정 지양 |

---

## Progress Tracker

| 지표 | 수치 |
|------|------|
| 총 시스템 수 | 31 |
| MVP 시스템 수 (게임 시스템) | 20 |
| MVP 시스템 수 (UI/Infra 추가) | +4 |
| Vertical Slice 시스템 수 | 4 |
| Alpha 시스템 수 | 2 |
| Editor 도구 | 1 |
| Implemented (코드+문서) | 20 / 31 |
| Implemented (no doc) | 3 / 31 (GameOver UI / SettingsService / PlayModeStartScene) |
| In Progress | 2 / 31 (MainMenu / AudioManager — 인프라/골격만) |
| 게임 시스템 MVP 구현 완료 | 20 / 20 ✅ |

---

## Next Steps

### 문서 보완 (코드는 있으나 GDD 미작성)
- [x] 골드 시스템 GDD 작성 (`design/gdd/gold-system.md`) — 2026-05-08
- [x] 등급 시스템 GDD 작성 (`design/gdd/grade-system.md`) — 2026-05-08
- [x] 피격 피드백 GDD 작성 (`design/gdd/hit-feedback.md`) — 2026-05-08
- [ ] 결과 화면 GDD 작성 (`design/gdd/results-screen.md`) — RunStats + GameOverUI 통합
- [ ] 옵션 영속화 GDD 작성 (`design/gdd/settings-service.md`) — PlayerPrefs 키 정의 + 기본값 + UI 슬라이더 매핑
- [ ] 오디오 시스템 GDD 작성 (`design/gdd/audio-system.md`) — 채널 구조 + 카탈로그 SO 설계 + AudioMixer 도입 시점

### Vertical Slice 단계 진입 시
- [ ] 캐릭터 선택 시스템 — 시작 캐릭터(검사/거너/마법사) 분기
- [ ] 보스 몬스터 시스템 — 모델 + AI + 등장 트리거 + 체력바
- [ ] 스테이지 진행 시스템 — Stage 1/2/3 전환

### MVP 폴리싱 (구현된 시스템 대상)
- [ ] UI 시각 디자인 — `production/remaining-work.md` 1장 참조
- [ ] 사운드 통합 — 9 카테고리 (`production/remaining-work.md` 3장)
- [ ] VFX 통합 — 5 카테고리 (`production/remaining-work.md` 4장)
- [ ] 자산 통합 — 맵·캐릭터·보스 모델링 (`production/remaining-work.md` 2장)
