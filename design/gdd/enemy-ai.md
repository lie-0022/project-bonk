# 적 AI (Enemy AI)

> **Status**: Designed
> **Author**: 사용자 + Claude
> **Last Updated**: 2026-03-26
> **Implements Pillar**: 파워 에스컬레이션

## Overview

적 AI 시스템은 플레이어를 향해 끊임없이 접근하는 두 종류의 적(추적형, 돌진형)을 제어한다. NavMesh 없이 단순 벡터 연산으로 플레이어 방향으로 직선 이동하며, 장애물이 있어도 타고 넘어오는 방식으로 처리한다. 단순하지만 대규모 적 집단을 빠르게 구현하고 성능을 유지하는 데 최적이다.

## Player Fantasy

수십 마리의 적이 사방에서 밀려오는 압박감. 추적형은 끊임없이 따라오고, 돌진형은 갑자기 속도를 올려 돌진한다. 이 혼돈 속에서 대시와 무기로 살아남는 것이 뱀서라이크의 핵심 긴장감이다.

## Detailed Design

### Core Rules

**공통 규칙 (모든 적)**
1. 매 프레임 플레이어 `Transform.position`을 목표로 설정
2. 이동 방향: `(player.position - self.position).normalized`
3. 이동: `transform.position += moveDir * moveSpeed * Time.deltaTime` (물리 없이 직접 이동)
4. 장애물 처리: 별도 충돌 회피 없음 — 지형 위를 타고 넘어오는 방식 (`CharacterController` 또는 콜라이더 설정으로 Y축 이동 허용)
5. 플레이어와 접촉(콜라이더 겹침) 시 `DamageDealer.Deal()`로 접촉 피해 적용
6. 게임 상태가 Playing이 아닐 때 모든 이동·공격 정지
7. 사망 시: `HealthComponent.OnDeath` 이벤트 발생 → 오브젝트 풀에 반환

**추적형 (Chaser)**
- 일정한 속도로 플레이어를 향해 직선 이동
- 접촉 피해: 초당 `chaserContactDamage` (지속 피해)
- 체력이 낮아 빠르게 처치 가능하지만 숫자가 많음

**돌진형 (Rusher)**
- 평소에는 천천히 이동하다가 `chargeWindupTime` 후 짧게 정지 → 고속 돌진
- 돌진 방향: 돌진 시작 시점의 플레이어 방향으로 고정 (돌진 중 방향 변경 없음)
- 돌진 피해: 충돌 시 1회 `rusherChargeDamage`
- 돌진 후 `rushCooldown`만큼 대기 → 다시 추적 시작

### States and Transitions

**추적형 상태**

| 상태 | 동작 | 전환 조건 |
|------|------|----------|
| Chasing | 플레이어 방향으로 이동 | 항상 유지 |
| Dead | 이동 정지, 풀 반환 | `currentHP <= 0` |

**돌진형 상태**

| 상태 | 동작 | 전환 조건 |
|------|------|----------|
| Approaching | 느린 속도로 플레이어 추적 | 기본 상태 |
| WindingUp | 정지 후 돌진 예고 (경고 이펙트) | `chargeTimer >= chargeWindupTime` |
| Charging | 고속 직선 돌진 (방향 고정) | WindingUp 완료 |
| Recovering | 돌진 후 잠시 정지 | 돌진 완료 또는 벽 충돌 |
| Dead | 이동 정지, 풀 반환 | `currentHP <= 0` |

### Interactions with Other Systems

- **← 플레이어 이동**: `player.Transform.position` 읽어 목표 방향 계산
- **← 게임 상태 관리**: Playing 외 상태에서 AI 정지
- **→ 데미지 시스템**: 접촉/충돌 피해 `DamageDealer.Deal()` 호출
- **→ 체력 시스템**: 피격 시 `HealthComponent.TakeDamage()` 수신
- **→ 경험치 시스템**: `OnDeath(xpReward)` 이벤트 발생
- **← 웨이브 스폰**: 스폰 시 초기화 (`Init()`), 사망 시 풀 반환

## Formulas

