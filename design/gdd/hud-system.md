# HUD 시스템 (HUD System)

> **Status**: Designed
> **Author**: 사용자 + Claude
> **Last Updated**: 2026-04-20
> **Implements Pillar**: 조작의 통제감

## Overview

HUD 시스템은 플레이어가 전투 중 필요한 모든 정보를 화면에 표시하는 Presentation 레이어 컴포넌트다. 체력, XP, 레벨, 보유 무기/아이템 슬롯, 생존 시간, 킬 수, 골드, 카운트다운, 미니맵, 보스 체력바를 실시간으로 표시한다. MVP에서는 Unity Canvas + TextMeshPro/Image로 구현한다.

## Player Fantasy

정보가 눈에 잘 들어올수록 전략적 판단이 빨라진다. HUD가 너무 많은 공간을 차지하면 전투 집중이 방해된다. 꼭 필요한 정보만, 직관적으로 배치한다.

## Detailed Design

### Core Rules

1. 모든 HUD 요소는 Screen Space - Overlay Canvas에 배치
2. 각 요소는 해당 시스템의 이벤트/값을 구독해 자동 업데이트
3. 게임 상태가 GameOver 또는 Win일 때 결과 화면으로 전환

### HUD 요소 목록

| 요소 | 위치 | 데이터 소스 | 업데이트 주기 |
|------|------|------------|--------------|
| XP 게이지 | 화면 최상단 전체 너비 | `XPSystem.OnXPChanged` | 이벤트 기반 |
| 흐른 시간 | 상단 행 좌측 | 자체 타이머 | 매 프레임 |
| 소지 골드 | 상단 행 좌측 (시간 옆) | `GoldSystem.CurrentGold` | 이벤트 기반 |
| 죽인 몬스터 수 | 상단 행 중앙 좌측 | `EnemyBase.OnEnemyDied` | 이벤트 기반 |
| 카운트다운 | 상단 행 중앙 | `WaveSpawner` or 스테이지 타이머 | 매 프레임 |
| 레벨 | 상단 행 우측 끝 | `XPSystem.OnLevelUp` | 이벤트 기반 |
| 플레이어 HP바 | 좌측 패널 최상단 | `HealthComponent.OnDamaged` | 이벤트 기반 |
| 무기 슬롯 (3개) | 좌측 패널 HP바 아래 | `WeaponSystem.GetEquippedWeapons()` | 이벤트 기반 |
| 무기 쿨타임 오버레이 | 무기 슬롯 위 | `WeaponSystem.GetCooldownNormalized()` | 매 프레임 |
| 스펙 슬롯 (3개) | 좌측 패널 무기 슬롯 아래 | `SpecSystem.GetEquippedSpecs()` | 이벤트 기반 |
| 목표1 / 목표2 | 좌측 패널 하단 | 스테이지 시스템 | 이벤트 기반 |
| 보스 이름 + 체력바 | 화면 중앙 | `HealthComponent.OnDamaged` (보스) | 이벤트 기반 |
| 미니맵 | 우측 원형 | 플레이어 위치 | 매 프레임 |

### 무기 슬롯 표시

- 슬롯 4개 고정 표시
- 빈 슬롯: 회색 빈 아이콘
- 보유 무기: 무기 아이콘 + 등급 색상 테두리 + 쿨타임 오버레이 (어둡게 fill)
- MVP에서는 아이콘 대신 무기명 첫 글자 또는 번호로 대체 가능

### 스펙 슬롯 표시

- 슬롯 3개 고정 표시
- 스펙 아이템 획득 시 해당 슬롯에 표시
- MVP에서는 아이콘 대신 스펙명 약자로 대체 가능

### GameOver / Win 화면

**GameOver:**
```
GAME OVER
생존 시간: 3:42
처치 수: 87
R — 재시작
```

**Win:**
```
CLEAR!
생존 시간: 5:00
처치 수: 213
R — 재시작
```

### Interactions with Other Systems

