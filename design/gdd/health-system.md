# 체력 시스템 (Health System)

> **Status**: Designed
> **Author**: 사용자 + Claude
> **Last Updated**: 2026-04-17
> **Implements Pillar**: 조작의 통제감

## Overview

체력 시스템은 플레이어와 적 모두의 생존 상태를 관리하는 Foundation 레이어 컴포넌트다. `HealthComponent`를 플레이어와 적에게 공통으로 부착하는 방식으로 구현하며, 현재 HP, 최대 HP, 사망 판정, 피격 이벤트를 처리한다. 플레이어는 HP 회복이 없으며, 적은 사망 시 경험치 드롭 이벤트를 발생시킨다.

## Player Fantasy

플레이어는 체력이 줄어들 때 긴장감을 느끼고, 위험을 피하기 위해 대시 타이밍을 신중하게 결정한다. 적의 체력바는 "이 적을 처치하려면 얼마나 더 때려야 하는가"를 명확히 전달해 타격감을 강화한다. 회복이 없기 때문에 실수에 대한 무게감이 있고, 이것이 쿨타임 관리와 대시 타이밍의 긴장감을 만든다.

## Detailed Design

### Core Rules

1. `HealthComponent`는 플레이어와 모든 적에게 공통으로 사용되는 단일 컴포넌트다
2. 체력은 `currentHP`와 `maxHP` 두 값으로 관리된다
3. `TakeDamage(float amount)` 호출 시 `currentHP -= amount`, 최솟값은 0
4. `currentHP <= 0`이면 즉시 `OnDeath` 이벤트를 발생시키고 사망 처리한다
5. **플레이어 체력 회복 없음** — 오직 피해를 피하는 것만이 생존 방법
6. **적 사망 시**: `OnDeath` 이벤트에 경험치 드롭량(`xpReward`)을 포함해 브로드캐스트
7. **플레이어 사망 시**: `OnDeath` 이벤트를 `GameManager.ChangeState(GameOver)`로 전달
8. 피격 시 `OnDamaged(float amount, float currentHP)` 이벤트 발생 (HUD, 피격 이펙트용)
9. 무적 상태(`isInvincible = true`)일 때는 `TakeDamage` 호출을 무시

### States and Transitions

| 상태 | 조건 | 동작 |
|------|------|------|
| Alive | `currentHP > 0` | 정상 피해 수신 가능 |
| Invincible | `isInvincible == true` | 피해 무시 (대시 무적 프레임용) |
| Dead | `currentHP <= 0` | `OnDeath` 발생, 컴포넌트 비활성화 |

### Interactions with Other Systems

- **← 데미지 시스템**: `TakeDamage(amount)` 호출
- **← 플레이어 이동**: 대시 중 `isInvincible = true` 설정
- **→ 게임 상태 관리**: 플레이어 `OnDeath` → `GameManager.ChangeState(GameOver)`
- **→ 경험치 시스템**: 적 `OnDeath(xpReward)` → XP 시스템이 수신해 플레이어에게 지급
- **→ HUD 시스템**: `OnDamaged` 이벤트 → 플레이어 체력바 업데이트
- **→ 적 체력바 UI**: 적 `OnDamaged` 이벤트 → 머리 위 체력바 업데이트

## Formulas

```
피해 계산:
  currentHP = Max(0, currentHP - damage)

사망 판정:
  if currentHP <= 0:
    OnDeath.Invoke(xpReward)  // 적의 경우 xpReward 포함
    isAlive = false

기본 스탯:
  플레이어 maxHP  = 100
  적(추적형) maxHP = 30
  적(돌진형) maxHP = 50
```

## Edge Cases

- **중복 사망**: `OnDeath`가 두 번 발생하지 않도록 `isAlive` 플래그로 방지
- **음수 피해(힐)**: MVP에서는 회복 없으므로 `TakeDamage`에 음수 전달 시 무시
- **대시 무적 중 피격**: `isInvincible == true`이면 `TakeDamage` 즉시 반환. 이펙트도 재생하지 않음
- **동시 다발 피격**: 같은 프레임에 여러 피해가 들어오면 순서대로 처리. 첫 번째 피해로 HP가 0이 되면 이후 피해는 이미 Dead 상태이므로 무시
- **maxHP 0 이하**: 절대 불가 설정 — `maxHP`는 인스펙터에서 최솟값 1로 강제

## Dependencies

### 업스트림 (이 시스템이 의존하는 것)
- **없음** — Foundation 레이어

### 다운스트림 (이 시스템에 의존하는 것)
- 데미지 시스템 (`TakeDamage` 호출)
- 플레이어 이동 (`isInvincible` 설정)
- 경험치 시스템 (`OnDeath` 이벤트 구독)
- 게임 상태 관리 (`OnDeath` 이벤트 구독)
- HUD 시스템 (`OnDamaged` 이벤트 구독)

### 인터페이스 계약
- `HealthComponent.TakeDamage(float amount)` — 피해 적용
- `HealthComponent.isInvincible` — 무적 플래그 (플레이어 이동이 설정)
- `HealthComponent.OnDamaged` — `Action<float, float>` (damage, currentHP)
- `HealthComponent.OnDeath` — `Action<float>` (xpReward, 적의 경우)

## Tuning Knobs

| 변수 | 기본값 | 안전 범위 | 설명 |
|------|--------|----------|------|
| 플레이어 `maxHP` | 100 | 50 ~ 200 | 높을수록 실수 허용, 낮을수록 긴장감 증가 |
| 적(추적형) `maxHP` | 30 | 10 ~ 80 | 낮을수록 빠르게 처치, 너무 낮으면 스킬이 의미 없음 |
| 적(돌진형) `maxHP` | 50 | 20 ~ 120 | 추적형보다 높아야 위협감 유지 |

## Visual/Audio Requirements

- 플레이어 피격 시: 화면 가장자리 빨간 비네트 효과 (선택적, MVP 후순위)
- 적 피격 시: 피격 색 변화 (흰색 플래시 1프레임) — 단순하지만 타격감에 효과적
- 적 사망 시: 오브젝트 비활성화 (즉시 또는 0.1초 딜레이 후)

## UI Requirements

- **플레이어 체력바**: HUD 시스템에서 구현 (HealthComponent.OnDamaged 구독)
- **적 체력바**: 적 오브젝트에 World Space Canvas로 부착, 머리 위 표시
  - 체력 100% 시 숨김, 피격 시 표시
  - 너비: 피격 후 `currentHP / maxHP` 비율로 조정

## Acceptance Criteria

- [ ] 플레이어가 적에게 맞으면 HP가 감소하고 HUD 체력바가 업데이트됨
- [ ] 플레이어 HP가 0이 되면 GameOver 상태로 전환됨
- [ ] 적이 충분한 피해를 받으면 사망하고 씬에서 제거됨
- [ ] 적 사망 시 경험치 이벤트가 발생함
- [ ] 대시 중(무적 프레임) 피해를 받아도 HP가 감소하지 않음
- [ ] 같은 적에게 OnDeath가 두 번 발생하지 않음
- [ ] 적 체력바가 피격 시 표시되고 올바른 비율로 줄어듦

## Open Questions

- **플레이어 최대 체력 증가**: 무기 선택 시 maxHP +20 같은 옵션을 추가할 수 있음. Vertical Slice에서 검토.
