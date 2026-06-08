# 스테이지 진행 시스템 (Stage Progression)

> **System #23** (systems-index) | Category: Core | Status: Implemented
> 관련 코드: `StageGate.cs`, `StageProgress.cs`, `StageDifficulty.cs`, `ArenaPhaseManager.cs`, `ArenaEncounterTrigger.cs`, `DebugStageSelector.cs`, `WaveSpawner.cs`(재무장)
> 작성: 2026-06-08 (코드 전수 기반)

---

## 1. Overview

스테이지 진행 시스템은 한 번의 런(run)을 **3개 스테이지의 연속 흐름**으로 묶는 Core 시스템이다. 플레이어는 단일 `GamePlay` 씬을 반복 로드하며 `GameSession.SelectedMapIndex`에 따라 슬라임(Stage1) → 고블린(Stage2) → 스켈레톤(Stage3) 맵을 차례로 진행한다. 각 스테이지는 봉인/웨이브/보스 처치 후 **게이트(`StageGate`)**를 통과해 다음 맵으로 넘어가며, 마지막 스테이지 게이트는 게임 클리어(Win)로 이어진다. 진행 결과는 **로컬 해금(`StageProgress`)**으로 영속되어, 신규 플레이어는 1스테이지만 선택 가능하고 클리어할 때마다 다음 스테이지가 열린다. 맵별 난이도는 **`StageDifficulty`** 정적 배율(적 HP/데미지/보상)로 조정한다. Stage3은 예외적으로 **2단 보스 아레나** 구조로, `ArenaEncounterTrigger`(봉인+웨이브 시작)와 `ArenaPhaseManager`(보스 사망 카운트→상층 개방→최종 게이트)가 한 씬에서 보스를 2회 등장시킨다(단일 `WaveSpawner` 재무장).

핵심 구성요소 요약:

| 요소 | 역할 |
|---|---|
| `GameSession.SelectedMapIndex` | 현재 진행 중인 맵 인덱스(씬 리로드에도 static 유지) |
| `DebugStageSelector` | 선택 종족에 맞는 맵 루트 활성화 + 스폰/난이도/PlayerSpawn 적용 |
| `StageGate` | 보스 처치 후 열림 → 다음 맵 로드 또는 Win + 진행도/통계 기록 |
| `StageProgress` | 해금 진행도(PlayerPrefs 로컬 영속) |
| `StageDifficulty` | 맵별 적 HP/데미지/보상 배율(static) |
| `ArenaEncounterTrigger` / `ArenaPhaseManager` | Stage3 전용 봉인 인카운터 + 2단 보스 진행 |

## 2. Player Fantasy

**"한 판 더"의 정복감 — 매 스테이지를 넘을 때마다 세계가 열린다.**

플레이어는 단순히 웨이브를 버티는 게 아니라 **눈에 보이는 관문을 하나씩 격파**하는 감각을 느껴야 한다. 보스를 쓰러뜨리면 막혀 있던 길이 **빛기둥과 함께 열리고**, 미니맵 화살표가 다음 목적지를 가리킨다. 게이트를 통과하는 순간 "이 스테이지를 정복했다"는 명확한 완료감이 주어진다.

스테이지가 거듭될수록 적은 더 단단하고(HP↑) 더 아프게(데미지↑) 변하지만 보상도 커져(코인·XP↑), 플레이어는 자신의 빌드가 강해지는 속도와 난이도 상승이 경쟁하는 긴장을 즐긴다.

처음 게임을 켠 플레이어에게는 **1스테이지만 열려 있다.** 이는 위압감을 줄이고 "여기부터 시작"이라는 명확한 출발점을 제시한다. 클리어로 다음 스테이지가 해금되는 순간은 **성취의 보상**이며, 다시 접속해도 그 진행이 남아 있다는 안정감(로컬 영속)을 준다.

Stage3의 2단 보스는 진행의 **클라이맥스**다. 첫 보스를 잡았다고 끝이 아니라 봉인이 열리며 위층으로 올라가 더 강한 최종 보스를 만나는 구조는 "거의 다 왔다"에서 "진짜 마지막"으로 긴장을 끌어올린다.

## 3. Detailed Rules

