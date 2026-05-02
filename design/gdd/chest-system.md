# 상자 시스템 (Chest System)

> **Status**: Drafted
> **Author**: 임예준 (with AI)
> **Last Updated**: 2026-05-02
> **Implements Pillar**: 파워 에스컬레이션 / 탐험 보상

## Overview

맵에 랜덤 배치된 상자를 골드로 열어 무기·패시브 카드를 추가로 획득하는 보조 성장 시스템. 레벨업과 동일한 카드 선택지를 사용하되, 골드를 자원으로 소비한다. 누적 구매 가격이 지수 증가해 후반부에 자원 압박을 만든다.

## Player Fantasy

레벨업만 기다리지 않고 능동적으로 강해질 수 있다. "골드를 모아서 다음 상자를 열까, 아껴서 더 큰 보상을 노릴까"의 자원 운영 결정. 맵 곳곳에 흩어진 상자가 탐험 동기를 만든다.

## Detailed Rules

1. **배치**: 게임 시작 시(또는 스테이지 시작 시) 맵에 **12개**의 상자가 랜덤 좌표로 스폰
2. **상호작용**: 플레이어가 상자 근처 일정 거리(`InteractRadius`) 안에 들어오면 "E로 열기 (XXG)" 표시. **E** 키 입력 시 구매 시도
3. **구매 처리**:
   - 골드가 비용 이상 → 골드 차감 + 카드 선택 UI 등장 + 상자 소멸 + 누적 구매 카운트 +1
   - 골드 부족 → 구매 실패, 상자 유지 (UI 알림은 추후)
4. **카드 선택**: 레벨업과 동일한 3장 카드 (무기 + 패시브 혼합 풀). 카드 선택 중 `Time.timeScale = 0`
5. **가격 공식**: `Cost = floor(50 * 1.5^purchases)`. `purchases` = 이번 런 누적 구매 수 (스테이지/스폰과 무관, 게임 시작 시 0으로 초기화)
6. **상자 잔여 수**: 0이 되어도 게임 진행에 영향 없음 (그냥 더 못 열 뿐)

## Formulas

```
Cost(n) = floor(50 * 1.5^n)
  n=0 → 50
  n=1 → 75
  n=2 → 112
  n=3 → 168
  n=4 → 253
  n=5 → 379
  n=6 → 569
  n=7 → 853
  n=8 → 1280
```

- 변수: `n` = 누적 구매 수 (0부터 시작)
- 안전 범위: 12회 구매 시 약 6500G — 후반 빌드용 골드 압박

## Edge Cases

- **카드 풀이 비어있을 때 (모든 무기·패시브 Lv 15 도달)**: 골드 차감 + 상자 소멸은 진행하되, 선택 UI 대신 즉시 닫힘 (레벨업의 동일 처리 따름) — 또는 구매 자체를 막음 (튜닝 필요, MVP는 막음 권장)
- **레벨업 UI가 이미 열린 상태에서 E 키**: 무시 (`LevelupWeaponSelection.IsSelecting == true`)
- **상자에 가까이 있는데 다른 상자도 범위 안**: 가장 가까운 1개만 대상
- **여러 상자가 같은 좌표에 스폰**: 최소 거리 제약 적용 (`MinChestDistance`)
- **플레이어가 멀어지면**: 프롬프트 사라지고 E 입력 무시

## Dependencies

### 업스트림 (Chest가 의존)
- **GoldSystem** — 골드 잔액 조회 + 차감 (`GoldSystem.TrySpend(int)` 같은 API 필요, 없으면 추가)
- **LevelupWeaponSelection** — `BuildChoices()` 와 `OnSelectionRequired` 흐름을 외부에서도 트리거 가능하게 public 메서드 추가 (`RequestExternalSelection()`)
- **PlayerController** — 상호작용 입력(E 키) 처리 위치는 `Chest` 측 OnTriggerStay 또는 별도 `InteractionController`에서

### 다운스트림 (Chest를 참조)
- 없음 (UI는 기존 LevelupSelectionUI 재활용)

## Tuning Knobs

| 노브 | 기본값 | 안전 범위 | 영향 |
|---|---|---|---|
| `_chestCount` | 12 | 5~20 | 맵당 상자 수 |
| `_baseCost` | 50 | 30~100 | 1차 상자 가격 |
| `_costMultiplier` | 1.5 | 1.2~2.0 | 가격 증가율 |
| `_interactRadius` | 2.0m | 1.5~3.0 | 상자 반응 거리 |
| `_minChestDistance` | 5.0m | 3~10 | 상자 간 최소 간격 |

## Acceptance Criteria

- [ ] 게임 시작 시 맵에 12개 상자가 랜덤 좌표로 스폰됨 (서로 5m 이상 떨어짐)
- [ ] 상자 근처 진입 시 화면(또는 월드)에 "E로 열기 (XXG)" 표시
- [ ] E 키 + 골드 충분 → 골드 차감 + 카드 선택 UI 등장 + 상자 소멸
- [ ] E 키 + 골드 부족 → 상자 유지, 골드 변화 없음
- [ ] 1차 50G, 2차 75G, 3차 112G로 비용이 올라감
- [ ] 카드 선택 후 게임 재개 (`Time.timeScale` 복구)
- [ ] 레벨업 UI가 열려있는 동안 E 키 무시
- [ ] Console에 NullRef 등 에러 없음

## 구현 분할 (작업 단위)

1. **ChestSystem** (싱글턴) — `purchases` 카운트, `Cost(n)` 계산, `TryPurchase()` API
2. **Chest** (MonoBehaviour) — 월드 오브젝트, OnTrigger로 플레이어 감지 + E 입력
3. **ChestSpawner** — 게임 시작 시 12개 랜덤 배치
4. **ChestPrompt** (UI) — "E로 열기 (50G)" 표시, MVP는 단순 텍스트
5. **LevelupWeaponSelection 보강** — 외부 트리거용 public 메서드 추가
6. **GoldSystem 보강** (필요 시) — `TrySpend(int)` 추가
7. **씬 작업** — Chest 프리팹 + Spawner GameObject + Prompt UI