```
추적형 이동:
  moveDir = (player.position - self.position).normalized
  transform.position += moveDir * chaserMoveSpeed * Time.deltaTime

돌진형 Charge 발동:
  chargeTimer += Time.deltaTime
  if chargeTimer >= chargeWindupTime:
    chargeDir = (player.position - self.position).normalized  // 방향 고정
    state = Charging

돌진형 이동 (Charging):
  transform.position += chargeDir * rusherChargeSpeed * Time.deltaTime
  chargeDistanceTraveled += rusherChargeSpeed * Time.deltaTime
  if chargeDistanceTraveled >= maxChargeDistance: state = Recovering

접촉 피해 (추적형):
  if isContactingPlayer:
    contactDamageTimer -= Time.deltaTime
    if contactDamageTimer <= 0:
      DamageDealer.Deal(chaserContactDamage, player.healthComponent)
      contactDamageTimer = contactDamageInterval

기본값:
  추적형 moveSpeed          = 3.5
  추적형 contactDamage      = 8 (초당)
  추적형 contactInterval    = 0.5초 (0.5초마다 8 피해)
  추적형 maxHP              = 30
  추적형 xpReward           = 10

  돌진형 approachSpeed      = 2.0
  돌진형 chargeWindupTime   = 2.5초
  돌진형 chargeSpeed        = 15.0
  돌진형 maxChargeDistance  = 8.0
  돌진형 rushCooldown       = 3.0초
  돌진형 chargeDamage       = 20 (충돌 1회)
  돌진형 maxHP              = 50
  돌진형 xpReward           = 20
```

## Edge Cases

- **적끼리 겹침**: 다수 적이 같은 위치에 쌓이는 현상 → MVP에서는 허용 (Separation Steering은 Vertical Slice에서 추가)
- **플레이어 사망 후 AI 동작**: 게임 상태가 GameOver로 전환되면 즉시 모든 AI 정지
- **돌진형 돌진 중 플레이어 사망**: 돌진 완료 후 Recovering 상태로 전환, AI는 정지
- **장애물 Y축 이동**: 경사면을 오를 때 Y축이 튀는 현상 → `CharacterController` 또는 콜라이더의 `slopeLimit` 설정으로 완화
- **풀 반환 전 중복 사망**: `isAlive` 플래그로 `OnDeath` 중복 발생 방지
- **스폰 직후 즉시 피격**: 스폰 후 0.5초간 무적 (`spawnInvincibility`) 적용 (스폰 위치에서 즉사 방지)

## Dependencies

### 업스트림
- **플레이어 이동**: `player.Transform.position` 참조
- **게임 상태 관리**: Playing 상태 여부
- **체력 시스템**: `HealthComponent` 피격 처리
- **데미지 시스템**: `DamageDealer.Deal()` 호출

### 다운스트림
- 웨이브 스폰 (스폰·반환 관리)
- 경험치 시스템 (`OnDeath` 이벤트 구독)

### 인터페이스 계약
- `EnemyAI.Init()` — 풀에서 꺼낼 때 상태 초기화
- `EnemyAI.OnDeath(float xpReward)` — 사망 이벤트

## Tuning Knobs

| 변수 | 기본값 | 안전 범위 | 설명 |
|------|--------|----------|------|
| 추적형 `moveSpeed` | 3.5 | 1.5 ~ 6.0 | 높을수록 이동 실력이 중요 |
| 추적형 `contactDamage` | 8/초 | 3 ~ 15/초 | 높을수록 접촉 위험 |
| 돌진형 `chargeWindupTime` | 2.5초 | 1.0 ~ 4.0초 | 짧을수록 회피가 어려움 |
| 돌진형 `chargeSpeed` | 15.0 | 8.0 ~ 25.0 | 높을수록 회피 타이밍이 중요 |
| 돌진형 `chargeDamage` | 20 | 10 ~ 40 | 높을수록 돌진 회피 페널티 |

## Visual/Audio Requirements

- 추적형: 단순 캡슐(빨간색) — MVP
- 돌진형: 단순 캡슐(주황색) + WindingUp 시 색상 변화(흰색 → 노란색) 경고
- 돌진형 돌진: 빠른 이동 중 잔상 이펙트 (선택적)

## UI Requirements

- 적 체력바: `HealthComponent`의 `OnDamaged` 이벤트로 업데이트 (체력 시스템 GDD 참조)

## Acceptance Criteria

- [ ] 추적형이 매 프레임 플레이어 방향으로 이동함
- [ ] 추적형이 플레이어와 접촉 시 0.5초마다 8 피해를 줌
- [ ] 돌진형이 2.5초 대기 후 경고 → 플레이어 방향으로 고속 돌진함
- [ ] 돌진형 돌진이 플레이어와 충돌 시 20 피해를 1회 줌
- [ ] 적 사망 시 올바른 xpReward 이벤트가 발생함
- [ ] 게임 상태가 Playing이 아닐 때 적이 완전히 멈춤
- [ ] 동일 적에서 `OnDeath`가 두 번 발생하지 않음

## Open Questions

- **Separation Steering**: 적끼리 겹치지 않도록 밀어내는 로직. MVP에서는 생략, Vertical Slice에서 추가.
- **웨이브 후반 속도 증가**: 웨이브 번호에 따라 `moveSpeed` 증가. 웨이브 스폰 시스템 설계 시 반영.
