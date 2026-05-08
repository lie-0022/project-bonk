# 등급 시스템 (Grade System)

> **Status**: Designed (역공학)
> **Author**: 사용자 + Claude
> **Last Updated**: 2026-05-08
> **Implements Pillar**: 빌드 다양성, 럭키 모먼트, 행운 패시브 가치

## Overview

등급 시스템은 레벨업·상자 카드 등장 시 각 카드의 **희귀도(grade)**를 결정하고, 등급에 따라
무기 강화량과 패시브 가중치를 차등 적용하는 진행 시스템이다. 4단계 등급(Common/Epic/Unique/Legend)으로
구성되며, 행운(LuckChance) 패시브가 상위 등급 출현 확률을 끌어올린다.
가챠 단계와 효과 단계를 분리해 같은 무기라도 등급이 다르면 강화 폭이 달라진다.

## Player Fantasy

레벨업 카드가 펼쳐지는 순간 등급 색상(흰/파/보/금)을 보는 그 짧은 두근거림이 핵심이다.
"커먼만 3장이면 한숨, 레전드 1장이 섞이면 이번 빌드가 산다" 라는 *런 차원의 운명감*을 만든다.
행운 패시브를 누적할수록 이 두근거림의 빈도가 늘어나, 빌드 자체가 운영 전략이 된다.

## Detailed Rules

1. 등급 4종: `Common (0)`, `Epic (1)`, `Unique (2)`, `Legend (3)` (열거형)
2. 카드 등장 시마다 `CardGradeRoller.Roll()`이 호출되어 등급을 1개 추첨
3. 기본 등장 확률 (LuckChance = 0):
   - Common 60%, Epic 25%, Unique 12%, Legend 3%
   - 합 = 1.0
4. 행운 보정: `PlayerStats.LuckChance` (0~0.80 범위) 만큼 Common 비중을 상위 등급에 재분배
   - Common 비중이 `BaseCommon × LuckChance` 만큼 감소
   - 감소분(`shifted`)은 상위 3등급(Epic/Unique/Legend)에 *기본 비율대로* 분배
   - 즉 Epic/Unique/Legend 간 상대 비율은 유지되며 "더 자주 등장"하는 효과
5. 추첨된 등급에 따라 적용되는 효과:
   - **무기 카드**: 데미지 보너스 + 공격 간격 감소 (등급별 차등)
   - **패시브 카드**: 레벨은 항상 +1이지만 *수치 환산용 가중치* 차등 (등급별)
6. 등급 보너스 표 (`GradeBonus.Get(grade)`):
   - Common: +12% damage, -6% interval
   - Epic: +20% damage, -10% interval
   - Unique: +32% damage, -15% interval
   - Legend: +50% damage, -22% interval
7. 패시브 가중치 (`GradeBonus.PassiveWeight(grade)`):
   - Common 1.0 / Epic 1.5 / Unique 2.5 / Legend 4.0
8. UI는 등급별 색상으로 카드 프레임을 표시 (Common 흰색, Epic 청색, Unique 보라색, Legend 금색 — 색상 정의는 HUD/UI 문서 참조)

## Formulas

```
가중치 계산 (LuckChance L 입력, 0 ≤ L ≤ 1):
  L = clamp01(L)

  commonW = BaseCommon × (1 - L)
  shifted = BaseCommon × L
  upperTotal = BaseEpic + BaseUnique + BaseLegend  // = 0.40

  epicW   = BaseEpic   + shifted × (BaseEpic   / upperTotal)
  uniqueW = BaseUnique + shifted × (BaseUnique / upperTotal)
  legendW = BaseLegend + shifted × (BaseLegend / upperTotal)

추첨 (가중치 합산 후 균등 난수):
  total = commonW + epicW + uniqueW + legendW
  roll = Random.value × total
  if roll < commonW           → Common
  elif roll < commonW+epicW   → Epic
  elif roll < commonW+epicW+uniqueW → Unique
  else                        → Legend

기본 상수:
  BaseCommon = 0.60
  BaseEpic   = 0.25
  BaseUnique = 0.12
  BaseLegend = 0.03

확률 변화 예시:
  L=0.00: C=60.0% E=25.0% U=12.0% L= 3.0%
  L=0.25: C=45.0% E=34.4% U=16.5% L= 4.1%
  L=0.50: C=30.0% E=43.8% U=21.0% L= 5.3%
  L=0.80: C=12.0% E=55.0% U=26.4% L= 6.6%

무기 등급 효과 적용 (현재 레벨 기준):
  finalDamage   = baseDamage   × (1 + gradeBonus.DamageBonus)
  finalInterval = baseInterval × (1 - gradeBonus.IntervalReduction)

패시브 등급 효과 (effective level):
  effectiveLevel = currentLevel + PassiveWeight(grade)
  실제 레벨은 +1만 증가, effective는 수치 환산용
```

## Edge Cases

- **LuckChance > 1.0**: `Mathf.Clamp01`으로 0~1 강제. 0.80 이상 누적은 의미 없음 (튜닝 상한 0.80).
- **LuckChance < 0**: `Mathf.Clamp01`으로 0 처리.
- **PlayerStats null**: `Roll()` 무인자 호출 시 LuckChance를 0으로 폴백.
- **모든 가중치 합이 0**: 이론상 불가 (Common이 최소 0이어도 상위 등급은 항상 양수).
- **Random.value == 0.0**: 항상 Common 첫 가지에 들어감 (분기 정상).
- **새 등급 추가 시**: enum + `GradeBonus.Get` switch + `Roller` 분기 + Base 상수 모두 갱신 필요.
- **무기 슬롯 이미 Legend**: 동일 무기의 다음 카드 등급은 새로 추첨 — 기존 등급과 독립. 카드 선택 시 더 높은 등급이면 *교체*, 낮거나 같으면 *레벨만 +1* (정책은 weapon-system 문서 영역).
- **레벨업과 상자 카드 동시 큐**: Roller는 호출 단위라 영향 없음 — 호출자가 큐 처리.

