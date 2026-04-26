# ADR-0002: 시스템 간 통신 방식

## Status
Accepted

## Date
2026-04-13

## Context

### Problem Statement
XP 획득 → HUD 업데이트, 레벨업 → 스킬 선택 UI 표시처럼 시스템 간 통신이 빈번하다.
직접 참조 방식은 결합도를 높이고, Inspector 연결은 최소화 원칙에 위배된다.

### Constraints
- Inspector 연결 최소화 원칙
- Unity 외부 라이브러리 없이 구현
- 순환 참조 방지

### Requirements
- 발신 시스템이 수신 시스템을 직접 알 필요 없음
- 구독/해제가 명확해야 함 (메모리 누수 방지)

## Decision

**시스템 간 통신은 C# static event를 사용한다.**

### 패턴

```csharp
// 발신 측 — 이벤트 선언 및 발행
public class XPSystem : MonoBehaviour
{
    public static event Action<int> OnLevelUp;
    public static event Action<int, int> OnXPChanged; // (current, max)

    private void LevelUp()
    {
        OnLevelUp?.Invoke(_currentLevel);
    }
}

// 수신 측 — 구독 (Inspector 연결 없음)
public class HUDController : MonoBehaviour
{
    private void OnEnable()
    {
        XPSystem.OnXPChanged += UpdateXPBar;
        XPSystem.OnLevelUp += ShowLevelUpEffect;
    }

    private void OnDisable()
    {
        XPSystem.OnXPChanged -= UpdateXPBar;
        XPSystem.OnLevelUp -= ShowLevelUpEffect;
    }
}
```

### 이벤트 목록 (예정)

| 이벤트 | 발신 | 수신 |
|--------|------|------|
| `GameManager.OnGameStateChanged` | GameManager | CameraController, HUD, UI 전체 |
| `XPSystem.OnLevelUp` | XPSystem | SkillSelectionUI, HUDController |
| `XPSystem.OnXPChanged` | XPSystem | HUDController |
| `GoldSystem.OnGoldChanged` | GoldSystem | HUDController |
| `SkillSystem.OnSkillEquipped` | SkillSystem | HUDController |

### Architecture Diagram

```
XPSystem ──OnLevelUp──► SkillSelectionUI
         ──OnXPChanged─► HUDController

GameManager ──OnGameStateChanged──► CameraController
                                 ──► HUDController
                                 ──► PlayerController
```

## Alternatives Considered

### Alternative 1: 직접 참조
```csharp
_hudController.UpdateXP(xp); // XPSystem이 HUD를 직접 알아야 함
```
- **Cons**: 결합도 높음, Inspector 연결 필요
- **Rejection Reason**: Inspector 최소화 원칙 위배, 순환 참조 위험

### Alternative 2: UnityEvent
- **Pros**: Inspector에서 시각적으로 확인 가능
- **Cons**: Inspector 연결 필요, 성능 오버헤드
- **Rejection Reason**: Inspector 최소화 원칙 위배

## Consequences

### Positive
- 시스템 간 결합도 낮음
- Inspector 연결 불필요
- 구독 해제 패턴(`OnDisable`)으로 메모리 누수 방지

### Negative
- 이벤트 발신처 추적이 직접 참조보다 어려움

### Risks
- `OnDisable`에서 구독 해제 누락 시 메모리 누수 → 모든 구독은 반드시 OnDisable에서 해제

## Validation Criteria
- 시스템 간 직접 참조(`GetComponent` 크로스 시스템) 없음
- OnEnable 구독 = OnDisable 해제 1:1 대응

## Related Decisions
- ADR-0001: 싱글턴 사용 기준
- ADR-0004: 컴포넌트 참조 방식
