# 경험치 시스템 (XP System)

> **Status**: Designed
> **Author**: 사용자 + Claude
> **Last Updated**: 2026-04-17
> **Implements Pillar**: 파워 에스컬레이션, 빌드 정체성

## Overview

경험치 시스템은 적 처치로 경험치를 획득하고, 임계값 도달 시 레벨업해 무기·스펙 선택 화면을 트리거하는 진행 시스템이다. 레벨이 높아질수록 레벨업에 필요한 XP가 선형으로 증가하며, 10분 런 기준으로 약 8~10회 레벨업이 가능하도록 설계한다.

## Player Fantasy

적을 처치할 때마다 경험치 바가 채워지는 시각적 피드백이 "다음 레벨업까지 조금만 더"를 유도한다. 레벨업 순간 게임이 잠시 멈추고 무기·스펙 선택 화면이 뜨는 그 순간이 런의 핵심 쾌감 포인트다.

## Detailed Design

### Core Rules

1. 플레이어는 `currentXP`와 `currentLevel`을 보유
2. 적 사망 시 해당 위치에 XP 오브 생성 (추적형 1개, 돌진형 2개 또는 고값 1개 TBD)
3. 플레이어가 `orbPickupRange` 이내로 접근하면 XP 오브 자동 흡수 → `currentXP += xpReward`
4. 자석 아이템 획득 시 흡수 범위 일시 확대
5. `currentXP >= xpToNextLevel`이면 즉시 레벨업 처리
6. 레벨업 시: `currentLevel++`, `currentXP -= xpToNextLevel`, 선택 이벤트 발생
7. 레벨업 후 남은 XP는 다음 레벨로 이월 (버리지 않음)
8. 최대 레벨 없음 — 선택지 소진 시 즉시 재개

### States and Transitions

| 상태 | 조건 | 동작 |
|------|------|------|
| Accumulating | Playing 상태 | XP 수신 대기 |
| LevelingUp | `currentXP >= xpToNextLevel` | 레벨업 처리 → 무기·스펙 선택 이벤트 발생 |

### Interactions with Other Systems

- **← 적 AI**: `OnDeath(xpReward)` 이벤트 수신
- **→ 레벨업 무기 선택**: `OnLevelUp` 이벤트 발생 → 무기 선택 UI 트리거
- **→ HUD 시스템**: `currentXP`, `xpToNextLevel`, `currentLevel` 값 제공

## Formulas

```
레벨업 필요 XP (선형 증가):
  xpToNextLevel = baseXP + (currentLevel - 1) * xpPerLevelIncrease

XP 오브 흡수:
  if distance(player, orb) <= orbPickupRange:
    currentXP += orb.xpReward
    ReturnOrbToPool(orb)

XP 획득 후 레벨업 처리:
  while currentXP >= xpToNextLevel:  // 연속 레벨업 처리
    currentXP -= xpToNextLevel
    LevelUp()

기본값:
  baseXP              = 100   // 레벨 1 → 2 필요 XP
  xpPerLevelIncrease  = 50    // 레벨당 추가 XP
  orbPickupRange      = TBD   // 자동 흡수 범위 (개발 중 결정)
  추적형 xpReward     = 10    // 오브 1개 생성
  돌진형 xpReward     = 20    // 오브 2개 또는 고값 1개 TBD

레벨별 필요 XP 예시:
  Lv 1 → 2: 100 XP
  Lv 2 → 3: 150 XP
  Lv 3 → 4: 200 XP
  Lv 4 → 5: 250 XP
  Lv 5 → 6: 300 XP

10분 런 예상 레벨업 횟수:
  Wave 1 (60초): 추적형 ~30마리 처치 → 300 XP → Lv 1~2 달성
  Wave 2-3: Lv 3~5 달성 예상
  Wave 4-5: Lv 6~8 달성 예상
  → 런당 약 7~9회 레벨업 (무기·스펙 선택 7~9회)
```

## Edge Cases

- **연속 레벨업**: 한 번에 다수의 적을 처치해 XP가 두 배 이상 들어오면 `while` 루프로 연속 레벨업 처리. 무기·스펙 선택 화면은 레벨업당 1회씩 순차적으로 표시
- **선택지 소진 후 레벨업**: 무기 시스템 GDD에서 처리. 모든 무기 최대 레벨 + 슬롯 가득 참 시 선택 없이 즉시 재개
- **게임 오버/Win 시 XP 이벤트**: 게임 상태 전환 후 도착하는 XP 이벤트는 무시 (isGameActive 체크)
- **음수 xpReward**: 무시 처리

## Dependencies

### 업스트림
- **적 AI**: `OnDeath(xpReward)` 이벤트

### 다운스트림
- 레벨업 무기 선택 (`OnLevelUp` 이벤트 구독)
- HUD 시스템 (XP 값 구독)

### 인터페이스 계약
- `XPSystem.OnLevelUp` — `Action` 이벤트
- `XPSystem.CurrentLevel` — 읽기 전용
- `XPSystem.XPProgress` — 0~1 float (HUD 게이지용)

## Tuning Knobs

| 변수 | 기본값 | 안전 범위 | 설명 |
|------|--------|----------|------|
| `baseXP` | 100 | 50 ~ 200 | 낮을수록 첫 레벨업이 빠름 |
| `xpPerLevelIncrease` | 50 | 20 ~ 100 | 높을수록 후반 레벨업이 느려짐 |
| 추적형 `xpReward` | 10 | 5 ~ 25 | 돌진형보다 낮게 유지 |
| 돌진형 `xpReward` | 20 | 10 ~ 40 | 처치 난이도 반영 |

## Visual/Audio Requirements

- XP 획득 시: XP 바 증가 애니메이션 (HUD)
- 레벨업 시: 화면 플래시 또는 레벨업 텍스트 팝업 (선택적)

## UI Requirements

- **XP 게이지**: HUD 하단에 `currentXP / xpToNextLevel` 비율의 프로그레스 바
- **레벨 표시**: 현재 레벨 숫자 (HUD)

## Acceptance Criteria

- [ ] 적 처치 시 올바른 XP가 즉시 추가됨
- [ ] XP가 임계값에 도달하면 레벨업 이벤트가 발생함
- [ ] 레벨이 높아질수록 레벨업 필요 XP가 50씩 증가함
- [ ] 레벨업 후 초과 XP가 다음 레벨로 이월됨
- [ ] HUD XP 게이지가 올바른 비율로 표시됨

## Open Questions

- `orbPickupRange` 수치 — 개발 중 결정
- 돌진형 XP 오브 수 — 오브 2개 vs 고값 1개 TBD
