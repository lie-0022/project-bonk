# ADR-0007: 게임 시스템 공통 인터페이스

## Status
Accepted

## Date
2026-04-13

## Context

### Problem Statement
ObjectPool, DamageDealer, WaveSpawner 등이 구체 클래스에 직접 의존하면
결합도가 높아지고 금지 패턴(`FindObjectsByType`)을 써야 하는 상황이 생긴다.
공통 인터페이스로 추상화해 의존 방향을 정리한다.

## Decision

### 인터페이스 목록

| 인터페이스 | 파일 위치 | 구현 클래스 |
|-----------|----------|-----------|
| `IPoolable` | `Scripts/Core/IPoolable.cs` | `ChaserEnemy`, `RusherEnemy`, `Projectile`, `HitFeedbackVFX` |
| `IDamageable` | `Scripts/Core/IDamageable.cs` | `HealthComponent` |
| `IInitializable` | `Scripts/Core/IInitializable.cs` | `XPSystem`, `GoldSystem`, `SkillSystem`, `ObjectPool` |
| `IHitFeedback` | `Scripts/Combat/IHitFeedback.cs` | `HitFeedback` |
| `ISkillData` | `Scripts/Skills/ISkillData.cs` | 각 스킬 ScriptableObject |
| `ISkillSlot` | `Scripts/Skills/ISkillSlot.cs` | `SkillSystem.SkillSlot` (내부 클래스) |
| `IEnemyController` | `Scripts/Enemy/IEnemyController.cs` | `ChaserEnemy`, `RusherEnemy` |

### 핵심 설계 결정

**ISkill 구현 방식 — 하이브리드 (Option C)**
- SO(`ISkillData`)는 수치 데이터만 보유 (BaseDamage, BaseCooldown, Icon)
- 발동 로직(Physics.OverlapSphere 등)은 `SkillSystem` 메서드에서 처리
- 이유: ScriptableObject는 MonoBehaviour 없이 Physics API 사용 불가

**오브젝트 풀 반환 방식 — 직접 호출 (Option A)**
- 적·투사체가 사망/만료 시 `ObjectPool.Instance.ReturnToPool(this)` 직접 호출
- ADR-0001 싱글턴 허용 목록에 ObjectPool 포함되므로 원칙 준수
- 이유: 코드 흐름이 명확하고 추적이 쉬움

### 의존 방향

```
ObjectPool
  └─ IPoolable.OnSpawn() / OnDespawn() 호출
  └─ 반환: 각 구현체가 ObjectPool.Instance.ReturnToPool(this) 직접 호출

DamageDealer
  └─ IDamageable.TakeDamage(amount) 호출
  └─ HealthComponent가 IDamageable 구현

GameManager.Start()
  └─ IInitializable.Initialize() 순서대로 호출
     1. ObjectPool
     2. GoldSystem
     3. XPSystem
     4. SkillSystem

HealthComponent (static event)
  └─ OnDamaged → IHitFeedback.PlayHitFeedback() 구독
  └─ OnDeath   → IHitFeedback.PlayDeathFeedback() 구독

HUDController
  └─ ISkillSlot.CooldownNormalized 읽기 (SkillSystem 내부 노출 안 함)

WaveSpawner / GameManager
  └─ IEnemyController.Activate(playerTransform)
  └─ IEnemyController.Deactivate()
```

## Consequences

### Positive
- `FindObjectsByType` 없이 전체 AI 일괄 제어 가능 (`List<IEnemyController>` 캐싱)
- DamageDealer가 HealthComponent 구체 타입 미참조
- HUD가 SkillSystem 내부 구조 몰라도 쿨타임 읽기 가능

### Negative
- 인터페이스 7개 + enum 2개 — 소규모 프로젝트 대비 다소 많음

### Risks
- `IInitializable.Initialize()` 호출 순서 누락 시 NullReference
  → GameManager.Start()에서 명시적 순서로 일괄 호출해 방지

## Related Decisions
- ADR-0001: 싱글턴 사용 기준
- ADR-0002: 시스템 간 통신 방식
- ADR-0003: ScriptableObject 로딩 방식