## Dependencies

### 업스트림
- **PlayerStats** (`LuckChance`): 행운 누적량 제공 — PlayerStats가 GradeSystem을 알 필요 없음
- **레벨업·상자 선택 시스템** (`LevelupWeaponSelection`, `ChestSystem`): 카드 등장 시 `Roll()` 호출

### 다운스트림
- **무기 시스템** (`WeaponSystem`, `WeaponSlot`): `GradeBonus.Get(grade)`로 데미지/간격 보너스 조회
- **선택 UI** (`WeaponSelectionUI`): 카드 색상·테두리 표현용 등급 정보 사용
- **패시브 시스템**: `PassiveWeight(grade)`로 effective level 환산

### 양방향 명시
- `design/gdd/weapon-system.md` 의 등급 강화 표 ↔ `GradeBonus.Get`
- `design/gdd/levelup-selection.md` 의 카드 가챠 ↔ `CardGradeRoller`
- `design/gdd/spec-system.md` 의 LuckChance ↔ Roller 입력
- `design/gdd/chest-system.md` 의 상자 보상 카드 ↔ Roller 호출

### 인터페이스 계약
- `enum CardGrade { Common, Epic, Unique, Legend }`
- `GradeBonus.Get(CardGrade) → Entry { DamageBonus, IntervalReduction }`
- `GradeBonus.PassiveWeight(CardGrade) → float`
- `CardGradeRoller.Roll() → CardGrade` — PlayerStats.LuckChance 자동 사용
- `CardGradeRoller.Roll(float luckChance) → CardGrade` — 명시 입력
- `CardGradeRoller.DebugProbabilities(float) → string` — 디버깅용

## Tuning Knobs

| 변수 | 위치 | 기본값 | 안전 범위 | 영향 |
|------|------|--------|----------|------|
| `BaseCommon` | CardGradeRoller (const) | 0.60 | 0.40 ~ 0.75 | 낮을수록 럭키 모먼트 빈도 ↑ |
| `BaseEpic` | CardGradeRoller (const) | 0.25 | 0.15 ~ 0.35 | 중간 등급 비중 |
| `BaseUnique` | CardGradeRoller (const) | 0.12 | 0.05 ~ 0.20 | 빌드 핵심 카드 빈도 |
| `BaseLegend` | CardGradeRoller (const) | 0.03 | 0.01 ~ 0.08 | 드물게 — "이번 런 결정타" |
| Common DamageBonus | GradeBonus | +0.12 | 0.05 ~ 0.20 | 너무 높으면 등급 차별화 약화 |
| Epic DamageBonus | GradeBonus | +0.20 | 0.15 ~ 0.30 | Common × 1.5~2배 권장 |
| Unique DamageBonus | GradeBonus | +0.32 | 0.25 ~ 0.45 | |
| Legend DamageBonus | GradeBonus | +0.50 | 0.40 ~ 0.75 | "한 장만 떠도 빌드가 산다" 임팩트 |
| IntervalReduction (Common~Legend) | GradeBonus | 6/10/15/22% | 데미지 보너스의 ~50% 비율 | 공속 폭주 주의 (스택 시 곱셈 효과) |
| Common PassiveWeight | GradeBonus | 1.0 | 1.0 (앵커) | 변경 비권장 — 기준점 |
| Epic PassiveWeight | GradeBonus | 1.5 | 1.2 ~ 1.8 | |
| Unique PassiveWeight | GradeBonus | 2.5 | 2.0 ~ 3.0 | |
| Legend PassiveWeight | GradeBonus | 4.0 | 3.0 ~ 5.0 | "패시브 한 장으로 풀빌드" 가능성 조절 |

## Acceptance Criteria

- [ ] LuckChance=0 일 때 1만 회 추첨 시 Common ~60%, Epic ~25%, Unique ~12%, Legend ~3% (±2% 오차) 분포
- [ ] LuckChance=0.5 일 때 Common 비중이 30%로 줄고 Epic/Unique/Legend가 기본 비율 유지하며 증가
- [ ] LuckChance > 1.0 입력 시 1.0으로 클램프되어 추가 변화 없음
- [ ] PlayerStats.Instance가 null일 때도 `Roll()` 호출이 예외 없이 Common 우세 분포로 동작
- [ ] 같은 무기가 Common→Legend로 등급업되면 데미지가 정확히 `× (1+0.50)` 적용
- [ ] 패시브를 Legend로 5회 획득 시 effective level이 `1 + 5 × 4.0 = 21` (실제 레벨은 5)
- [ ] DebugProbabilities 출력 문자열이 입력 LuckChance에 대한 4등급 % 표시를 정확히 반환
- [ ] UI 카드 프레임 색상이 등급에 1:1 대응 (Common/Epic/Unique/Legend = 4가지 시각적 구분)

## Open Questions

- 행운 상한 0.80 — 이상치 도달 빈도 측정 후 0.70/0.85 조정 검토
- Legend 출현 시 SFX/VFX 강화 (현재는 색상만) — 폴리싱 단계
- 같은 카드 슬롯에 더 낮은 등급이 등장했을 때 *덮어쓰지 않고 레벨만* 올리는 정책 — weapon-system.md와 정합 확인 필요
- 패시브 effective level이 *어떤 수치*에 적용되는지 패시브별 정의 필요 (PlayerStats 영역)
