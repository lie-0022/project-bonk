# ADR-0005: Inspector 허용 범위

## Status
Accepted

## Date
2026-04-13

## Context

### Problem Statement
Inspector 연결을 최소화하되, 완전히 제거하면 오히려 유지보수가 어려운 경우가 있다.
명확한 허용/금지 기준이 필요하다.

### Constraints
- Inspector 연결 최소화 원칙
- 튜닝 값(수치)은 디자이너가 Inspector에서 쉽게 조절할 수 있어야 함

### Requirements
- 허용/금지 기준이 한 줄로 설명 가능해야 함
- 예외 없이 일관되게 적용

## Decision

**Inspector는 "같은 프리팹 내부의 수치(튜닝값)" 에만 허용한다.**

### 허용 O

```csharp
// 수치 튜닝값 — Inspector에서 조절 가능해야 함
[SerializeField] private float _moveSpeed = 6f;
[SerializeField] private float _dashDuration = 0.2f;
[SerializeField] private float _jumpHeight = 2f;
[SerializeField] private int _xpReward = 10;
```

### 허용 X (코드로 처리)

```csharp
// 다른 씬 오브젝트 참조 → Singleton 또는 GetComponent 사용
[SerializeField] private XPSystem _xpSystem;   // ❌
[SerializeField] private GameManager _gm;      // ❌

// ScriptableObject → Resources.Load 사용
[SerializeField] private EnemyStatsSO _stats;  // ❌ (씬 오브젝트에서)

// 다른 GameObject의 컴포넌트 → FindWithTag + GetComponent 사용
[SerializeField] private Transform _player;    // ❌
```

### 판단 기준 한 줄 요약

> **"이 값이 숫자(float/int/bool)인가?"** → Inspector 허용  
> **"이 값이 다른 오브젝트/컴포넌트/에셋 참조인가?"** → 코드로 처리

### 예외 케이스

| 케이스 | 처리 방식 |
|--------|----------|
| `CameraController._target` (플레이어 Transform) | `FindWithTag("Player")` in Awake |
| `WaveSpawner` 스폰 포인트 배열 | 같은 프리팹/씬 내 자식이면 Inspector 허용 |
| Prefab 내 자식 컴포넌트 참조 | `GetComponentInChildren<T>()` 우선, 불가 시 Inspector 허용 |

## Alternatives Considered

### Alternative 1: Inspector 전면 금지
- **Cons**: 튜닝값 수정마다 코드 수정 필요, 비효율적
- **Rejection Reason**: 수치 튜닝은 Inspector가 훨씬 편리

### Alternative 2: 제한 없이 Inspector 사용
- **Cons**: 씬마다 연결 필요, 연결 누락 시 NullReference
- **Rejection Reason**: Inspector 최소화 원칙 위배

## Consequences

### Positive
- 수치 튜닝은 Inspector에서 직관적으로 가능
- 씬 간 참조 깨짐 없음
- 새 씬 추가 시 Inspector 재연결 불필요

### Negative
- "숫자인가 참조인가" 판단이 모호한 케이스 발생 가능

### Risks
- 팀원이 기준을 모를 경우 → 이 ADR을 온보딩 자료로 공유

## Validation Criteria
- `[SerializeField]` 필드 타입이 primitive(float, int, bool) 또는 string인지 확인
- 씬 오브젝트에 컴포넌트/SO 참조 `[SerializeField]` 없음

## Related Decisions
- ADR-0001: 싱글턴 사용 기준
- ADR-0003: ScriptableObject 로딩 방식
- ADR-0004: 컴포넌트 참조 방식
