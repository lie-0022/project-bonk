# 바닥 드롭 아이템 (Drop Items)

> **Status**: Designed
> **Author**: 임예준
> **Last Updated**: 2026-04-20
> **Implements Pillar**: 파워 에스컬레이션

## Overview

몬스터 처치 시 골드·XP 오브와 함께 바닥에 드롭되는 즉발 효과 아이템. 레벨업 선택지에는 등장하지 않으며, 전투 중 상황 대응용으로 플레이어가 접촉해 획득한다.

## Player Fantasy

치열한 전투 중 바닥에 반짝이는 아이템이 눈에 띄는 순간 — 자석으로 XP를 한꺼번에 빨아들이거나, 타임스톱으로 적을 얼리고 한숨 돌리는 그 순간이 전세 역전의 쾌감을 준다.

## Detailed Design

### 아이템 목록

| 아이템 | 획득 방식 | 효과 |
|--------|----------|------|
| **자석** | 바닥 드롭 → 접촉 획득 | 일정 시간 동안 주변 골드·XP 오브를 자동 흡수 |
| **이동속도 증가** | 바닥 드롭 → 접촉 획득 | 일정 시간 동안 이동속도 대폭 증가 |
| **타임스톱** | 바닥 드롭 → 접촉 획득 | 일정 시간 동안 모든 적 이동·공격 정지 |

> 효과 지속 시간 및 수치는 개발 중 결정 (TBD)

### Core Rules

1. 몬스터 처치 시 일정 확률로 바닥에 드롭
2. 플레이어가 아이템에 접촉(콜라이더 겹침)하면 즉시 효과 발동 및 소멸
3. 효과는 중첩 적용 가능 (같은 종류 여러 개 획득 시 지속 시간 연장)
4. 바닥에 드롭된 상태로 일정 시간 경과 시 자동 소멸 (TBD)
5. 오브젝트 풀링으로 관리

### Interactions with Other Systems

- **← 적 AI**: `EnemyBase.OnEnemyDied` 이벤트 수신 → 확률적 드롭
- **→ 플레이어**: 자석 = XP 흡수 범위 확대, 이동속도 = `MoveSpeed` 버프, 타임스톱 = 적 AI 일시 정지
- **→ 게임 상태 관리**: 타임스톱 중 Playing 상태 유지 (timeScale 변경 아님 — 적 AI만 정지)

## Formulas

```
드롭 확률:
  추적형: TBD%
  돌진형: TBD% (더 높게)

자석 효과:
  XPOrb.pickupRange = magnetRange  // 일정 시간 동안 확대
  GoldOrb.pickupRange = magnetRange

이동속도 증가:
  player.MoveSpeed *= speedMultiplier  // 지속 시간 후 원복

타임스톱:
  foreach enemy in ActiveEnemies: enemy.SetFrozen(true)  // AI 틱 정지
  // timeScale은 건드리지 않음 — 플레이어·UI는 정상 동작
```

## Edge Cases

- **타임스톱 중 게임오버**: 즉시 적 동결 해제 후 GameOver 처리
- **이동속도 버프 중 스테이지 클리어**: 버프 효과 초기화
- **여러 아이템 동시 접촉**: 모두 적용

## Dependencies

### 업스트림
- 적 AI (`OnEnemyDied` 드롭 트리거)
- 오브젝트 풀링

### 다운스트림
- 플레이어 컨트롤러, XPSystem, 적 AI

## Tuning Knobs

| 변수 | 기본값 | 설명 |
|------|--------|------|
| 드롭 확률 | TBD | 너무 자주 나오면 긴장감 감소 |
| 자석 지속 시간 | TBD | |
| 이동속도 배율 | TBD | |
| 타임스톱 지속 시간 | TBD | |

## Acceptance Criteria

- [ ] 몬스터 처치 시 일정 확률로 아이템이 바닥에 드롭됨
- [ ] 접촉 시 즉시 효과가 발동되고 아이템이 사라짐
- [ ] 자석: 주변 XP·골드 오브가 플레이어로 빨려옴
- [ ] 이동속도 증가: 지속 시간 동안 체감 가능한 속도 증가
- [ ] 타임스톱: 모든 적이 멈추고 플레이어는 정상 동작

## Open Questions

- 드롭 확률 수치 — 플레이테스트 후 결정
- 각 효과 지속 시간 — 개발 중 결정
- 아이템 자동 소멸 시간 — TBD
