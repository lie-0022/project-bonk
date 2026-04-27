# ADR-0003: ScriptableObject 로딩 방식

## Status
Accepted

## Date
2026-04-13

## Context

### Problem Statement
적 스탯, 무기 정의 등 게임플레이 수치를 ScriptableObject로 관리한다.
씬 오브젝트에 Inspector로 SO를 드래그 연결하면 씬마다 재연결이 필요하고
Inspector 최소화 원칙에 위배된다.

### Constraints
- Inspector 연결 최소화 원칙
- Addressables 미사용 (학교 프로젝트 범위)

### Requirements
- 코드에서 SO를 직접 로드
- 경로 규칙이 명확해야 함

## Decision

**ScriptableObject는 `Resources/` 폴더에 배치하고 `Resources.Load<T>()`로 코드에서 로드한다.**

### 폴더 구조

```
Assets/
└── Resources/
    └── Data/
        ├── Enemy/
        │   ├── Enemy_Goblin_Data.asset
        │   └── Enemy_Goblin_Elite_Data.asset
        ├── Player/
        │   └── Player_Swordsman_Data.asset
        └── Skills/
            ├── Skill_Slash_Data.asset
            └── Skill_Fireball_Data.asset
```

### 로딩 패턴

```csharp
// 단일 로드
var data = Resources.Load<EnemyStatsSO>("Data/Enemy/Enemy_Goblin_Data");

// 카테고리 전체 로드
var allSkills = Resources.LoadAll<SkillDefinitionSO>("Data/Skills");
```

### SO 클래스 네이밍

```csharp
[CreateAssetMenu(fileName = "Enemy_Goblin_Data", menuName = "BladeSurge/Enemy Stats")]
public class EnemyStatsSO : ScriptableObject { }

[CreateAssetMenu(fileName = "Skill_Slash_Data", menuName = "BladeSurge/Skill Definition")]
public class SkillDefinitionSO : ScriptableObject { }
```

### Inspector 허용 예외
프리팹 내부에서 자신의 SO를 직접 참조하는 경우만 Inspector 허용.
씬 오브젝트에서의 SO 연결은 금지.

## Alternatives Considered

### Alternative 1: Inspector 드래그 연결
- **Cons**: 씬마다 재연결 필요, Inspector 최소화 원칙 위배
- **Rejection Reason**: 원칙 위배

### Alternative 2: Addressables
- **Pros**: 메모리 관리 우수, 비동기 로딩
- **Cons**: 학교 프로젝트에 오버엔지니어링, 설정 복잡
- **Rejection Reason**: 규모 대비 복잡도 높음

## Consequences

### Positive
- Inspector 연결 불필요
- 경로 규칙만 지키면 어디서든 로드 가능

### Negative
- `Resources/` 폴더는 빌드에 전부 포함되어 용량 증가 가능
- 경로 문자열 오타 시 런타임 null (컴파일 타임 체크 불가)

### Risks
- 경로 오타 → 각 SO 타입에 경로 상수 정의로 방지

```csharp
public static class DataPaths
{
    public const string Enemy = "Data/Enemy/";
    public const string Skills = "Data/Skills/";
    public const string Player = "Data/Player/";
}
```

## Validation Criteria
- 씬 오브젝트의 SO 필드에 Inspector 연결 없음
- Resources.Load 반환값 null 체크 후 사용

## Related Decisions
- ADR-0001: 싱글턴 사용 기준
