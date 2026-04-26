# ADR-0004: 컴포넌트 참조 방식

## Status
Accepted

## Date
2026-04-13

## Context

### Problem Statement
같은 GameObject 또는 자식 오브젝트의 컴포넌트를 참조할 때 Inspector 드래그 대신
코드에서 처리하는 방식이 필요하다.

### Constraints
- Inspector 연결 최소화 원칙
- `FindObjectsOfType` 런타임 반복 호출 금지 (코딩 표준)
- Awake/Start에서 1회만 캐싱

### Requirements
- 런타임 성능 영향 없음 (캐싱)
- 참조 누락 시 명확한 에러

## Decision

**컴포넌트 참조는 상황에 따라 3가지 방식으로 처리한다.**

### 방식 1: 같은 GameObject → `GetComponent<T>()` in Awake

```csharp
public class EnemyAI : MonoBehaviour
{
    private HealthComponent _health;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
    }
}
```

### 방식 2: 글로벌 시스템 → Singleton.Instance

```csharp
public class EnemyAI : MonoBehaviour
{
    private void Die()
    {
        XPSystem.Instance.AddXP(_xpReward);
        GoldSystem.Instance.AddGold(_goldReward);
    }
}
```

### 방식 3: 씬 내 유일한 타입 → `FindFirstObjectByType<T>()` in Awake (1회만)

```csharp
// 싱글턴이 아닌 씬 오브젝트가 필요한 예외적 경우만
public class WaveSpawner : MonoBehaviour
{
    private Transform _playerTransform;

    private void Awake()
    {
        // "Player" 태그로 찾기 (FindFirstObjectByType 대신 태그 우선)
        _playerTransform = GameObject.FindWithTag("Player").transform;
    }
}
```

### 참조 방식 결정 흐름

```
같은 GameObject인가?
  └─ YES → GetComponent<T>() in Awake

글로벌 시스템인가? (ADR-0001 싱글턴 목록)
  └─ YES → Singleton.Instance

씬에 하나뿐인 오브젝트인가?
  └─ YES → FindWithTag() 또는 FindFirstObjectByType<T>() in Awake (1회 캐싱)
  └─ NO  → Inspector [SerializeField] 허용 (프리팹 내부)
```

## Alternatives Considered

### Alternative 1: 전부 Inspector [SerializeField]
- **Cons**: Inspector 최소화 원칙 위배, 씬마다 재연결
- **Rejection Reason**: 원칙 위배

### Alternative 2: 전부 FindObjectsByType
- **Cons**: 런타임 성능 비용, 금지된 패턴
- **Rejection Reason**: 코딩 표준 위반

## Consequences

### Positive
- Inspector 연결 대부분 불필요
- Awake 1회 캐싱으로 성능 영향 없음

### Negative
- Awake 순서 의존성 발생 가능 (GetComponent는 무관, Singleton은 Awake 순서 주의)

### Risks
- Singleton.Instance가 Awake에서 아직 초기화 안 됐을 경우 → Singleton의 Awake가 먼저 실행되도록 Script Execution Order 설정

## Validation Criteria
- `FindObjectsOfType` 런타임 반복 호출 없음
- 모든 `GetComponent` 호출은 Awake/Start에서만

## Related Decisions
- ADR-0001: 싱글턴 사용 기준
- ADR-0005: Inspector 허용 범위
