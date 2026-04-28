# 무기 획득 시스템 (Weapon Acquisition System)

> **Status**: Designed
> **Author**: 사용자 + Claude
> **Last Updated**: 2026-04-17
> **Implements Pillar**: 빌드 정체성, 파워 에스컬레이션

## Overview

무기 획득 시스템은 런 중에 무기를 획득하고 자동 발동시키는 진행 핵심 시스템이다. 플레이어는 레벨업 선택 또는 상자를 통해 새 무기를 추가하거나 기존 무기를 강화한다. 각 무기는 고유한 패턴으로 자동 공격하며, 여러 무기가 동시에 독립적으로 발동된다. 매 런마다 다른 무기 조합을 구성하는 것이 빌드 정체성 필라의 핵심이다.

## Player Fantasy

레벨업 선택으로 무기를 하나씩 추가할수록 화면이 점점 화려해지고, 내 캐릭터 혼자 여러 패턴으로 적을 쓸어버리는 순간이 파워 트립의 절정이다. "이번엔 어떤 무기 조합으로 가볼까"가 런을 반복하게 만드는 핵심 동기다.

## Detailed Design

### Core Rules

1. **무기 슬롯**: 최대 `maxWeaponSlots`(3)개의 무기를 동시에 보유 가능
2. **자동 발동**: 각 무기는 고유한 `attackInterval` 타이머에 따라 독립적으로 자동 발동
3. **대상 탐색**: 각 무기는 자체 `attackRange` 내에서 가장 가까운 적(또는 무기 특성에 따른 대상)을 자동 선택
4. **중복 획득**: 이미 보유한 무기를 다시 선택하면 해당 무기가 강화됨 (데미지 +25% 또는 공격속도 +20% 중 선택)
5. **최대 강화**: 무기당 최대 강화 레벨 `maxWeaponLevel`(15). 이미 최대 레벨인 무기는 선택지에서 제외
6. **슬롯 가득 참**: 3개 슬롯이 모두 차면 기존 무기 강화 선택만 제공
7. **스테이지 초기화**: 스테이지 클리어 후 다음 스테이지로 넘어갈 때 모든 무기 초기화
8. **게임 상태**: Playing이 아닐 때 모든 무기 발동 정지

### 무기 목록 (MVP)

| 무기 | 패턴 | 피해 | 간격 | 범위 | 등급 |
|------|------|------|------|------|------|
| **검** | 전방 부채꼴 근접 공격 | 20 | 1.5초 | 반경 3, 각도 180° | 커먼 |
| **총** | 가장 가까운 적에게 고속 단일 투사체 | 20 | 0.8초 | 사거리 15 | 커먼 |
| **마법** | 가장 가까운 적에게 느린 투사체 → 적중 시 반경 폭발 | 30 | 1.5초 | 사거리 12, 폭발 반경 2 | 에픽 |

> 상세 무기 수치·강화 단계는 별도 데이터 시트(Data/Weapons/)에서 관리 (TBD)

### 무기 강화 (레벨업)

**최대 레벨 15** — 모든 무기 통일.

강화 카드는 무기당 1장(옵션 분기 없음). 카드를 선택하면 해당 무기 레벨이 +1 되고, 레벨에 따라 정의된 효과가 적용된다.

#### 공통 규칙

- **데미지·공속**: 매 레벨업마다 항상 증가. 증가 폭은 카드 등장 시 결정된 **등급**에 따라 차등.
- **범위(사거리·각도 등)**: 무기별 정의된 **마일스톤 레벨(5, 10, 15)**에서만 변화. 등급과 무관.
- **만렙(15)**: 무기마다 정의된 "게임 체인저" 효과 발동.

#### 등급별 데미지·공속 증가 폭 (매 레벨업)

| 등급 | 색상 | 데미지 | 공격 간격 |
|------|------|--------|----------|
| Common | 회색 | +12% | -6% |
| Epic | 보라 | +20% | -10% |
| Unique | 노랑 | +32% | -15% |
| Legend | 빨강 | +50% | -22% |