### 3.1 맵 구성과 활성화
- 모든 스테이지는 **단일 `GamePlay` 씬** 안의 별도 루트(`Stage1`/`Stage2`/`Stage3`)로 존재한다. 동시에 하나만 활성.
- `DebugStageSelector.Awake()`가 `GameSession.SelectedRace`(=선택 맵)에 따라 해당 맵 루트만 `SetActive(true)`, 나머지는 비활성화한다.
- 활성화 직후: 그 맵의 `PlayerSpawn` 위치로 플레이어 이동(CharacterController 일시 비활성 후 이동), 맵별 항아리/상자 수·난이도 배율 적용, ObjectPool 잡몹 프리팹·WaveSpawner 보스 프리팹을 해당 종족으로 교체.
- 맵에 `ArenaEncounterTrigger`(봉인)가 있으면 WaveSpawner 자동시작 OFF(트리거가 시작), 없으면 진입 즉시 자동시작 ON.

### 3.2 스테이지 클리어와 게이트 통과
- 보스 처치 시(`BossEnemy.OnBossDied`) `_autoOpenOnBossDied=true`인 게이트는 자동으로 열린다. 열리면: 장애물 비활성화 + 빛기둥(클리어 마커) 등장 + 미니맵 유도(`MinimapObjective.Set`).
- 열린 게이트 트리거에 **플레이어 태그**가 진입하면 1회 발동:
  1. 현재 스테이지 통계를 누적 랭킹에 기록(`RunTotals.AddStage(RunStats.CaptureCurrent(true))`)
  2. 해금 진행도 갱신(`StageProgress.MarkCleared(GameSession.SelectedMapIndex)`)
  3. **`_isFinalStage=true`면** → `GameManager.ChangeState(Win)`(게임 클리어)
  4. **아니면** → `GameSession.SelectedMapIndex = _nextMapIndex` 후 `GamePlay` 씬 리로드
- 중복 통과 방지: `_opened`/`_passed` 플래그로 각 1회만.

### 3.3 해금 진행도(로컬 영속)
- `StageProgress.HighestUnlocked`(PlayerPrefs 키 `BladeSurge.HighestUnlockedStage`, 기본 0).
- `IsUnlocked(index)` = `index <= HighestUnlocked`. 맵 선택 화면은 잠긴 맵을 LockedSprite + 클릭 불가로 표시.
- `MarkCleared(clearedIndex)` = `clearedIndex+1`이 현재 최고치보다 크면 해금 확장(되돌리지 않음, 단조 증가).
- 디버그 메뉴: `BladeSurge ▸ Stage Progress`에서 초기화(0)/전체 해금/현재 로그.

### 3.4 맵별 난이도 배율
- `StageDifficulty`(static): `EnemyHpMultiplier`, `EnemyDamageMultiplier`, `RewardMultiplier`. 기본 1.0.
- `DebugStageSelector.ApplyDifficulty()`가 맵 진입 Awake에서 그 맵 값으로 매번 재설정(씬 리로드 무관). 적용처: 적/보스 최대 HP(WaveSpawner·HealthComponent), 적 데미지(DamageDealer), 코인·XP 획득량.

### 3.5 Stage3 — 2단 보스 아레나(특수)
- 통로를 올라 중앙홀에 진입하면 `ArenaEncounterTrigger`가 **위치 폴링**(플레이어 y ≥ `_enterHeight` AND 봉인벽 평면을 홀 쪽으로 넘음)으로 1회 발동 → 봉인벽 활성화(되돌아가기 차단) + `WaveSpawner.BeginEncounter()`로 1차 웨이브/보스 시작.
- `ArenaPhaseManager`가 `BossEnemy.OnBossDied`를 카운트:
  - **1번째 보스 처치** → 상층 진입 봉인(`_upperSeal`) 비활성화(개방) + 미니맵을 상층 진입로로 유도.
  - **2번째(최종) 보스 처치** → 최종 게이트 `Open()` 직접 호출(이 게이트는 `_autoOpenOnBossDied=false`).
- 상층 진입 시 두 번째 `ArenaEncounterTrigger`가 동일 방식으로 발동, **같은 `WaveSpawner`를 재무장**(`BeginEncounter`에 스폰영역/보스 배율 오버라이드 전달)해 2차 보스를 등장시킨다.
- 물리 트리거 대신 위치 폴링을 쓰는 이유: CharacterController ↔ 트리거 콜라이더 이벤트가 환경에 따라 불안정 → 결정적 판정 채택. 봉인벽 평면(막는 축/홀 방향/경계값)은 Start에서 BoxCollider로부터 자동 해석.

