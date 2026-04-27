# ADR-0001: 싱글턴 사용 기준

## Status
Accepted

## Date
2026-04-13

## Context

### Problem Statement
여러 시스템(XP, 골드, 무기 등)이 게임 전반에서 접근된다. Inspector 드래그 연결 없이
코드에서 직접 참조할 수 있는 접근 방식이 필요하다.

### Constraints
- Inspector 연결 최소화 원칙
- 학교 프로젝트 규모 (복잡한 DI 프레임워크 도입 불필요)
- Unity MonoBehaviour 기반

### Requirements
- 어느 스크립트에서도 Inspector 없이 글로벌 시스템에 접근 가능
- 씬 로드 시 중복 인스턴스 방지

## Decision

**글로벌 게임 시스템은 싱글턴 패턴을 사용한다. 로컬 컴포넌트는 싱글턴 금지.**

### 싱글턴 허용 목록

| 클래스 | 이유 |
|--------|------|
| `GameManager` | 게임 상태 전반 제어 |
| `XPSystem` | 경험치·레벨업 전역 관리 |
| `GoldSystem` | 골드 전역 관리 |
| `WeaponSystem` | 무기 슬롯·발동 전역 관리 |
| `ObjectPool` | 오브젝트 풀 전역 관리 |

### 싱글턴 금지 목록

| 클래스 | 이유 |
|--------|------|
| `HealthComponent` | 플레이어·적 각자 인스턴스 필요 |
| `DamageDealer` | 오브젝트마다 별개 |
| `EnemyAI` | 적마다 별개 |
| `AutoAttack` | 플레이어 전용 컴포넌트 |
| `HitFeedback` | 오브젝트마다 별개 |

### 싱글턴 구현 패턴

```csharp
public class XPSystem : MonoBehaviour
{
    public static XPSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
}
```

### Architecture Diagram

```
[PlayerController] ──────────────────────────────┐
[EnemyAI]          → XPSystem.Instance.AddXP()   │
[WaveSpawner]      → GoldSystem.Instance          ├─ Singleton Systems
[WeaponSelectionUI] → WeaponSystem.Instance       │
[HUDController]    → GameManager.Instance         │
                                                  ┘
[EnemyAI] → GetComponent<HealthComponent>()  ← Local Component
```

## Alternatives Considered

### Alternative 1: 모든 참조를 Inspector로
- **Pros**: Unity 표준 방식
- **Cons**: 씬마다 드래그 연결 필요, 연결 누락 시 런타임 NullReference
- **Rejection Reason**: Inspector 최소화 원칙에 위배

### Alternative 2: ServiceLocator 패턴
- **Pros**: 싱글턴보다 테스트 용이
- **Cons**: 학교 프로젝트에 오버엔지니어링
- **Rejection Reason**: 규모 대비 복잡도 높음

## Consequences

### Positive
- Inspector 드래그 없이 어디서든 시스템 접근 가능
- 씬 전환 시 참조 깨짐 없음

### Negative
- 글로벌 상태가 많아지면 디버깅 복잡도 증가

### Risks
- 싱글턴 남용 시 의존성 스파게티 → 허용 목록 엄수로 방지

## Validation Criteria
- 허용 목록 외 클래스에 `Instance` 프로퍼티 없음
- 씬 재로드 시 중복 인스턴스 없음

## Related Decisions
- ADR-0002: 시스템 간 통신 방식
- ADR-0004: 컴포넌트 참조 방식
