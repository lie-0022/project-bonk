# 레벨업·상자 선택 (Levelup & Chest Selection)

> **Status**: Designed
> **Author**: 임예준
> **Last Updated**: 2026-04-20
> **Implements Pillar**: 빌드 정체성

## Overview

레벨업 또는 상자 구매 시 게임을 일시정지하고 무기·스펙 혼합 3개 선택지를 제시하는 시스템이다. 플레이어가 하나를 선택하면 새 무기 추가·강화 또는 스펙 추가·강화가 이루어진다. 이 선택 순간이 빌드 정체성 필라의 핵심이다.

## Player Fantasy

레벨업 순간 전투가 멈추고 세 가지 선택지가 등장한다. "무기를 추가할까, 스펙으로 생존력을 올릴까" — 이 짧은 고민의 순간이 매 런을 다르게 만들고 "한 번만 더"를 유도한다.

## Detailed Design

### Core Rules

1. `XPSystem.OnLevelUp` 이벤트 또는 상자 구매 시 즉시 발동
2. `Time.timeScale = 0` — 전투 완전 정지 (게임 상태는 Playing 유지, timeScale만 0)
3. **선택지 생성**: 미획득 무기 + 보유 무기 강화 + 스펙(책) 항목을 혼합해 등급 차등 랜덤 3개 추출
4. **선택지가 3개 미만**: 가능한 수만큼 표시 (1~2개)
5. **선택지가 0개**: 모든 무기·스펙 최대 레벨 + 슬롯 가득 → 선택 없이 즉시 재개
6. **선택 방법**: 1/2/3 키 또는 클릭
7. 선택 완료 후 `Time.timeScale = 1` — 전투 재개
8. ESC로 선택 취소 불가 (반드시 선택해야 재개)
9. **연속 레벨업**: 현재 선택 완료 후 즉시 다음 선택 화면 표시 (큐잉)

### 선택지 생성 규칙

| 조건 | 표시 내용 |
|------|----------|
| 미획득 무기 + 슬롯 여유 있음 | 새 무기 추가 (신규) |
| 보유 무기 + 최대 레벨 미만 | 무기 강화 (Lv X → Lv X+1) |
| 보유 무기 + 최대 레벨 | 선택지 제외 |
| 무기 슬롯 가득 참 | 무기 강화 + 스펙 선택지만 제공 |
| 스펙 미획득 또는 최대 레벨 미만 | 스펙(책) 추가·강화 |

### 등급 차등

선택지 등장 시마다 해당 항목의 등급이 랜덤 결정 (무기·스펙 자체에 고정 아님).

| 등급 | 색상 | 등장 확률 | 수치 증가폭 |
|------|------|----------|------------|
| 커먼 | 회색 | 높음 | 소폭 강화 |
| 에픽 | 보라 | 보통 | 중간 강화 |
| 유니크 | 노랑 | 낮음 | 대폭 강화 |
| 레전드 | 빨강 | 희귀 | 게임 체인저급 |

> 등급별 가중치 TBD. 행운 스펙이 높을수록 높은 등급이 더 자주 등장.

### States and Transitions

| 상태 | 조건 | 동작 |
|------|------|------|
| Idle | 평상시 | 대기 |
| Selecting | `OnLevelUp` 수신 | timeScale=0, UI 표시 |
| Complete | 선택 완료 | timeScale=1, UI 숨김 |

### Interactions with Other Systems

- **← 경험치 시스템**: `OnLevelUp` 이벤트 수신
- **← 상자 시스템**: 상자 구매 시 선택 화면 트리거
- **→ 무기 시스템**: `AddWeapon()` 또는 `UpgradeWeapon()` 호출
- **→ 스펙 시스템**: 스펙 추가·강화 호출
- **→ 선택 UI**: 선택지 데이터 전달, 선택 결과 수신

## Formulas

```
선택지 생성:
  allWeapons      = WeaponType 전체 목록
  addable         = allWeapons.Where(w => !WeaponSystem.HasWeapon(w))
                              .Where(_ => currentSlots < maxWeaponSlots)
  upgradable      = allWeapons.Where(w => WeaponSystem.HasWeapon(w)
                                       && WeaponSystem.GetWeaponLevel(w) < maxWeaponLevel)
  candidatePool   = addable.Concat(upgradable).ToList()
  choices         = candidatePool.OrderBy(_ => Random.value).Take(3).ToList()

선택 처리:
  if WeaponSystem.HasWeapon(chosen):
    WeaponSystem.UpgradeWeapon(chosen)
  else:
    WeaponSystem.AddWeapon(chosen)
```

## Edge Cases

- **연속 레벨업**: 선택 화면 중 또 레벨업 이벤트 수신 시 → 현재 선택 완료 후 즉시 다음 선택 화면 표시 (큐잉)
- **선택지 0개**: 모든 무기가 최대 레벨 + 슬롯 가득 참 → 선택 없이 즉시 재개
- **선택지 1~2개**: 가능한 선택지만 표시

## Dependencies

### 업스트림
- 경험치 시스템 (`OnLevelUp`)
- 무기 시스템 (`AddWeapon`, `UpgradeWeapon`, `HasWeapon`, `GetWeaponLevel`)

### 다운스트림
- 무기 선택 UI

### 인터페이스 계약
- `LevelupWeaponSelection.OnSelectionRequired(List<WeaponChoice> choices)` — UI 트리거
- `LevelupWeaponSelection.OnWeaponChosen(WeaponType chosen)` — UI 콜백

## Tuning Knobs

| 변수 | 기본값 | 설명 |
|------|--------|------|
| 선택지 수 | 3 | 많을수록 선택 고민 증가 |

## Visual/Audio Requirements

- 선택 화면 등장 시 배경 어둡게 디밍
- 레벨업 사운드 (선택적)

## UI Requirements

→ 무기 선택 UI GDD 참조

## Acceptance Criteria

- [ ] 레벨업 시 전투가 멈추고 선택 화면이 표시됨
- [ ] 최대 레벨 무기는 선택지에 표시되지 않음
- [ ] 슬롯이 가득 차면 강화 선택지만 표시됨
- [ ] 새 무기 선택 시 해당 무기가 슬롯에 추가되고 자동 발동 시작됨
- [ ] 기존 무기 선택 시 강화되고 수치가 증가됨
- [ ] 선택 후 전투가 즉시 재개됨
- [ ] 선택지 0개 상황에서 선택 없이 즉시 재개됨

## Open Questions

- **등급 가중치**: 레벨업 선택지에서 레전드 무기가 나올 확률을 낮게 설정할지 여부. Balance 설계 시 반영.