## 4. Formulas

**변수 정의**

| 변수 | 의미 | 범위 |
|---|---|---|
| `U` | `StageProgress.HighestUnlocked` (해금 최고 인덱스) | 정수 0 ~ `N-1` (N=스테이지 수=3) |
| `c` | 방금 클리어한 스테이지 인덱스 | 0 ~ N-1 |
| `i` | 조회 대상 스테이지 인덱스 | 0 ~ N-1 |
| `Hp_base` | 적/보스 프리팹 기본 최대 HP | >0 |
| `m_hp`, `m_dmg`, `m_rew` | 맵별 `StageDifficulty` 배율 | 권장 0.5 ~ 3.0 (기본 1.0) |
| `w_hp` | 웨이브 진행 HP 배율(WaveSpawner) | ≥1.0 |

**F1 — 해금 갱신 (단조 증가)**
```
U' = max(U, c + 1)
```
- 예: U=0(1스테이지만 해금), Stage1(c=0) 클리어 → U'=max(0,1)=1 (Stage2 해금). 이미 U=2인 상태에서 Stage1(c=0) 재클리어 → U'=max(2,1)=2 (불변).

**F2 — 해금 판정**
```
IsUnlocked(i) = (i ≤ U)
```
- 예: U=1 → i=0,1 해금 / i=2 잠김.

**F3 — 난이도 적용 적 최대 HP**
```
Hp_final = Hp_base × m_hp × w_hp
```
- 예: 스켈레톤 보스 Hp_base=1000, m_hp=1.0, 보스는 웨이브배율 미적용(w_hp=1.0) → 1000.
- 예: 잡몹 Hp_base=50, m_hp=1.0, 5웨이브 w_hp=1.4 → 70.

**F4 — 난이도 적용 적 데미지**
```
Dmg_final = Dmg_base × m_dmg
```
- 현재 튜닝: 슬라임 m_dmg=1.0, 고블린=1.1, 스켈레톤=1.0(즉사방지 완화). 예: 스켈레톤 잡몹 Dmg_base=8 → 8×1.0=8.

**F5 — 난이도 적용 보상**
```
Reward_final = Reward_base × m_rew
```
- 코인/XP 획득량에 곱. 예: 코인 10 × m_rew=1.5 → 15.

**F6 — 아레나 진입 판정(위치 폴링)**
```
넘어옴 = (player.y ≥ enterHeight) AND
         (hallIsNegative ? player[axis] ≤ threshold : player[axis] ≥ threshold)
threshold = hallIsNegative ? (center[axis] − ext) : (center[axis] + ext)
```
- `axis`: 봉인벽의 두 수평축 중 **더 얇은 축**(=막는 법선). `ext`: 그 축 반(half) 두께. `center`: 봉인벽 월드 중심. `hallIsNegative`: 홀 중심이 벽 기준 음의 방향인지.
- 예: enterHeight=13, 벽이 Z축을 막고(axis=2) 홀이 −Z쪽(hallIsNegative=true), center.z=20, ext=1 → threshold=19. player.y=13.5, player.z=18.5 → (13.5≥13) AND (18.5≤19) = 발동.

**F7 — 2단 보스 페이즈 전이**
```
bossDeaths += 1 (보스 사망마다)
bossDeaths == 1 → 상층 봉인 개방
bossDeaths ≥ 2 → 최종 게이트 Open
```

## 5. Edge Cases

