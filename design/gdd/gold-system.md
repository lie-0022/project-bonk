# 골드 시스템 (Gold System)

> **Status**: Designed (역공학)
> **Author**: 사용자 + Claude
> **Last Updated**: 2026-05-08
> **Implements Pillar**: 메타 진행, 상자·상인 경제

## Overview

골드 시스템은 적 처치로 골드를 획득하고 상자·상인 시스템에서 소비하는 자원 진행 시스템이다.
적 사망 시 위치에 골드 오브가 스폰되고, 플레이어가 흡수 범위에 들어오면 자동으로 수집된다.
경험치 시스템과 평행 구조이지만 별개 자원으로, 플레이어 강화(레벨업)와 분리된 구매·해금 경로를 형성한다.

## Player Fantasy

적을 처치할 때마다 노란색 골드 오브가 튀어나오는 시각적 만족감, 그리고 누적된 골드로 상자를 열 때의
"무엇이 나올까" 기대감이 핵심이다. XP는 강제 성장이지만 골드는 선택 소비라 플레이어가 *결정*하는 자원이다.

## Detailed Rules

1. 플레이어는 `CurrentGold` (int)를 보유하며 시작 시 0
2. 적 사망 시 적 타입에 따라 기본 골드 보상이 결정됨
   - **추적형 (Chaser)**: `_chaserGold` (기본 5)
   - **돌진형 (Rusher)**: `_rusherGold` (기본 10)
   - 판정 기준: `xpReward >= 20f` → Rusher 보상, 그 외 → Chaser 보상
3. `PlayerStats.DifficultyRewardMultiplier`를 곱한 후 `Mathf.RoundToInt`로 정수 변환
4. 변환된 양만큼의 골드를 담은 GoldOrb 1개를 적 사망 위치에 스폰
   - `PickupPool`에서 풀링된 인스턴스 사용
   - 스폰 위치는 적 위치에 `Random.insideUnitCircle * 0.5f` 오프셋 + Y `0.3f` 적용
5. 골드 오브는 플레이어가 `_attractRadius` (기본 5m) 내로 진입 시 자동 추적
   - `DropItemEffects.MagnetActive`가 true면 거리 무관 즉시 추적
   - `_collectRadius` (기본 0.5m) 도달 시 수집 → `GoldSystem.AddGold(amount)` → 풀로 반환
6. 외부 시스템(상자, 이벤트)도 `GoldSystem.Instance.AddGold(amount)`로 골드 추가 가능
7. 상자/상인은 `GoldSystem.Instance.SpendGold(amount)` 호출
   - 잔액 부족 시 false 반환 (소비 실패) → 호출 측에서 UX 처리
   - 잔액 충분 시 true 반환 + 골드 차감
8. 골드 변경 시 `OnGoldChanged(int currentGold)` 정적 이벤트 브로드캐스트 (HUD 구독용)

## Formulas

```
적 사망 시 골드 보상 결정:
  baseReward = (xpReward >= 20) ? rusherGold : chaserGold
  rewardMult = PlayerStats.DifficultyRewardMultiplier (없으면 1.0)
  finalReward = RoundToInt(baseReward * rewardMult)

골드 오브 스폰 위치:
  offset = Random.insideUnitCircle * 0.5f          // 평면 분산
  spawnPos = enemyPos + (offset.x, 0.3, offset.y)  // Y 살짝 띄움

골드 오브 흡수 판정:
  if MagnetActive OR sqrDistance(orb, player) <= attractRadius²:
    isAttracting = true
  if isAttracting AND distance(orb, player) <= collectRadius:
    GoldSystem.AddGold(goldAmount)
    PickupPool.ReturnGoldOrb(orb)

기본값:
  chaserGold        = 5      // 추적형 1마리당
  rusherGold        = 10     // 돌진형 1마리당
  attractRadius     = 5.0    // 자동 추적 시작 거리(m)
  collectRadius     = 0.5    // 수집 거리(m)
  flySpeed          = 8.0    // 추적 이동 속도(m/s)
  bobSpeed          = 2.0    // 대기 시 위아래 진동 속도
  bobHeight         = 0.15   // 진동 폭

5분 런 예상 골드 (난이도 배율 1.0 가정):
  Wave 1 (60s): Chaser ~30 = 150 G
  Wave 2~3:    Chaser ~50 + Rusher ~10 = 350 G
  Wave 4~5:    Chaser ~60 + Rusher ~25 = 550 G
  합계 ~1000~1200 G → 상자 4~5회 오픈 가능 (상자 1회 200~300G 가정)
```

## Edge Cases

