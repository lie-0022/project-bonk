# 데미지 시스템 (Damage System)

> **Status**: Designed
> **Author**: 사용자 + Claude
> **Last Updated**: 2026-04-17
> **Implements Pillar**: 조작의 통제감, 파워 에스컬레이션

## Overview

데미지 시스템은 공격이 피해를 발생시키는 방식을 정의하는 Core 레이어 컴포넌트다. 무기, 기본 공격, 적의 공격 모두 이 시스템을 통해 `HealthComponent.TakeDamage()`를 호출한다. MVP에서는 단순 고정 피해(flat damage) 방식을 사용하며, 향후 크리티컬·방어력 등 확장 가능한 구조로 설계한다.

## Player Fantasy

무기를 발동했을 때 적의 체력바가 눈에 띄게 줄어드는 것이 파워 트립의 시각적 핵심이다. 숫자가 크게 튀어나올수록, 적이 빠르게 사라질수록 플레이어는 강해진다는 느낌을 받는다. 데미지 시스템이 이 피드백의 근간이다.

## Detailed Design

### Core Rules

1. 모든 피해는 `DamageInfo` 구조체로 표현된다: `{float amount, DamageSource source, GameObject instigator}`
2. 피해를 주는 주체(무기, 기본 공격, 적)는 `DamageDealer.Deal(DamageInfo, HealthComponent target)`을 호출한다
3. `DamageDealer`는 `target.TakeDamage(amount)`를 호출하고, 피해 숫자 팝업 이벤트를 발생시킨다
4. MVP에서는 방어력, 저항, 크리티컬 없음 — 피해량 = `DamageInfo.amount` 그대로 적용
5. 피해는 항상 양수. 0 이하의 피해는 무시
6. **팀 구분**: `DamageSource`로 플레이어 공격과 적 공격을 구분해 아군 피해(friendly fire) 방지
7. 플레이어 공격은 적에게만, 적 공격은 플레이어에게만 피해를 줄 수 있다

### States and Transitions

별도 상태 없음. 데미지 시스템은 호출될 때마다 즉시 처리되는 stateless 시스템이다.

### Interactions with Other Systems

- **← 무기 시스템**: 무기 히트 시 `DamageDealer.Deal()` 호출
- **← 기본 공격**: 공격 히트 시 `DamageDealer.Deal()` 호출
- **← 적 AI**: 적 공격 히트 시 `DamageDealer.Deal()` 호출
- **→ 체력 시스템**: `target.TakeDamage(amount)` 호출
- **→ HUD 시스템**: 피해 숫자 팝업 이벤트 발생 (선택적, MVP 후순위)

## Formulas

```
피해 적용:
  finalDamage = damageInfo.amount  // MVP: 보정 없음
  target.TakeDamage(finalDamage)

팀 구분 검사:
  if damageInfo.source == DamageSource.Player && target.tag != "Enemy": return
  if damageInfo.source == DamageSource.Enemy  && target.tag != "Player": return

기본 피해 값:
  기본 공격 피해   = 15 (검사 기준)
  무기별 피해      = 무기 획득 시스템 GDD 참조
  적(추적형) 접촉  = 8  (초당)
  적(돌진형) 충돌  = 20 (1회)
```

## Edge Cases

- **0 피해**: 무시. 이펙트도 재생하지 않음
- **동시 다중 히트**: 범위 무기가 같은 프레임에 같은 적을 여러 번 히트하는 경우 → 히트 판정마다 독립적으로 피해 적용 (단, 무기별로 동일 타겟 히트 쿨다운 설정 가능)
- **이미 사망한 적**: `HealthComponent.isAlive == false`인 타겟에 피해 호출 시 무시
- **플레이어→플레이어 피해**: `DamageSource` 검사로 차단
- **피해 오버플로우**: `float` 범위를 초과하는 피해는 MVP에서 발생하지 않으나, `Mathf.Min(damage, 9999f)` 캡 적용 권장

## Dependencies

### 업스트림 (이 시스템이 의존하는 것)
- **체력 시스템**: `TakeDamage()` 인터페이스 필요

### 다운스트림 (이 시스템에 의존하는 것)
- 무기 시스템, 기본 공격, 적 AI — 모두 `DamageDealer.Deal()` 호출

### 인터페이스 계약
```csharp
struct DamageInfo {
    float amount;
    DamageSource source;  // enum: Player, Enemy
    GameObject instigator;
}

static class DamageDealer {
    static void Deal(DamageInfo info, HealthComponent target)
}
```

## Tuning Knobs

| 변수 | 기본값 | 안전 범위 | 설명 |
|------|--------|----------|------|
| 기본 공격 피해 | 10 | 5 ~ 20 | 낮을수록 무기 의존도 증가 |
| 적(추적형) 접촉 피해 | 8/초 | 3 ~ 15/초 | 높으면 이동 회피가 매우 중요 |
| 적(돌진형) 충돌 피해 | 20 | 10 ~ 40 | 돌진 회피 실패 시 페널티 |

*무기별 피해는 무기 획득 시스템 GDD에서 관리*

## Visual/Audio Requirements

- 피해 숫자 팝업: 타격 위치에서 위로 올라가는 텍스트 (선택적, MVP 후순위)
- 히트 사운드: 무기/기본 공격 히트 시 타격음 (선택적)

## UI Requirements

- 없음 (피해 숫자 팝업은 HUD 시스템에서 구현)

## Acceptance Criteria

- [ ] 플레이어 무기가 적에게 올바른 피해량을 적용함
- [ ] 적의 공격이 플레이어에게 올바른 피해량을 적용함
- [ ] 플레이어 공격이 플레이어 자신에게 피해를 주지 않음
- [ ] 이미 사망한 적에게 피해를 줘도 추가 이벤트가 발생하지 않음
- [ ] 대시 중(무적) 플레이어는 적의 공격을 받아도 HP가 감소하지 않음

## Open Questions

- **크리티컬 시스템**: Vertical Slice 이후 추가 검토. 크리티컬 확률 + 2배 피해 방식이 파워 에스컬레이션 필라와 잘 맞음.
- **피해 감소(방어력)**: MVP에서는 없음. 후반 웨이브 밸런싱 시 필요하면 추가.