| 상황 | 처리 |
|---|---|
| **게이트가 안 열린 상태로 통과 시도** | `_opened==false`면 `OnTriggerEnter` 무시. 보스 처치 전엔 진입 불가. |
| **게이트 중복 통과** | `_passed` 플래그로 1회만. 두 번째 진입은 무시(이중 씬 로드/이중 통계 방지). |
| **선택한 맵보다 높은 인덱스를 이미 해금** | `MarkCleared`는 `max`라 되돌리지 않음. 해금된 낮은 스테이지를 다시 골라 클리어해도 진행도 감소 없음. |
| **PlayerPrefs에 진행도 키 없음(최초 실행)** | `GetInt(Key, 0)` → 1스테이지만 해금. |
| **맵 루트 참조가 하나도 설정 안 됨(DebugStageSelector)** | `ApplyMap`이 조기 반환 → 기존 씬 구성 그대로 유지(맵 전환 생략). |
| **선택 종족 맵이 null** | 경고 로그 + 맵 비활성 상태 가능. 플레이어 이동/스폰 생략. |
| **맵에 PlayerSpawn 없음** | 플레이어 기존 위치 유지(이동 생략). |
| **봉인벽 BoxCollider 없음 / 평면 해석 실패** | 경고 로그 + 트리거 발동 불가(`ResolveBarrierPlane`이 false). 인카운터 시작 안 됨 → **데이터 배선 필수**. |
| **봉인벽 회전됨** | 미지원(축 정렬 박스 가정). 회전된 벽은 경계 판정 부정확 → 회전 없이 배치할 것. |
| **WaveSpawner 미할당(ArenaEncounterTrigger)** | 경고 로그 + 인카운터 시작 생략(봉인벽만 닫힘). |
| **2단 보스에서 보스2가 안 죽고 플레이어 사망** | `GameManager`가 GameOver 전환. 페이즈 카운트는 씬 리로드로 리셋(재도전 시 처음부터). |
| **`StageDifficulty` 잔류값** | static이라 씬 리로드에도 유지되지만, 매 맵 Awake에서 그 맵 값으로 재설정되므로 이전 맵 배율이 새 맵에 새지 않음. |
| **최종 게이트 `_autoOpenOnBossDied=true` 오설정** | 보스1 처치 즉시 최종 게이트가 열려 2단 흐름 붕괴. → **최종 게이트는 반드시 false + ArenaPhaseManager가 Open 호출**(배선 규칙). |
| **진입 높이(`_enterHeight`)보다 낮은 점프로 홀 진입** | 발동 안 됨. 홀 바닥 높이에 맞게 enterHeight 설정 필요(현재 13). |

## 6. Dependencies

**이 시스템이 의존하는 것 (upstream)**

| 시스템 | 용도 |
|---|---|
| 게임 상태 관리(`GameManager`) | 최종 게이트 통과 시 `Win` 전환 / 사망 시 GameOver |
| 웨이브 스폰(`WaveSpawner`) | 보스 스폰, `BeginEncounter` 재무장(2단), 자동시작 토글, 난이도 HP 배율 적용 |
| 보스 몬스터(`BossEnemy`) | `OnBossDied` 정적 이벤트(게이트 자동 개방·페이즈 카운트의 트리거) |
| 랭킹·점수(`RunStats`/`RunTotals`) | 게이트 통과 시 스테이지 통계 스냅샷 누적 |
| 미니맵(`MinimapObjective`) | 게이트·상층 진입로 유도 마커 |
| `GameSession`(static) | 현재 맵 인덱스/선택 종족 보관(씬 리로드 유지) |

**이 시스템에 의존하는 것 (downstream)**

| 시스템 | 관계 |
|---|---|
| 맵 선택 UI(`MapSelectController`) | `StageProgress.IsUnlocked`로 잠금 표시, 클리어로 해금 확장 |
| 캐릭터 선택 | 선택 결과를 `GameSession.SelectedRace/MapIndex`에 기록 → 진행 시작점 |
| 난이도 소비처(`DamageDealer`, `GoldSystem`, `XPSystem`, `HealthComponent`) | `StageDifficulty` 배율을 읽어 적용 |

**양방향 문서 갱신 필요 (design-docs 규칙)**
- `wave-spawner.md` → "스테이지 진행(재무장/자동시작 토글)" 의존 언급 추가 필요
- 신규 작성 예정 GDD에 본 문서 상호 참조: `boss-monster.md`(OnBossDied), `ranking-scoring.md`(AddStage), `minimap.md`(Objective), `character-select.md`(SelectedRace)
- `game-state-manager.md` → Win 전환 출처로 본 시스템 언급 추가 권장

## 7. Tuning Knobs

