# 피격 피드백 (Hit Feedback)

> **Status**: Designed (역공학)
> **Author**: 사용자 + Claude
> **Last Updated**: 2026-05-08
> **Implements Pillar**: 게임필 — 타격감, 사망 인지

## Overview

피격 피드백은 데미지를 받은 대상이 *시각적으로 즉시 반응*하도록 만드는 UI/연출 시스템이다.
`HealthComponent`의 데미지·사망 이벤트를 구독해 메시 색상을 흰색(피격) 또는 회색(사망)으로 짧게 플래시한다.
`IHitFeedback` 인터페이스로 추상화되어 향후 사운드·파티클 등 추가 피드백 구현체로 확장 가능하다.

## Player Fantasy

검을 휘두르거나 총알이 적에 닿는 순간 적이 *번쩍* 반응해야 한다. "맞췄다"의 즉각 확인이 자동공격
서바이벌의 핵심 만족감이며, 피드백이 없으면 적이 그냥 사라지는 듯한 무미한 게임필이 된다.
플레이어 자신도 피격 시 동일하게 반응해 "맞고 있다"를 인지시켜야 한다.

## Detailed Rules

1. `IHitFeedback` 인터페이스 정의 — 피격/사망 피드백 재생 메서드 2개
   - `void PlayHitFeedback(float amount)` — 데미지량 인자
   - `void PlayDeathFeedback()` — 인자 없음
2. 기본 구현체 `HitFlash`는 `MonoBehaviour + IHitFeedback`이며 `[RequireComponent(typeof(HealthComponent))]`로 강제
3. `HitFlash.Awake`에서 자체 `HealthComponent`와 자식 중 첫 `Renderer`를 캐싱
4. `OnEnable`에서 HealthComponent의 `OnDamaged`, `OnDeath` 이벤트 구독, `OnDisable`에서 해제
5. `OnDamaged(amount, currentHp)` 수신 시 `PlayHitFeedback(amount)` 호출 → 흰색 플래시
6. `OnDeath(_)` 수신 시 `PlayDeathFeedback()` 호출 → 회색 플래시
7. 플래시 동작:
   - 기존 코루틴이 있으면 `StopCoroutine` 후 새로 시작 (중첩 방지)
   - `_renderer.material.color = flashColor` 즉시 적용
   - `WaitForSeconds(_flashDuration)` (기본 0.1초)
   - 원래 색으로 복구 (Awake 시 캐싱한 `_originalColor`)
8. `_renderer == null`이면 모든 메서드가 조기 반환 (예외 없이)
9. 적과 플레이어 모두 적용 — 단 `Renderer`가 자식에 있는 모델 구조여야 함

## Formulas

```
플래시 색상 결정:
  Hit:   Color.white  (1, 1, 1, 1)
  Death: Color.gray   (0.5, 0.5, 0.5, 1)

플래시 시퀀스:
  if existing flashCoroutine: stop
  material.color = flashColor                  // t = 0
  wait flashDuration                            // 0.1s 기본
  if renderer still exists:
    material.color = originalColor              // 원복

원복 보호:
  - 코루틴 중간에 GameObject가 풀로 반환되거나 파괴되면 색 복구 스킵
  - HitFlash 인스턴스 자체가 사라져도 코루틴 종료 시 NullReferenceException 방지

플레이어 입장에서 시각적 양:
  flashDuration = 0.1s
  → 60fps 기준 약 6프레임 동안 흰색
  → 너무 짧으면 인지 어려움, 너무 길면 색 잔상

데미지량(amount)은 현재 사용 안 함 (확장 여지):
  // 향후: 강도/색상 매핑 가능
  // ex) amount > 50 → 빨간색 플래시 + 카메라 셰이크
```

## Edge Cases

- **자식 Renderer 없음**: `GetComponentInChildren<Renderer>()` 결과가 null → 모든 피드백 메서드 조기 반환. 컴파일 에러 없이 무시.
- **연속 피격 (코루틴 중첩)**: 새 호출 시 기존 코루틴 정지 후 재시작. 색상이 끊김 없이 다시 흰색으로 리셋.
- **사망 직전 피격**: OnDamaged → 흰 플래시 시작 → 같은 프레임 OnDeath → 회색 플래시로 교체. 흰 플래시 코루틴은 정지됨.
- **풀로 반환 중 코루틴**: `_renderer == null` 체크가 코루틴 끝에 있어 안전. 풀에서 다시 활성화되면 OnEnable이 재구독.
- **머티리얼 인스턴스**: `material.color` 접근은 자동으로 머티리얼 인스턴스 생성 (Unity 표준). 메모리 증가 가능 — 풀링 대상에서 주의.
- **다중 Renderer**: 첫 자식 Renderer 1개만 플래시. 모든 메시 동시 플래시 필요 시 `GetComponentsInChildren` 확장 필요.
- **OnDamaged amount 음수/0**: HealthComponent 측에서 가드 — 도달하면 플래시는 그대로 재생됨.
- **씬 비활성화**: OnDisable에서 이벤트 해제, 코루틴은 GameObject 비활성화로 자동 정지.

