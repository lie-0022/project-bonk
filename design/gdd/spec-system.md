# 스펙 시스템 (Spec System)

> **Status**: Designed
> **Author**: 임예준
> **Last Updated**: 2026-04-20 (패시브 MVP 확정)
> **Implements Pillar**: 빌드 정체성, 파워 에스컬레이션

## Overview

스펙 시스템은 레벨업·상자 선택지에서 **패시브** 형태로 등장하는 강화 시스템이다. 무기와 동일한 선택지 풀에서 등급에 따라 차등 노출되며, 생존·공격·이동 등 다양한 스탯을 강화한다. 매 런마다 다른 패시브 조합이 다른 플레이 스타일을 만드는 빌드 정체성의 핵심 축이다.

## Player Fantasy

레벨업 선택지에 무기 대신 책이 등장하는 순간 — "이번엔 생존력을 올릴까, 아니면 공격에 집중할까"를 고민하는 그 순간이 매 런을 다르게 만든다.

## Detailed Design

### Core Rules

1. 레벨업 또는 상자 선택지에서 무기와 동일한 풀에서 책(스펙) 형태로 등장
2. 스펙 슬롯 수 3개, 최대 레벨 15 (무기 슬롯과 동일)
3. 중복 선택 시 해당 스펙 강화 (레벨 +1)
4. 스테이지 클리어 시 모든 스펙 초기화
5. Playing 외 상태에서는 스펙 효과 중 타이머 기반 항목 정지

### 패시브 항목 목록

#### MVP 구현 (12개)

**최대 레벨**: 모든 패시브 **Lv 15** (무기와 통일). MVP 패시브 **12종**.

**생존 계열**

| 패시브 | 영향 시스템 | 공식 | Lv5 | Lv15 |
|--------|------------|------|-----|------|
| 최대 HP | HealthComponent | +15 HP/lvl (가산) | +75 | +225 |
| HP 회복 | HealthComponent | +0.5 HP/s/lvl (가산) | +2.5/s | +7.5/s |
| 회피 | HealthComponent | DR cap 60%, per-lvl 8% | 23.6% | 41.0% |

**공격 계열**

| 패시브 | 영향 시스템 | 적용 무기 | 공식 | Lv5 | Lv15 |
|--------|------------|----------|------|-----|------|
| 공격 속도 | WeaponSystem | 전체 | `interval × 0.95^lvl, floor 0.3` | ×0.77 | ×0.46 |
| 치명타 확률 | WeaponSystem | 전체 | DR cap 75%, per-lvl 8% | 29.5% | 51.3% |
| 치명타 데미지 | WeaponSystem | 전체 | +15% 배율/lvl | +75% | +225% |
| 생명 흡수 | WeaponSystem + HealthComponent | 전체 | +2%/lvl, cap 25% | 10% | 25% (cap) |

**투사체/공격 계열**

| 패시브 | 영향 시스템 | 적용 무기 | 공식 | Lv5 | Lv10 | Lv15 |
|--------|------------|----------|------|-----|------|------|
| 발사체 수 | WeaponSystem | 전체 (검=연속 휘두르기) | `floor(lvl/3)` | +1 | +3 | +5 |

> **발사체 속도**는 별도 패시브가 아닌 **공격 속도 패시브의 파생 효과**다. 공격 속도가 X배 빨라지면 발사체 이동 속도/검 휘두르기 속도도 X배 빨라진다. (`ProjectileSpeed = 1 / AttackSpeedMultiplier`)

**이동 계열**

| 패시브 | 영향 시스템 | 공식 | Lv5 | Lv15 |
|--------|------------|------|-----|------|
| 이동 속도 | PlayerController | +1.0/lvl 유닛/초 | +5.0 | +15.0 |
| 추가 점프 | PlayerController | +1회/lvl | +5 | +15 |

**기타**

| 패시브 | 영향 시스템 | 공식 | Lv5 | Lv15 |
|--------|------------|------|-----|------|
| 행운 | LevelupSystem | DR cap 80%, per-lvl 10% | 32.8% | 64.5% |
| 난이도 | WaveSpawner | 스폰 +15%/lvl, 보상 +20%/lvl | +75%/+100% | +225%/+300% |

> **DR (Diminishing Returns) 공식**: `final = cap × (1 - (1 - perLevel)^level)` — 확률 패시브가 100%를 넘지 않도록 점감 적용.

#### 나중에 구현 (추후)

| 패시브 | 분류 | 비고 |
|--------|------|------|
| 오버힐 | 생존 | HP 초과 체력 — 복잡도 높음 |
| 쉴드 | 생존 | 피해 흡수 보호막 — 복잡도 높음 |
| 생명 흡수 | 공격 | 처치/적중 시 HP 회복 |
| 넉백 | 공격 | 공격 시 적 후퇴 |
| 엘리트 데미지 | 공격 | 보스/엘리트 전용 — 보스 시스템 이후 |
| 가시 데미지 | 공격 | 피격 반사 데미지 |
| 발사체 반사 | 투사체 | 투사체 반사 — 복잡도 높음 |
| 지속 시간 | 투사체 | 해당 무기 추가 시 |
| 점프 높이 | 이동 | 추가 점프와 함께 |