> 등급별 등장 확률 및 행운 보정은 `levelup-selection.md` 참조.
> 누적은 곱셈(multiplicative). 예: 모두 Common 14회 = 1.12^14 ≈ 4.89× 데미지.

#### 검 (Sword) 범위 마일스톤

| Lv | 각도 | 사거리 배율 | 변화 |
|----|------|-----------|------|
| 1  | 180° | 1.0× (3.0) | 시작 |
| 5  | 240° | 1.4× (4.2) | 1차 확장 |
| 10 | 300° | 1.8× (5.4) | 2차 확장 |
| 15 | **360°** | **2.5× (7.5)** | **만렙 — 광역 폭풍** |

> Lv 2~4 / 6~9 / 11~14 구간은 데미지·공속만 증가. 각도·사거리 동결.

#### 총 (Gun) 범위·관통 마일스톤

| Lv | 사거리 배율 | 관통 횟수 | 변화 |
|----|-----------|----------|------|
| 1  | 1.0× (15) | 0 (1명 명중 후 소멸) | 시작 |
| 5  | 1.3× (19.5) | 1 (2명 관통) | 1차 확장 |
| 10 | 1.6× (24) | 2 (3명 관통) | 2차 확장 |
| 15 | **2.0× (30)** | **무한 관통** | **만렙 — 라인 클리어** |

> 관통 = 투사체가 적 명중 후 소멸하지 않고 통과. 정체성: "단일 고속 저격 → 만렙에서 직선 라인 청소".
> Lv 2~4 / 6~9 / 11~14 구간은 데미지·공속만 증가. 사거리·관통 동결.

#### 마법 (Magic) 동작 및 범위 마일스톤

**동작**: 가장 가까운 적 방향으로 느린 투사체 발사 → 적중 시 **반경 R 폭발**(범위 내 모든 적 피해). 검(근접)/총(직선 고속)과 차별화된 광역 정체성.

| Lv | 폭발 반경 | 사거리 배율 | 변화 |
|----|---------|-----------|------|
| 1  | 2.0 | 1.0× (12) | 시작 |
| 5  | 3.0 | 1.17× (14) | 1차 확장 |
| 10 | 4.5 | 1.33× (16) | 2차 확장 |
| 15 | **7.0** | **1.67× (20)** | **만렙 — 화면 청소급 폭발** |

> Lv 2~4 / 6~9 / 11~14 구간은 데미지·공속만 증가. 폭발 반경·사거리 동결.
> 폭발 반경은 패시브 "크기"와 무관 (크기 패시브는 MVP 제외).

### States and Transitions

각 무기 슬롯의 독립적 상태:

| 상태 | 조건 | 동작 |
|------|------|------|
| Empty | 미획득 | 비활성 |
| Active | 획득 + 쿨타임 0 + 범위 내 적 있음 | 발동 |
| Cooldown | 발동 후 | 쿨타임 카운트다운 |
| Disabled | 비Playing 상태 | 타이머 정지 |

### Interactions with Other Systems

- **← 레벨업 무기 선택**: `WeaponSystem.AddWeapon(weaponType)` 또는 `WeaponSystem.UpgradeWeapon(weaponType)` 호출
- **→ 데미지 시스템**: 무기 히트 시 `DamageDealer.Deal()` 호출
- **← 게임 상태 관리**: Playing 외 상태에서 발동 차단
- **→ HUD 시스템**: 보유 무기 목록, 각 무기의 쿨타임 진행률 제공

## Formulas

```
무기 타이머 업데이트:
  foreach weapon in _equippedWeapons:
    if weapon.cooldownRemaining > 0:
      weapon.cooldownRemaining -= Time.deltaTime

무기 발동 조건:
  if weapon.cooldownRemaining <= 0 && gameState == Playing:
    target = FindNearestEnemy(weapon.attackRange)  // 무기 패턴에 따라 다름
    if target != null:
      ExecuteWeapon(weapon)
      weapon.cooldownRemaining = weapon.attackInterval

강화 수치 계산:
  finalDamage   = weapon.baseDamage   * weapon.damageMultiplier[level]
  finalInterval = weapon.baseInterval * weapon.intervalMultiplier[level]
  finalRange    = weapon.baseRange    * weapon.rangeMultiplier[level]

HUD 쿨타임용:
  weapon.CooldownNormalized = weapon.cooldownRemaining / weapon.attackInterval
  // 0 = 준비됨, 1 = 방금 발동
```

