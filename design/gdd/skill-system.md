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
5. **최대 강화**: 무기당 최대 강화 레벨 `maxWeaponLevel`(5). 이미 최대 레벨인 무기는 선택지에서 제외
6. **슬롯 가득 참**: 3개 슬롯이 모두 차면 기존 무기 강화 선택만 제공
7. **스테이지 초기화**: 스테이지 클리어 후 다음 스테이지로 넘어갈 때 모든 무기 초기화
8. **게임 상태**: Playing이 아닐 때 모든 무기 발동 정지

### 무기 목록 (MVP)

| 무기 | 패턴 | 피해 | 간격 | 범위 | 등급 |
|------|------|------|------|------|------|
| **검** | 전방 부채꼴 근접 공격 | 15 | 1.0초 | 반경 3, 각도 90° | 커먼 |
| **총** | 가장 가까운 적에게 투사체 발사 | 20 | 0.8초 | 사거리 15 | 커먼 |
| **마법** | 플레이어 주변 범위 폭발 또는 투사체 (TBD) | TBD | TBD | TBD | 에픽 |

> 상세 무기 수치·강화 단계는 별도 데이터 시트(Data/Weapons/)에서 관리 (TBD)

### 무기 강화 (레벨업)

강화 선택 시 아래 두 옵션 중 하나를 선택한다 (최대 Lv 5):

| 옵션 | 효과 |
|------|------|
| 데미지 강화 | 피해량 +25% |
| 공격속도 강화 | 공격 간격 -20% |

> 강화 선택 UI는 레벨업 선택지 카드에 옵션 두 개로 표시. 구체적 UX는 선택 UI GDD 참조.

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
| `maxWeaponLevel` | 5 | 3 ~ 7 | 높을수록 강화 폭이 크고 파워 에스컬레이션 강함 |
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