> 패시브 슬롯 수 3개, 최대 레벨 **15** — 무기 슬롯과 동일.

### Interactions with Other Systems

- **← 레벨업·상자 선택**: 선택지 풀에 스펙 항목 포함
- **→ 플레이어 스탯**: 스펙 효과를 PlayerStats 또는 각 시스템에 직접 적용
- **← 게임 상태 관리**: 스테이지 클리어 시 초기화

## Formulas

```
최대 HP:        PlayerStats.MaxHp += 15 * level
HP 회복:        PlayerStats.HpRegen += 0.5f * level  (초당)

회피 (DR):      dodgeChance = 0.60f * (1f - Mathf.Pow(1f - 0.08f, level))
                if (Random.value < dodgeChance) damage = 0

공격 속도:      mult = Mathf.Max(Mathf.Pow(0.95f, level), 0.30f)
                weapon.AttackInterval = baseInterval * mult

치명타 확률 (DR): critChance = 0.75f * (1f - Mathf.Pow(1f - 0.08f, level))
                if (Random.value < critChance) isCrit = true
치명타 데미지:  critDamage = baseDamage * (1f + 0.15f * level)
생명 흡수:      lifestealRatio = Mathf.Min(0.02f * level, 0.25f)
                healAmount = dealtDamage * lifestealRatio

발사체 수 (검): swingCount = 1 + Mathf.FloorToInt(level / 3f)
발사체 수 (총/마법): projectileCount = 1 + Mathf.FloorToInt(level / 3f)

// 발사체 속도는 AttackSpeed의 파생값
projectileSpeed = 1f / attackSpeedMultiplier
검 휘두르기 속도 *= projectileSpeed

이동 속도:      PlayerStats.MoveSpeed += 1.0f * level
추가 점프:      PlayerStats.ExtraJumps += level

행운 (DR):      epicChance = 0.80f * (1f - Mathf.Pow(1f - 0.10f, level))
                // 레벨업 선택지 고등급 가중치
난이도:         WaveSpawner.SpawnRateMultiplier += 0.15f * level
                XP/골드 보상 *= (1f + 0.20f * level)
```

### DR (Diminishing Returns) 공식 설명

```
final = cap × (1 - (1 - perLevel)^level)
```

- `cap`: 절대 상한 (회피 60%, 크리 75%, 행운 80%)
- `perLevel`: 레벨당 비율 (회피 0.08, 크리 0.08, 행운 0.10)
- `level`: 현재 패시브 레벨 (1~15)

레벨이 올라갈수록 증가폭이 줄어들지만, 절대로 cap을 넘지 않는다. Lv∞에서도 cap에 점근.

## Edge Cases

- **스테이지 초기화**: 모든 스펙 효과 제거, 스탯 원복
- **선택지 0개**: 무기 최대 + 스펙 슬롯 가득 + 모든 항목 최대 레벨 → 즉시 재개

## Dependencies

### 업스트림
- 레벨업·상자 선택 시스템
- 게임 상태 관리 (스테이지 초기화)

### 다운스트림
- 플레이어 컨트롤러, 무기 시스템, 체력 시스템 (스펙 효과 수신)

## Tuning Knobs

| 변수 | 기본값 | 설명 |
|------|--------|------|
| 패시브 슬롯 수 | 3 | 무기 슬롯과 동일 |
| 패시브 최대 레벨 | 15 | 무기 최대 레벨과 동일 |
| 공속 감소율 | 5%/레벨 (곱셈) | floor 0.30 (70% 감소가 한계) |
| 회피 cap | 60% | DR 적용, 항상 cap 미만 |
| 치명타 확률 cap | 75% | DR 적용 |
| 행운 cap | 80% | DR 적용 |
| 생명 흡수 cap | 25% | 가산식, 12레벨에서 cap 도달 |
| 발사체 수 증가 | 3레벨당 +1 | Lv15 = +5 |
| 난이도 스폰 증가 | 15%/레벨 | Lv15 = 스폰량 ×3.25 |

> **제외된 패시브**: 아머, 크기 — 무기 차별성 강화를 위해 MVP에서 제외

## Acceptance Criteria

- [ ] 레벨업 선택지에 무기와 함께 스펙(책)이 혼합되어 등장함
- [ ] 스펙 선택 시 해당 스탯이 즉시 적용됨
- [ ] 스테이지 클리어 시 모든 스펙이 초기화됨
- [ ] 동일 스펙 중복 선택 시 레벨이 올라가고 효과가 강화됨

## Open Questions

- 각 스펙 세부 수치 — 플레이테스트 후 밸런싱
- 등급(Common/Epic/Unique/Legend)별 패시브 효과 차등 — `levelup-selection.md`에 명시. 무기와 동일 패턴 적용 검토 (Lv 가속).
