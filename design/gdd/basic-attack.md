# 기본 공격 (Basic Attack)

> **Status**: Designed
> **Author**: 사용자 + Claude
> **Last Updated**: 2026-04-17
> **Implements Pillar**: 파워 에스컬레이션

## Overview

기본 공격 시스템은 플레이어 입력 없이 가장 가까운 적을 주기적으로 자동 공격하는 시스템이다. 뱀파이어 서바이벌의 자동 무기 방식을 차용해 플레이어가 이동과 회피에 집중할 수 있도록 한다. 기본 공격은 캐릭터마다 고유한 패턴을 가지며, 무기 획득 시스템으로 추가되는 무기들과 함께 빌드를 구성하는 기반이 된다.

## Player Fantasy

이동하고 대시하는 동안 캐릭터가 알아서 주변 적을 때리고 있다. 기본 공격만으로는 한계가 있지만, 무기가 추가될수록 동시에 여러 패턴이 발동되며 "내 캐릭터가 쉬지 않고 싸우고 있다"는 느낌을 준다.

## Detailed Design

### Core Rules

1. 기본 공격은 플레이어 입력 없이 일정 주기(`attackInterval`)마다 자동 발동된다
2. 공격 대상: 플레이어로부터 `attackRange` 내에 있는 가장 가까운 적 1명
3. 범위 내 적이 없으면 공격하지 않는다 (타이머는 계속 진행)
4. 공격 시 `DamageDealer.Deal()`로 대상에게 피해를 준다
5. 게임 상태가 Playing이 아닐 때는 자동 공격이 멈춘다
6. 기본 공격은 무기 발동에 영향을 주지 않는다 (완전히 독립적)

### States and Transitions

| 상태 | 조건 | 동작 |
|------|------|------|
| Active | Playing 상태 | 타이머 카운트다운, 범위 내 적 자동 공격 |
| Waiting | 공격 후 쿨타임 | 다음 공격까지 대기 |
| Disabled | 비Playing 상태 | 타이머 정지, 공격 없음 |

### Interactions with Other Systems

- **← 게임 상태 관리**: Playing 상태에서만 동작
- **→ 데미지 시스템**: `DamageDealer.Deal()` 호출
- **← 무기 시스템**: 공격 속도 증가 무기 효과가 `attackInterval` 감소 가능 (향후)

## Formulas

```
타이머 업데이트:
  attackTimer -= Time.deltaTime
  if attackTimer <= 0:
    TryAttack()
    attackTimer = attackInterval

가장 가까운 적 탐색:
  enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None)
  nearest = enemies.Where(e => distance(e) <= attackRange)
                   .OrderBy(e => distance(e))
                   .FirstOrDefault()

기본값:
  attackDamage   = 10
  attackInterval = 1.0초  // 초당 1회
  attackRange    = 5.0    // 유닛
```

## Edge Cases

- **공격 대상이 공격 직전 사망**: `target.isAlive == false` 검사 후 공격 취소, 다음 가장 가까운 적으로 재탐색
- **범위 내 적 없음**: 공격 없이 타이머만 리셋
- **대시 중 자동 공격**: 대시 중에도 자동 공격은 계속 발동됨 (대시와 독립적)
- **다수 적이 동일 거리**: `OrderBy` 정렬 결과 첫 번째 적 선택 (결정론적)

## Dependencies

### 업스트림
- **데미지 시스템**: `DamageDealer.Deal()` 인터페이스
- **게임 상태 관리**: Playing 상태 여부

### 다운스트림
- 무기 시스템: 공격 속도 버프 가능성 (향후)

### 인터페이스 계약
- `BasicAttack.attackInterval` — 무기 시스템에서 수정 가능한 공개 필드

## Tuning Knobs

| 변수 | 기본값 | 안전 범위 | 설명 |
|------|--------|----------|------|
| `attackDamage` | 10 | 5 ~ 20 | 낮을수록 무기 의존도 증가 |
| `attackInterval` | 1.0초 | 0.3 ~ 2.0초 | 짧을수록 기본 공격이 강해짐 |
| `attackRange` | 5.0 | 3.0 ~ 8.0 | 넓을수록 이동 중에도 공격 가능 |

## Visual/Audio Requirements

- 공격 시: 짧은 검 휘두르기 애니메이션 또는 이펙트 (MVP에서는 생략 가능)
- 히트 시: 타격 이펙트 (데미지 시스템에서 공통 처리)

## UI Requirements

- 없음 (기본 공격은 HUD에 별도 표시 없음)

## Acceptance Criteria

- [ ] 범위 내 적이 있을 때 `attackInterval`마다 자동으로 피해를 줌
- [ ] 범위 내 적이 없을 때 공격이 발동되지 않음
- [ ] 항상 가장 가까운 적을 우선 공격함
- [ ] 게임 상태가 Playing이 아닐 때 자동 공격이 멈춤
- [ ] 공격 대상이 사망해도 크래시 없이 다음 적으로 전환됨

## Open Questions

- **공격 속도 버프**: 무기 강화 시 `attackInterval` 감소 옵션 추가 검토.