## Dependencies

### 업스트림
- **체력 시스템** (`HealthComponent`):
  - `event Action<float, float> OnDamaged(amount, currentHp)`
  - `event Action<float> OnDeath(currentHp)`
  - `[RequireComponent]`로 강제 의존
- **데미지 시스템** (`DamageDealer`): HealthComponent를 통해 간접 트리거

### 다운스트림
- 없음 (피드백은 종단 — 시각적 출력으로 끝)
- 향후 사운드 시스템·VFX 시스템과 결합 시 별도 IHitFeedback 구현체로 분기

### 양방향 명시
- `design/gdd/health-system.md` 의 OnDamaged/OnDeath 이벤트 ↔ HitFlash 구독자
- `design/gdd/damage-system.md` 의 데미지 흐름 끝단 ↔ 피격 피드백 트리거

### 인터페이스 계약
- `interface IHitFeedback`
  - `void PlayHitFeedback(float amount)`
  - `void PlayDeathFeedback()`
- 한 GameObject에 다수 IHitFeedback 구현체 부착 가능 (HitFlash + 향후 HitSound 등 병렬)
- HealthComponent는 IHitFeedback을 직접 알 필요 없음 — 이벤트 발행만 담당

## Tuning Knobs

| 변수 | 위치 | 기본값 | 안전 범위 | 영향 |
|------|------|--------|----------|------|
| `_flashDuration` | HitFlash (SerializeField) | 0.1 | 0.05 ~ 0.25 | 길수록 인지 쉬우나 잔상감, 짧으면 놓침 |
| Hit 색상 | HitFlash 코드 상수 | Color.white | white / lightYellow / red | 적/플레이어 구분 가능성 (현재 동일) |
| Death 색상 | HitFlash 코드 상수 | Color.gray | gray / black / darkRed | 사망 시각 구분 |
| Renderer 검색 범위 | `GetComponentInChildren` | 첫 매치 | 전체 매치 | 다중 메시 모델 시 변경 검토 |

## Acceptance Criteria

- [ ] 적 처치 직전이 아닌 일반 피격 시 적 메시가 약 0.1초 흰색으로 변하고 원래 색으로 복구
- [ ] 플레이어가 피격받을 때 플레이어 메시가 동일하게 0.1초 흰 플래시
- [ ] 사망 처리 직후 메시가 회색으로 0.1초 변함 (풀 반환 전 시각 신호)
- [ ] 0.1초 이내에 연속 피격 발생 시 흰 플래시가 끊기지 않고 재시작 (색 진동 없음)
- [ ] `Renderer`가 없는 GameObject에 HitFlash가 부착되어도 NullReferenceException 발생 안 함
- [ ] 풀에서 회수된 적이 다시 활성화될 때 원래 색상이 유지됨 (이전 플래시 잔여 X)
- [ ] HealthComponent 없이 HitFlash만 부착 시 컴파일 단계에서 자동으로 HealthComponent 추가 (`RequireComponent`)
- [ ] OnDisable 시 이벤트 구독이 해제되어 비활성 GameObject에서 콜백이 실행되지 않음

## Open Questions

- 강도 매핑 — `amount`에 비례한 색상/지속시간 조절 (큰 데미지 = 빨강 플래시 등) 도입 여부
- 카메라 셰이크 연동 — 플레이어 피격 시 별도 IHitFeedback 구현체로 추가
- 사운드 피드백 — `HitSound` 컴포넌트를 IHitFeedback 두 번째 구현체로 분리, 적/플레이어/무기 타입별 SFX
- VFX 파티클 — `HitParticle` 구현체로 피·먼지·스파크 (현재 `production/remaining-work.md` 4장에 미작성 항목)
- 다중 Renderer 모델 대응 — 캐릭터 모델에 메시가 여러 개일 경우 전체 동기 플래시 필요성