- **풀 고갈**: `PickupPool.GetGoldOrb()`가 null 반환 시 골드 오브 미스폰 (해당 처치는 골드 손실). 호출자는 null 체크 후 조기 반환.
- **연속 사망 동시 처리**: 다수 적이 같은 프레임에 사망해도 각각 독립 GoldOrb 스폰. 위치 오프셋이 랜덤이라 시각적 겹침 거의 없음.
- **자석 활성 중 GoldOrb 스폰**: 스폰 즉시 `Update()`에서 `MagnetActive` 체크 → 다음 프레임에 추적 시작.
- **음수/0 골드 추가**: `AddGold(amount <= 0)`은 즉시 반환 (조기 종료).
- **소비 시 amount < 0**: 별도 가드 없음 — 호출자 책임. 음수 호출하면 골드 증가 효과 발생 (TODO: 가드 추가 검토).
- **PlayerStats null**: `DifficultyRewardMultiplier` 접근 시 null 체크 → 기본 배율 1.0 사용.
- **씬 전환/재시작**: GoldSystem은 Awake에서 싱글턴 인스턴스 설정, Initialize에서 0으로 리셋. 씬 다시 로드 시 새 인스턴스 생성.

## Dependencies

### 업스트림
- **적 AI** (`EnemyBase.OnEnemyDied(xpReward, position)`): 사망 시 보상·위치 제공
- **PlayerStats** (`DifficultyRewardMultiplier`): 난이도 보정 배율 (역방향: PlayerStats가 GoldSystem을 알 필요는 없음)
- **PickupPool** (`GetGoldOrb`, `ReturnGoldOrb`): 풀 인프라
- **DropItemEffects** (`MagnetActive`): 자석 효과 상태 조회

### 다운스트림
- **HUD 시스템** (`TopBarUI`): `OnGoldChanged` 이벤트 구독 → 골드 표시 갱신
- **상자 시스템** (`Chest`, `ChestSystem`): `SpendGold` 호출
- **상인 시스템** (Vertical Slice): `SpendGold` 호출 예정

### 양방향 명시
- `design/gdd/enemy-ai.md` 의 OnDeath 이벤트 → GoldSystem이 구독자 중 하나
- `design/gdd/chest-system.md` 의 골드 소비 → GoldSystem이 공급
- `design/gdd/hud-system.md` 의 골드 표시 → GoldSystem `OnGoldChanged` 이벤트

### 인터페이스 계약
- `GoldSystem.Instance.CurrentGold` — 읽기 전용 (int)
- `GoldSystem.Instance.AddGold(int amount)` — 양수만 유효
- `GoldSystem.Instance.SpendGold(int amount) → bool` — 잔액 부족 시 false
- `GoldSystem.OnGoldChanged` — `static event Action<int>` (HUD가 구독)

## Tuning Knobs

| 변수 | 기본값 | 안전 범위 | 영향 |
|------|--------|----------|------|
| `_chaserGold` | 5 | 2 ~ 15 | 낮을수록 상자 오픈이 느려짐 |
| `_rusherGold` | 10 | 5 ~ 30 | 처치 난이도 반영 — Chaser의 1.5~3배 권장 |
| `_attractRadius` (GoldOrb) | 5 | 3 ~ 10 | 작을수록 회수 위해 직접 접근 필요 (긴장감 ↑) |
| `_flySpeed` (GoldOrb) | 8 | 5 ~ 15 | 흡수 만족감에 영향 |
| `_collectRadius` (GoldOrb) | 0.5 | 0.3 ~ 1.0 | 너무 크면 시각적으로 일찍 사라짐 |
| Rusher 판정 기준 (`xpReward >= 20`) | 20 | — | 적 추가 시 Rusher 분류 임계값 (코드 상수) |

## Acceptance Criteria

- [ ] 추적형 적 처치 시 화면에 노란 골드 오브 1개가 나타나고, 가까이 가면 자동 흡수되어 HUD 골드 +5 (배율 1.0 기준)
- [ ] 돌진형 적 처치 시 동일하게 +10 골드
- [ ] PlayerStats.DifficultyRewardMultiplier가 1.5일 때 추적형 처치 보상이 8 (= round(5 * 1.5))
- [ ] 자석 아이템 활성 중에는 화면 모든 골드 오브가 즉시 플레이어로 추적
- [ ] 골드 오브가 collectRadius (0.5m) 도달 시 풀로 반환되고 동일 오브가 재활용됨
- [ ] HUD 골드 텍스트가 `OnGoldChanged` 이벤트마다 즉시 갱신
- [ ] `SpendGold(amount)`가 잔액 부족 시 false 반환하며 골드는 변하지 않음
- [ ] `SpendGold(amount)`가 잔액 충분 시 true 반환하며 정확히 차감, HUD 즉시 갱신
- [ ] 게임 시작 시 CurrentGold = 0
- [ ] PickupPool 고갈 시 골드 오브 미스폰 (예외 없이 정상 진행)

## Open Questions

- `SpendGold(amount < 0)` 가드 추가 여부 — 현재는 호출자 책임
- 골드 보상 다양화 — 보스/엘리트 적의 별도 보상 (현재 추적형/돌진형 2종만)
- 골드 오브 풀 사이즈 튜닝 — 5분 런 동안 동시 활성 최대치 측정 필요