| 노브 | 위치 | 안전 범위 | 영향 |
|---|---|---|---|
| `EnemyHpMultiplier` (맵별) | `DebugStageSelector._{slime/goblin/skeleton}HpMult` | 0.5 ~ 3.0 (기본 1.0) | 적·보스 생존성. ↑ = 라운드 길이·체감 난이도↑ |
| `EnemyDamageMultiplier` (맵별) | `DebugStageSelector._{...}DamageMult` | 0.5 ~ 2.0 (현재 슬라임1.0/고블린1.1/스켈1.0) | 플레이어 위협도. **과도 시 떼 즉사** — 피격무적과 함께 튜닝 |
| `RewardMultiplier` (맵별) | `DebugStageSelector._{...}RewardMult` | 0.5 ~ 3.0 (기본 1.0) | 빌드 성장 속도(코인·XP). ↑ = 후반 스테이지 보상감 |
| 항아리 수 (맵별) | `_{...}JarCount` | 50 ~ 200 (슬라임150/고블린120/스켈120) | 골드·XP 소스 밀도, 맵 과밀/성능 |
| 상자 수 (맵별) | `_{...}ChestCount` | 5 ~ 20 (슬라임12/고블린10/스켈10) | 추가 카드 획득 기회 |
| `_enterHeight` | `ArenaEncounterTrigger` | 홀 바닥 높이 ±1 (현재 13) | 봉인 인카운터 발동 타이밍. 너무 낮으면 조기 발동, 높으면 미발동 |
| `_bossHpScale` / `_bossSpeedScale` | `ArenaEncounterTrigger` (2단용) | 1.0 ~ 2.0 (기본 1.0) | 재무장 보스 강화(2차 보스를 1차보다 세게) |
| `_nextMapIndex` / `_isFinalStage` | `StageGate` | 진행 체인 정의(1→2→final) | 스테이지 순서·종착. **오설정 시 흐름 붕괴** |
| `_autoOpenOnBossDied` | `StageGate` | 일반=true / 2단 최종게이트=false | 게이트 개방 주체 |

비-노브(고정 설계): 해금은 단조 증가(F1), 스테이지 수 N=3, 위치 폴링 판정 방식.

## 8. Acceptance Criteria

각 항목은 QA가 합/불 판정 가능해야 한다.

**해금 진행도**
- [ ] 진행도 초기화 후 맵 선택 화면에서 Stage1만 선택 가능, Stage2/3은 잠금(LockedSprite·클릭 불가)으로 표시된다.
- [ ] Stage1 클리어(게이트 통과) 후 메인으로 나갔다 재진입 시 Stage2가 해금되어 선택 가능하다.
- [ ] 앱을 완전히 종료 후 재실행해도 해금 진행도가 유지된다(PlayerPrefs 영속).
- [ ] 이미 Stage3까지 해금된 상태에서 Stage1을 다시 골라 클리어해도 해금이 줄지 않는다.
- [ ] 디버그 메뉴 `Stage Progress ▸ Reset` 실행 시 즉시 Stage1만 해금 상태로 돌아간다.

**게이트·진행 체인**
- [ ] 보스를 잡기 전 게이트 위치를 지나가도 다음 맵으로 넘어가지 않는다.
- [ ] 보스 처치 후 빛기둥이 등장하고 미니맵 화살표가 게이트를 가리킨다.
- [ ] 게이트 통과 시 Stage1→2, Stage2→3으로 정확히 전환된다(엉뚱한 맵 로드 없음).
- [ ] Stage3 최종 게이트 통과 시 GameClear(Win) 화면으로 전환된다.
- [ ] 게이트를 빠르게 두 번 밟아도 씬이 한 번만 로드된다(이중 통계·이중 로드 없음).

**난이도 배율**
- [ ] 각 맵 진입 시 `StageDifficulty` 값이 해당 맵 인스펙터 값으로 설정된다(이전 맵 값 잔류 없음).
- [ ] 동일 잡몹이 스켈레톤 맵에서 슬라임 맵과 다른 데미지/HP를 보인다(배율≠1일 때).

**Stage3 2단 보스**
- [ ] 중앙홀 진입 시 봉인벽이 닫히고 1차 웨이브/보스가 시작된다(통로로 되돌아갈 수 없다).
- [ ] 1차 보스 처치 시 상층 진입 봉인이 열리고 미니맵이 상층 진입로로 안내한다.
- [ ] 상층 진입 시 2차 보스 인카운터가 시작된다(WaveSpawner 재무장 정상).
- [ ] 2차(최종) 보스 처치 시에만 최종 게이트가 열린다(1차 처치로는 안 열림).

---

> **문서 완료** (8/8 섹션). 코드 전수 기반 검증. 후속: systems-index 상태 갱신, 의존 GDD 양방향 참조 추가.