## Edge Cases

- **슬롯 가득 참 + 모든 무기 최대 레벨**: 레벨업 시 선택지 없이 즉시 재개 (XP 이월)
- **공격 대상이 발동 직전 사망**: 타겟 유효성 재확인 후 다음 가장 가까운 적으로 전환
- **여러 무기 동시 발동**: 각 무기 타이머가 같은 프레임에 만료되면 순서대로 모두 발동
- **범위 내 적 없음**: 타이머 리셋 없이 대기 (다음 프레임에 재시도)
- **스테이지 초기화 시**: 모든 무기 슬롯 비우고 타이머 리셋

## Dependencies

### 업스트림
- **데미지 시스템**: `DamageDealer.Deal()` 인터페이스
- **레벨업 무기 선택**: `AddWeapon()` / `UpgradeWeapon()` 호출
- **게임 상태 관리**: Playing 상태 여부

### 다운스트림
- HUD 시스템 (무기 목록, 쿨타임 값 제공)

### 인터페이스 계약
- `WeaponSystem.AddWeapon(WeaponType type)` — 새 무기 추가
- `WeaponSystem.UpgradeWeapon(WeaponType type)` — 기존 무기 강화
- `WeaponSystem.GetEquippedWeapons()` — 보유 무기 목록 (HUD용)
- `WeaponSystem.GetCooldownNormalized(WeaponType type)` — 0~1, HUD 쿨타임용
- `WeaponSystem.HasWeapon(WeaponType type)` — 보유 여부 (레벨업 선택지 생성용)
- `WeaponSystem.GetWeaponLevel(WeaponType type)` — 강화 레벨 (선택지 표시용)

## Tuning Knobs

| 변수 | 기본값 | 안전 범위 | 설명 |
|------|--------|----------|------|
| `maxWeaponSlots` | 3 | 2 ~ 4 | 많을수록 빌드 다양성 증가, 성능 주의 |
| `maxWeaponLevel` | 15 | 10 ~ 20 | 모든 무기 통일. 데미지·공속 14회 증가 + 범위 3회 마일스톤 |
| 기본검 `attackInterval` | 1.0초 | 0.5 ~ 2.0초 | |
| 마법 볼트 `attackInterval` | 0.8초 | 0.4 ~ 1.5초 | |

## Visual/Audio Requirements

- 각 무기 발동 시 고유 이펙트 (MVP에서는 히트 이펙트만 유지, 발동 이펙트는 후순위)
- 무기 획득 시: 등급별 획득 연출 (등급이 높을수록 화려한 연출)

## UI Requirements

- **무기 슬롯 HUD**: 보유 무기 최대 3개 표시, 각 슬롯에 무기 아이콘 + 쿨타임 오버레이 + 등급 색상 테두리
- 미획득 슬롯: 회색 빈 슬롯

## Acceptance Criteria

- [ ] 무기 획득 시 해당 무기가 자동으로 독립 타이머를 시작해 발동됨
- [ ] 여러 무기가 동시에 각자의 패턴으로 발동됨
- [ ] 중복 획득 시 무기 레벨이 증가하고 수치가 변화됨
- [ ] `maxWeaponSlots`(6)개 초과 획득 시도 시 무시됨
- [ ] 게임 상태가 Playing이 아닐 때 모든 무기 발동이 정지됨
- [ ] 스테이지 초기화 시 모든 무기가 초기화됨
- [ ] HUD에 보유 무기와 쿨타임이 올바르게 표시됨

## Open Questions

- **무기 간 시너지**: 특정 무기 조합 시 보너스 효과. Alpha 이후 검토.
- **캐릭터별 무기 제한**: 검사는 근접 무기 위주, 마법사는 투사체 위주로 선택지를 제한할지 여부. Vertical Slice에서 검토.
