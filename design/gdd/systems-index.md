# Systems Index: [TBD]

> **Status**: Updated
> **Created**: 2026-03-26
> **Last Updated**: 2026-04-17
> **Source Concept**: design/gdd/game-concept.md

---

## Overview

[TBD]는 귀엽고 밝은 중세판타지 세계관의 3D 뱀파이어 서바이벌라이크다.
플레이어는 검사·마법사·거너 중 하나를 선택해 3개 스테이지(3분/5분/7분)를 생존한다.
코어 루프는 "이동·회피·점프 → 자동공격 → 적 처치 → 경험치·골드 획득 → 무기 획득·강화 → 반복"으로 구성된다.
MVP(1-2주)는 검사 1캐릭터 + Stage 1 로직 완성을 목표로 한다.

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | 오빗 카메라 | Core | MVP 🔴 | Designed | design/gdd/camera-system.md | 없음 |
| 2 | 게임 상태 관리 | Core | MVP 🔴 | Designed | design/gdd/game-state-manager.md | 없음 |
| 3 | 체력 시스템 | Core | MVP 🔴 | Designed | design/gdd/health-system.md | 없음 |
| 4 | 오브젝트 풀링 | Core | MVP 🟢 | Designed | design/gdd/object-pooling.md | 없음 |
| 5 | 플레이어 이동+점프 | Core | MVP 🔴 | Designed | design/gdd/player-movement.md | 오빗 카메라, 게임 상태 관리 |
| 6 | 데미지 시스템 | Gameplay | MVP 🔴 | Designed | design/gdd/damage-system.md | 체력 시스템 |
| 7 | 기본 자동 공격 | Gameplay | MVP 🔴 | Designed | design/gdd/basic-attack.md | 데미지 시스템 |
| 8 | 적 AI | Gameplay | MVP 🔴 | Designed | design/gdd/enemy-ai.md | 체력 시스템, 플레이어 이동 |
| 9 | 웨이브 스폰 | Gameplay | MVP 🔴 | Designed | design/gdd/wave-spawner.md | 적 AI, 오브젝트 풀링 |
| 10 | 경험치 시스템 | Progression | MVP 🔴 | Designed | design/gdd/xp-system.md | 적 AI |
| 11 | 골드 시스템 | Progression | MVP 🟡 | 신규 필요 | — | 적 AI |
| 12 | 무기 획득 시스템 | Gameplay | MVP 🔴 | Designed | design/gdd/weapon-system.md | 데미지 시스템 |
| 13 | 스펙 시스템 | Progression | MVP 🟡 | Designed | design/gdd/spec-system.md | 무기 획득 시스템 |
| 14 | 레벨업·상자 선택 | Progression | MVP 🔴 | Designed | design/gdd/levelup-selection.md | 경험치 시스템, 무기 획득, 스펙 |
| 15 | 등급 시스템 | Progression | MVP 🟡 | 신규 필요 | — | 무기·스펙 시스템 |
| 16 | 선택 UI (레벨업·상자) | UI | MVP 🟡 | Designed | design/gdd/weapon-selection-ui.md | 무기 획득, 레벨업 선택 |
| 17 | 바닥 드롭 아이템 | Gameplay | MVP 🟡 | Designed | design/gdd/drop-items.md | 적 AI |
| 18 | HUD 시스템 | UI | MVP 🟡 | Designed | design/gdd/hud-system.md | 체력, XP, 무기, 골드, 게임 상태 |
| 19 | 피격 피드백 | UI | MVP 🟡 | 신규 필요 | — | 데미지 시스템 |
| 20 | 상자 기본 | Gameplay | MVP 🟡 | 신규 필요 | — | 골드, 등급 시스템 |
| 19 | 캐릭터 선택 | UI | Vertical 🔵 | 신규 필요 | — | 게임 상태 관리 |
| 20 | 상인 시스템 | Gameplay | Vertical 🔵 | 신규 필요 | — | 골드, 등급 시스템 |
| 21 | 스테이지 진행 | Core | Vertical 🔵 | 신규 필요 | — | 게임 상태, 웨이브 스폰 |
| 22 | 보스 몬스터 | Gameplay | Vertical 🔵 | 신규 필요 | — | 적 AI, 체력 시스템 |
| 23 | 랭킹 시스템 | Meta | Alpha 🟣 | 신규 필요 | — | 게임 상태 관리 |
| 24 | 외부 재화·해금 | Meta | Alpha 🟣 | 신규 필요 | — | 랭킹, 게임 상태 |

---

## Categories

| Category | Description |
|----------|-------------|
| **Core** | 모든 것의 기반 — 카메라, 상태 관리, 오브젝트 풀링 |
| **Gameplay** | 게임을 재미있게 만드는 시스템 — 전투, AI, 이동 |
| **Progression** | 플레이어 성장 — XP, 레벨업, 골드, 무기 선택 |
| **UI** | 플레이어 정보 표시 — HUD, 무기 선택, 피격 피드백 |
| **Meta** | 런 간 영구 진행 — 랭킹, 해금 |

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
| 총 시스템 수 | 24 |
| MVP 시스템 수 | 18 |
| Vertical Slice 시스템 수 | 5 |
| Alpha 시스템 수 | 2 |
| 설계 문서 있음 | 16 / 24 |

---

## Next Steps

- [ ] `CameraController.cs` — 오빗 카메라 구현
- [ ] `PlayerController.cs` — 점프·낙하 데미지 추가, 오빗 카메라 기준 이동
- [ ] `WeaponSystem.cs` — 무기 획득·자동 발동 구현
- [ ] `LevelupWeaponSelection.cs` — 레벨업 무기 선택 구현
- [ ] 골드 시스템 GDD 작성
- [ ] 등급 시스템 GDD 작성
