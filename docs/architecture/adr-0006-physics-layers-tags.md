# ADR-0006: Physics Layer, Tag, Script Execution Order

## Status
Accepted

## Date
2026-04-13

## Context

### Problem Statement
플레이어·적·투사체 간 충돌 처리, 오브젝트 식별, 싱글턴 초기화 순서를
명확하게 정의하지 않으면 Physics 연산 낭비, NullReference, 자해 판정 등이 발생한다.

## Decision

### Physics Layer (8~14번)

| 번호 | 이름 | 용도 |
|------|------|------|
| 8 | Player | 플레이어 캐릭터 본체 (CharacterController) |
| 9 | Enemy | 근접/원거리 적 본체 |
| 10 | PlayerProjectile | 플레이어가 발사하는 투사체 |
| 11 | EnemyProjectile | 적이 발사하는 투사체 |
| 12 | Pickup | XP 오브, 골드 드롭 |
| 13 | Environment | 맵 지형, 장애물, 벽 |
| 14 | HitBox | 근접 공격 판정 트리거 (PhysX 매트릭스 전체 비활성, OnTriggerEnter로만 처리) |

### 충돌 매트릭스

O = 충돌 활성, X = 충돌 비활성

|  | Player | Enemy | PlayerProj | EnemyProj | Pickup | Environ | HitBox |
|--|:------:|:-----:|:----------:|:---------:|:------:|:-------:|:------:|
| **Player** | X | O | X | O | O | O | X |
| **Enemy** | O | X | O | X | X | O | X |
| **PlayerProj** | X | O | X | X | X | O | X |
| **EnemyProj** | O | X | X | X | X | O | X |
| **Pickup** | O | X | X | X | X | X | X |
| **Environ** | O | O | O | O | X | X | X |
| **HitBox** | X | X | X | X | X | X | X |

**주요 판단 근거:**
- Enemy↔Enemy 비활성: 50+ 동시 처리 시 PhysX 오버헤드 감소, 군집 분리는 AI Steering으로 처리
- 자기 진영 투사체 자해 방지: PlayerProjectile↔Player, EnemyProjectile↔Enemy 비활성
- HitBox 전체 비활성: OnTriggerEnter / Physics.OverlapBox로만 판정

### Tag 규칙

| 태그 | 용도 | 사용처 |
|------|------|--------|
| `Player` | 플레이어 오브젝트 | `CameraController.Awake` |
| `Enemy` | 적 오브젝트 | 투사체 히트 판정, 범위 스킬 타게팅 |
| `Pickup` | XP/골드 드롭 | 픽업 트리거 감지 |
| `Projectile` | 투사체 | 오브젝트 풀 반환 식별 |
| `SpawnPoint` | 웨이브 스폰 위치 마커 | `WaveSpawner.Awake`에서 일괄 수집 |

**원칙:** Layer는 Physics 연산에, Tag는 런타임 오브젝트 식별에 사용. 역할 중복이나 혼용 금지.

**FindWithTag 사용 제한:** 단일 오브젝트 조회(`FindWithTag("Player")`)는 Awake 1회만 허용.
적·투사체처럼 다수 생성되는 오브젝트에 대한 `FindWithTag` 반복 호출은 금지.

### Script Execution Order

| 값 | 스크립트 | 이유 |
|----|----------|------|
| -200 | `GameManager` | 모든 싱글턴의 기준. 가장 먼저 Instance 설정 |
| -150 | `XPSystem`, `GoldSystem`, `SkillSystem`, `ObjectPool` | GameManager 이후, 게임플레이 스크립트 이전 |
| -100 | `CameraController` | PlayerController.Awake보다 먼저 완료돼야 함 |
| -50 | `PlayerController` | CameraController 참조를 Awake에서 획득 |
| 0 | 나머지 | 기본값 |

숫자가 작을수록 먼저 실행. `Edit > Project Settings > Script Execution Order`에서 설정.
`ProjectSettings/ProjectSettings.asset`에 저장 — Git 커밋 필수.

## 적용 체크리스트

- [ ] `Edit > Project Settings > Tags and Layers` — 레이어 8~14 이름 입력
- [ ] `Edit > Project Settings > Tags and Layers > Tags` — Enemy, Pickup, Projectile, SpawnPoint 추가
- [ ] `Edit > Project Settings > Physics > Layer Collision Matrix` — 매트릭스 설정
- [ ] `Edit > Project Settings > Script Execution Order` — 5개 값 입력
- [ ] `ProjectSettings/ProjectSettings.asset` Git 커밋

## Related Decisions
- ADR-0001: 싱글턴 사용 기준
- ADR-0004: 컴포넌트 참조 방식
