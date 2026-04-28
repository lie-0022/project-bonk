# Editor 핸드오프: 스펙 시스템 통합

> 작성: 2026-04-28
> 관련 GDD: `design/gdd/spec-system.md`
> 관련 코드: `PlayerStatsBinder.cs`, `LevelupWeaponSelection.cs` (확장), `WeaponSelectionUI.cs` (확장), `HealthComponent.cs`, `PlayerController.cs`, `WaveSpawner.cs`, `XPSystem.cs`, `GoldSystem.cs`

---

## 0. 변경 요약

스펙(패시브) 13종이 레벨업 카드에 무기와 함께 등장하고, PlayerStats 패시브가 실제로 게임 로직에 적용되도록 컨슈머를 와이어링했다.

### 코드 변경
- **`HealthComponent`** — `DodgeChance` 필드 + `SetMaxHp(newMax, refillToFull)` 추가. TakeDamage에서 회피 굴림.
- **`PlayerStatsBinder` (신규)** — Player에 부착. PlayerStats → HealthComponent (MaxHp, DodgeChance) 동기화 + HpRegen 매 프레임 적용.
- **`PlayerController`** — MoveSpeed를 PlayerStats에서 읽고, ExtraJumps 다중 점프 카운팅 추가.
- **`WaveSpawner`** — 스폰 인터벌을 `DifficultySpawnMultiplier`로 나눔.
- **`XPSystem` / `GoldSystem`** — 보상에 `DifficultyRewardMultiplier` 곱.
- **`LevelupWeaponSelection`** — `LevelupChoice` 통합 구조체로 리팩터, 무기·패시브 혼합 풀 + 등급별 가속(커먼=1, 에픽=2, 유니크=3, 레전드=4 step).
- **`WeaponSelectionUI`** — 패시브 카드 표시 (이름·설명·신규/강화 라벨 한국어).

---

## 1. Scene 작업: `GamePlay.unity`

### 1-1. Player에 PlayerStatsBinder 추가 (필수)

| 항목 | 값 |
|------|-----|
| 대상 GameObject | `Player` |
| 추가할 컴포넌트 | `PlayerStatsBinder` |
| Inspector 값 | (자동 — 같은 오브젝트의 HealthComponent를 GetComponent로 잡음) |

체크리스트:

- [ ] 1. Hierarchy에서 `Player` 선택
- [ ] 2. `Add Component` → `PlayerStatsBinder`
- [ ] 3. Console에 `[PlayerStatsBinder] PlayerStats.Instance is null.` 경고 안 뜨는지 확인 (PlayerStats가 Player와 같은 오브젝트 또는 별도 Systems 오브젝트에 부착돼 있어야 함)

### 1-2. PlayerStats 위치 확인 (이미 있을 가능성 큼)

- [ ] 4. PlayerStats가 Scene 어딘가에 1개 부착돼 있는지 확인 (`Hierarchy` 검색 `PlayerStats`)
- [ ] 5. 없으면 `Systems` 그룹 또는 `Player` 자체에 `PlayerStats` 컴포넌트 추가
- [ ] 6. Inspector에서 baseMoveSpeed/baseMaxHp 등 값 확인 (기본값 그대로 OK)

### 1-3. (선택) 무기 선택 UI 패널이 이미 만들어져 있다면 그대로 사용

이전 핸드오프(`2026-04-28_levelup-weapon-selection.md`)에서 `Panel_WeaponSelection`을 만들어뒀다면 추가 작업 없음. 카드 3장은 이제 무기 또는 패시브를 표시하므로 같은 슬롯에서 자동 동작.

---

## 2. 검증 시나리오

### 2-1. 패시브 등장
- [ ] Play 모드 진입
- [ ] 적 처치 → 레벨업
- [ ] 카드에 무기 + 패시브가 섞여 등장하는지 확인 (예: "검 신규" / "최대 체력 신규")
- [ ] 등급(커먼/에픽/유니크/레전드) 표시 확인

### 2-2. 패시브 효과 적용
- [ ] **최대 체력**: 선택 후 HP 바 최대치 증가 (TopBarUI/HpBarUI)
- [ ] **체력 재생**: 데미지 받은 후 시간 지나면 HP 회복 (콘솔 또는 HP 바)
- [ ] **회피**: 적에게 맞아도 가끔 데미지 0 (콘솔 `[Health] ... 회피!`)
- [ ] **이동 속도**: 체감 가속
- [ ] **추가 점프**: 공중에서 추가 점프 가능 (Lv1 → 더블 점프)
- [ ] **공격 속도**: 무기 발동 인터벌 짧아짐
- [ ] **발사체 수 (Lv 3,6,9,12,15)**: 총/마법 발사 수 +1, 검 휘두르기 수 +1
- [ ] **난이도**: 적 스폰 빨라지고 XP/Gold 증가

### 2-3. 등급 가속 확인
- [ ] **레전드 패시브** 선택 시 레벨이 +4 (1번에 4단계 점프)
- [ ] **커먼**은 +1

### 2-4. 게임 재시작
- [ ] R 키로 재시작 → 모든 패시브/무기 초기화 (Scene 리로드)

---

## 3. 미흡 / 추후 작업

| 항목 | 비고 |
|------|------|
| 패시브 아이콘 | 현재 텍스트만, MVP OK |
| HUD에 현재 패시브 표시 | 무기 슬롯 HUD와 함께 |
| 스테이지 전환 (1→2→3) | 현재 단일 Scene Wave 진행. 멀티 스테이지 도입 시 ResetPassives 훅 필요 |
| 등급 가속(커먼=1/에픽=2/유니크=3/레전드=4 step) | 임시 정책. 플레이테스트 후 GDD 명문화 검토 |

---

## 4. 알려진 한계 (명시)

- **PlayerStats 위치 가정**: PlayerStatsBinder는 `PlayerStats.Instance`를 Start에서 잡음. PlayerStats가 다른 GameObject에 있어도 Awake 순서 문제 없음 (Singleton 패턴).
- **HpRegen 누적**: 0.5 HP/s 같은 분수도 정수로 누적해 적용 (UI 깜빡임 방지). 시각적으로는 1초마다 1포인트씩 보일 수 있음.
- **회피 즉시 차단**: 회피 시 데미지 0이면 OnDamaged 이벤트 발생 안 함. HitFlash/사운드도 안 남.