- **← 체력 시스템**: `OnDamaged` 이벤트 구독
- **← 경험치 시스템**: `OnLevelUp`, `XPProgress` 구독
- **← 무기 시스템**: 보유 무기 목록 + 쿨타임 값 폴링 (매 프레임)
- **← 웨이브 스폰**: 카운트다운 타이머 구독
- **← 플레이어 이동**: `DashCooldownNormalized` 구독 (추후)
- **← 골드 시스템**: `GoldSystem.CurrentGold` 구독
- **← 게임 상태 관리**: GameOver/Win 시 결과 화면 표시
- **← 적 AI**: `EnemyBase.OnEnemyDied` 이벤트 구독 (킬 카운트)

## Formulas

```
생존 시간:
  survivalTime += Time.deltaTime  (Playing 상태에서만)
  표시: string.Format("{0:0}:{1:00}", Mathf.Floor(t/60), t%60)

킬 수:
  killCount++ (OnDeath 이벤트마다)

무기 쿨타임 오버레이:
  fillAmount = WeaponSystem.GetCooldownNormalized(weaponType)
  // 1=방금 발동, 0=준비됨 → Image.fillAmount로 시계방향 차감

대시 쿨타임:
  fillAmount = PlayerMovement.DashCooldownNormalized
```

## Edge Cases

- **게임 시작 직후**: 체력 100%, XP 0%, 레벨 1, 무기 슬롯 모두 빈 상태로 초기화
- **체력이 0**: 체력바가 0으로 표시된 직후 GameOver 화면으로 전환
- **무기 획득 시**: 해당 슬롯에 무기 아이콘 표시, 쿨타임 오버레이 시작
- **무기 강화 시**: 슬롯 외형 변화 없음 (내부 수치만 변경), 선택적으로 강화 연출 추가

## Dependencies

### 업스트림
- 체력 시스템, 경험치 시스템, 무기 시스템, 웨이브 스폰, 플레이어 이동, 게임 상태 관리, 적 AI

## Tuning Knobs

| 변수 | 기본값 | 설명 |
|------|--------|------|
| HUD 투명도 | 100% | 전투 방해 최소화 위해 낮출 수 있음 |

## Visual/Audio Requirements

- MVP: 단순 흰색 텍스트 + 색상 바 (Unity UI 기본 컴포넌트)
- 체력바: 빨간색
- XP 게이지: 파란색
- 무기 쿨타임: 어두운 오버레이 + 시계방향 fill
- 무기 등급 테두리: 커먼=회색, 에픽=보라, 유니크=노랑, 레전드=빨강

## UI Requirements

```
[XP ████████████████████████████████████████████████████]  ← 최상단 전체 너비

[흐른시간  소지골드]  [죽인 몬스터 수]  [카운트다운]              [레벨]

┌──────────────┐         [보스 이름             ]        ┌──────────┐
│ HP ████████  │         [보스 체력바            ]        │          │
│ 무기 □□□     │                                         │  미니맵  │
│ 스펙  □□□    │                                         │  (원형)  │
│ 목표1        │                                         └──────────┘
│ 목표2        │
└──────────────┘
```

## Acceptance Criteria

- [ ] 플레이어가 피해를 받으면 체력바가 실시간 감소함
- [ ] 경험치 획득 시 XP 게이지가 증가함
- [ ] 무기 발동 시 해당 슬롯에 쿨타임 오버레이가 표시되고 시간이 지나면 사라짐
- [ ] 무기 획득 시 빈 슬롯에 무기 표시가 추가됨
- [ ] XP 게이지가 화면 최상단 전체 너비로 표시됨
- [ ] 생존 시간이 초 단위로 증가함
- [ ] 적 처치 시 킬 수가 증가함
- [ ] 골드 획득 시 소지 골드가 갱신됨
- [ ] 레벨업 시 우상단 레벨 숫자가 갱신됨
- [ ] 무기 슬롯 3개가 좌측 패널에 표시됨
- [ ] 스펙 슬롯 3개가 좌측 패널에 표시됨
- [ ] 보스 등장 시 중앙에 보스 이름 + 체력바가 표시됨
- [ ] GameOver / Win 시 결과 화면이 올바른 정보와 함께 표시됨

## Open Questions

없음
