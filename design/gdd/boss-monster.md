# 보스 몬스터 시스템 (Boss Monster)

> **System #24** (systems-index) | Category: Gameplay | Status: Implemented
> 관련 코드: `BossEnemy.cs`, `BossAI.cs`, `BossAttackBase.cs`, `BossAttackOrchestrator.cs`, `GroundSlamAttack.cs`, `AreaBarrageAttack.cs`, `SummonAttack.cs`, `ChargeAttack.cs`, `TelegraphIndicator.cs`, `ImpactBurst.cs`, `BossHpBarUI.cs`, `BossBanner.cs`
> 프리팹: `Assets/Prefabs/Enemy/Slime/Boss_Slime.prefab`, `Assets/Prefabs/Enemy/Goblin/Boss_Goblin_Purple.prefab`, `Assets/Prefabs/Enemy/Skeleton/Boss_Skeleton.prefab`
> 작성: 2026-06-09 (코드 전수 기반)

---

## 1. Overview

보스 몬스터 시스템은 각 스테이지 끝에서 등장하는 **3종 보스(슬라임 킹·고블린 킹·본 로드)**의 이동·공격·사망 흐름을 구현한다. 잡몹과 달리 보스는 오브젝트 풀을 사용하지 않으며, `BossEnemy`(`EnemyBase` 상속)가 전체 생명주기를 관장한다. 이동과 공격은 각각 `BossAI`(추적/점프)와 `BossAttackOrchestrator`+`BossAttackBase` 서브클래스(AOE/소환/돌진)로 분리되어 있어, 보스 프리팹에 원하는 `BossAttackBase` 컴포넌트를 추가하는 것만으로 공격 레퍼토리가 달라진다.

공격은 **텔레그래프(빨간 펄스 원) → 윈드업 대기 → 임팩트**의 3단계 라이프사이클로 진행되며, `SnapToGround`(Environment 레이어 레이캐스트)가 멀티레벨 맵에서 표시 높이를 바닥 기준으로 자동 보정한다. 보스 사망 시 `BossEnemy.OnBossDied` 정적 이벤트를 발행하여 `StageGate`(자동 게이트 개방)·`ArenaPhaseManager`(2단 보스 페이즈 전이)·UI가 반응한다.

핵심 구성요소 요약:

| 요소 | 역할 |
|---|---|
| `BossEnemy` | 생명주기, 사망 연출, 정적 이벤트 발행 |
| `BossAI` | 이동(걷기/점프), 접촉 데미지, `SuspendMovement` |
| `BossAttackOrchestrator` | 공격 컴포넌트 라운드로빈 발동, 쿨다운 관리 |
| `BossAttackBase` (추상) | 텔레그래프→임팩트 라이프사이클, `SnapToGround` |
| `GroundSlamAttack` | 단일 원 AOE (플레이어 또는 보스 위치) |
| `AreaBarrageAttack` | 원 4개 동시 폭격 (플레이어 주변 분산) |
| `SummonAttack` | 잡몹 4마리 소환 |
| `ChargeAttack` | 직선 돌진, `SuspendMovement` 제어 |
| `TelegraphIndicator` / `ImpactBurst` | 공격 시각 피드백 |
| `BossHpBarUI` / `BossBanner` | 보스 전용 HUD |

---

## 2. Player Fantasy

**"겨우 이겼다" — 일방적 압도가 아니라 치열한 대결 끝의 처치감.**

잡몹 떼를 정리하다가 보스가 등장하는 순간, 화면 중앙 배너("SLIME KING 등장!")와 하단 HP 바가 뜨며 긴장감이 고조된다. 보스는 단순히 체력만 많은 것이 아니라 발밑에 빨간 원이 점점 빠르게 펄스치며 "지금 여기서 피해야 한다"는 신호를 보낸다. 대시로 원을 벗어나 한숨 돌리는 순간의 쾌감, 그리고 놓쳐서 큰 피해를 받는 위기감이 번갈아 온다.

보스별로 다른 이동·공격 패턴은 보스마다 다른 전략을 요구한다. 슬라임 킹은 넓은 착지 AOE로 접근을 막고, 고블린 킹은 예측 불가 돌진과 소환으로 주의를 분산시키며, 본 로드는 여러 원이 동시에 내려오는 포격으로 안전지대를 줄인다. 처치 순간 "보스 처치!" 배너가 노란빛으로 뜨는 것이 완료감의 마침표가 된다.

---

## 3. Detailed Rules

### 3.1 공통 생명주기

- 보스는 `WaveSpawner`가 `EnemyType.Boss` 슬롯에서 꺼내 `Activate(playerTransform)`으로 활성화한다.
- 활성화(`OnSpawn`) 직후 `BossEnemy.OnBossSpawned` 정적 이벤트 발행 → `BossHpBarUI`·`BossBanner` 구독 반응.
- `BossAI.SuspendMovement`와 `_boss.IsActive` 플래그 둘 다 true여야 이동·공격이 진행된다.
- `DropItemEffects.TimeStopActive` 또는 `GameState != Playing`이면 이동·공격 완전 정지(BossAI Update, BossAttackOrchestrator Update 모두 조기 반환).
- 사망 시 `HandleDeath` 오버라이드: `Deactivate()` → `OnBossDied` 발행 → 골드 즉시 지급(`_goldReward=200`) → `FireEnemyDied(0f, pos)` → `DeathSequence` 코루틴(1초 대기) → `StageGate`가 있으면 Win 생략(게이트가 맵 전환 담당), 없으면 `GameManager.ChangeState(Win)`.
- 보스는 오브젝트 풀에 반환하지 않는다. 사망 연출 후 `gameObject.SetActive(false)`.

### 3.2 이동 — 2모드

**걷기 모드** (`_jumpMovement = false`, 고블린 킹·본 로드):
- 매 Update, 플레이어 방향 XZ 벡터를 `Rigidbody.linearVelocity`에 직접 적용.
- 속도: `_moveSpeed = 2.0f` (m/s).
- `SuspendMovement = true`면 수평 속도 0으로 강제(Y축 물리는 유지).
- 이동 중 `Quaternion.RotateTowards`로 360°/s 회전.

**점프 이동 모드** (`_jumpMovement = true`, 슬라임 킹):
- 점프 사이(`_jumpInterval = 0.9초`) 동안: `SlowCrawlAndFace()` — 플레이어 방향으로 `_jumpWalkSpeed = 1.2 m/s` 저속 이동 + 주시.
- 타이머 만료 → `JumpHopRoutine` 코루틴:
  1. Rigidbody를 kinematic으로 전환, 수평 위치 키프레임 제어.
  2. 목표 = 플레이어 방향, `min(실제거리, _maxHopDistance=10m)` 지점.
  3. 포물선 보간: `y = start.y + _jumpHeight × 4u(1−u)`, `u = t/_jumpDuration`.
  4. `_jumpDuration = 0.8초` 도달 → 착지. 동시에 `_slamTelegraphPrefab`을 `_jumpDuration`만큼 표시(착지 예고).
  5. 착지 시 `_slamRadius = 7.5m` OverlapSphere → 플레이어 `_slamDamage = 30` 적용 + `ImpactBurst`.
  6. Rigidbody kinematic 해제. `_jumping = false`.

### 3.3 공격 시스템 — 오케스트레이터 + 라이프사이클

**BossAttackOrchestrator** (고블린 킹·본 로드에 부착; 슬라임 킹에는 없음):
- Awake에서 `GetComponents<BossAttackBase>()` → 부착 순서대로 `_attacks[]` 배열.
- Start에서 `_timer = _initialDelay` (슬라임: 해당 없음 / 고블린: 3.0초 / 본 로드: 1.5초).
- Update: 모든 공격의 `IsFiring`이 false이고 `_timer <= 0`이면 `_attacks[_nextIdx].TryFire(boss, player)`.
  - 발동 성공: `_timer = atk.Cooldown + _intervalBetweenAttacks`.
  - 발동 실패(이미 발동 중 등): `_timer = _intervalBetweenAttacks`.
  - `_nextIdx` 라운드로빈 증가.

**BossAttackBase 라이프사이클** (모든 공격 공통):
```
TryFire() 호출
  → _firing = true
  → FireRoutine() 코루틴 시작
    → TelegraphIndicator.Setup(pos, radius, windupDuration) — 빨간 펄스 원 표시
    → WaitForSeconds(_windupDuration)
    → ImpactBurst.Setup(pos, radius) — 오렌지 폭발 시각
    → Physics.OverlapSphere(pos + Vector3.up * 1.5f, radius) — 플레이어 검출
    → DamageDealer.Deal(damage) — 데미지 적용
    → _firing = false
```
- `SnapToGround(pos)`: origin = pos + (0, 12, 0)에서 아래로 최대 80m 레이캐스트, 레이어 8(Environment) 첫 히트 y + 0.05f로 보정. 레이 미히트 시 원래 y 유지.
- `TelegraphIndicator`: Quad 메시 수평 배치, 사인파 펄스(주파수 `_pulseFreq = 6f`, 시간 지날수록 가속), windupDuration 만료 후 자동 소멸.
- `ImpactBurst`: 0→`radius * 2f`로 `_duration = 0.45초` 동안 확장 + 알파 페이드.

### 3.4 보스 3종 상세 패턴

**슬라임 킹 (Boss_Slime)**

| 항목 | 값 |
|---|---|
| HP | 1000 |
| 이동 | 점프 전용(`_jumpMovement = true`) |
| 점프 간격 | 0.9초 |
| 점프 이동 거리(최대) | 10m |
| 점프 높이 | 9m |
| 점프/착지 시간 | 0.8초 |
| 착지 AOE 반경 | 7.5m |
| 착지 데미지 | 30 |
| 접촉 데미지 | 25 / 0.8초 간격 |
| 접촉 반경 | 2.5m |
| BossAttackOrchestrator | 없음 (착지 슬램만) |
| 골드 보상 | 200 |

착지 예고: `_slamTelegraphPrefab`을 착지 목표 지점에 0.8초(점프 시간) 동안 표시 → 플레이어가 범위를 벗어날 시간 제공.

**고블린 킹 (Boss_Goblin_Purple)**

| 항목 | 값 |
|---|---|
| HP | 1000 |
| 이동 | 걷기(moveSpeed=2.0) |
| BossAttackOrchestrator | 있음, _initialDelay=3.0초, _intervalBetweenAttacks=1.0초 |
| SummonAttack | cooldown=8초, windup=1.2초, 잡몹 4마리, 소환 반경=4m |
| ChargeAttack | cooldown=5초, windup=0.9초, 데미지=35, 돌진속도=20m/s, 지속=0.7초, 히트반경=2m |
| 접촉 데미지 | 25 / 0.8초 간격 |
| 골드 보상 | 200 |

패턴 순서(라운드로빈): SummonAttack → ChargeAttack → SummonAttack → ChargeAttack → …

ChargeAttack 흐름:
1. 플레이어 방향 고정 후 `TelegraphIndicator` 표시(windup=0.9초).
2. `BossAI.SuspendMovement = true` → 20m/s 직진, `Rigidbody.linearVelocity` 직접 제어.
3. 돌진 경로 내 플레이어 히트반경(2m) 진입 시 35 데미지 1회.
4. 0.7초 경과 또는 보스 사망 → 속도 0, `SuspendMovement = false`.

**본 로드 (Boss_Skeleton)**

| 항목 | 값 |
|---|---|
| HP | 1000 |
| 이동 | 걷기(moveSpeed=2.0) |
| BossAttackOrchestrator | 있음, _initialDelay=1.5초, _intervalBetweenAttacks=0.8초 |
| GroundSlamAttack | cooldown=4초, windup=1.5초, 데미지=50, 반경=4m, _targetPlayer=true |
| AreaBarrageAttack | cooldown=4초, windup=1.4초, 데미지=35, 반경=3.5m, 원 4개, spread=8m, _aroundPlayer=true |
| SummonAttack | cooldown=6초, windup=1.3초, 잡몹 4마리, 소환 반경=4m |
| 접촉 데미지 | 25 / 0.8초 간격 |
| 골드 보상 | 200 |

패턴 순서(라운드로빈): GroundSlamAttack → AreaBarrageAttack → SummonAttack → GroundSlamAttack → …

AreaBarrageAttack 흐름:
1. 플레이어 주변(`_aroundPlayer=true`)에 `_circleCount=4`개의 위치를 `Random.insideUnitCircle * _spread` 오프셋으로 결정, 각각 `SnapToGround` 적용.
2. 4개 `TelegraphIndicator`를 동시에 표시(windup=1.4초).
3. windup 만료 → 4개 위치 동시 OverlapSphere 데미지 판정. 각 원에서 플레이어 1회씩(중복 가능).

### 3.5 UI 피드백

**BossBanner**: `OnBossSpawned` → "{BossName} 등장!" (빨간색, 1.6초 유지 + 0.5초 페이드 인/아웃). `OnBossDied` → "보스 처치!" (노란색, 같은 타이밍). 2단 보스(Stage3)에서도 각 스폰·사망마다 표시.

**BossHpBarUI**: `OnBossSpawned` → 활성화, HP 바 색을 `boss.HpBarColor`로 설정, 이름 표시. `HealthComponent.OnHealthChanged` → `fillAmount = CurrentHp / MaxHp`. `OnBossDied` → 비활성화.

---

## 4. Formulas

**변수 정의**

| 변수 | 의미 | 범위 |
|---|---|---|
| `Hp_base` | 보스 프리팹 `HealthComponent._maxHp` | > 0 (현재 3종 모두 1000) |
| `m_hp` | `StageDifficulty.EnemyHpMultiplier` | 0.5 ~ 3.0 (기본 1.0) |
| `Hp_final` | 실제 보스 최대 HP | `Hp_base × m_hp` |
| `t` | 착지 보간 타임 파라미터 | 0 ~ `_jumpDuration` |
| `u` | 정규화 타임 파라미터 | `t / _jumpDuration` ∈ [0, 1] |
| `_slamRadius` | 착지 AOE 반경(m) | 슬라임=7.5 |
| `_spread` | AreaBarrage 분산 반경(m) | 본 로드=8 |
| `_circleCount` | AreaBarrage 동시 원 수 | 본 로드=4 |
| `T_next` | 다음 공격까지 대기 시간(초) | `atk.Cooldown + _intervalBetweenAttacks` |

**F1 — 보스 최대 HP (난이도 배율 적용)**
```
Hp_final = Hp_base × m_hp
```
예: Hp_base=1000, m_hp=1.0 → 1000. m_hp=1.5 적용 시 → 1500.

**F2 — 점프 포물선 Y 위치 (슬라임 킹)**
```
pos.y = start.y + _jumpHeight × 4 × u × (1 − u)
pos.xz = Lerp(start.xz, target.xz, u)
u = Clamp01(t / _jumpDuration)
```
예: start.y=0, _jumpHeight=9, u=0.5 → 0 + 9×4×0.5×0.5 = 9.0 (정점). u=1.0 → 0 (착지).

**F3 — 착지 슬램 데미지 판정 (슬라임 킹)**
```
OverlapSphere(target, _slamRadius=7.5)
→ Player 태그 + HealthComponent 있으면 DamageDealer.Deal(_slamDamage=30)
```
예: 플레이어가 착지 목표에서 5m 이내 → 30 피해.

**F4 — AreaBarrage 원 위치 결정 (본 로드)**
```
for i in [0, _circleCount):
  off = Random.insideUnitCircle × _spread
  pos[i] = SnapToGround(center + (off.x, 0, off.y))
center = player.position (_aroundPlayer=true)
```
예: spread=8, 4개 원 → 플레이어 반경 0~8m 이내 랜덤 4지점 동시 타격.

**F5 — 오케스트레이터 다음 발동 타이머**
```
T_next = atk.Cooldown + _intervalBetweenAttacks
```
예 (본 로드 GroundSlam): T_next = 4 + 0.8 = 4.8초 후 다음 공격. 발동 실패 시: T_next = 0.8초(즉시 재시도).

**F6 — 접촉 데미지 (BossAI)**
```
sqrDist = (boss.xz − player.xz)²
if sqrDist < _contactRadius²:
  _contactTimer -= Time.deltaTime
  if _contactTimer ≤ 0: Deal(_contactDamage); _contactTimer = _contactInterval
else:
  _contactTimer = 0
```
예: contactDamage=25, contactInterval=0.8초 → 보스 접촉 시 1.25초당 평균 25 피해(최초 접촉은 즉시).

---

## 5. Edge Cases

| 상황 | 처리 |
|---|---|
| **보스 사망 중 `TryFire` 호출** | `TryFire`에서 `boss.Health.IsAlive` 검사 → false이면 코루틴 시작 안 함. |
| **`FireRoutine` 진행 중 보스 사망** | `TelegraphAndImpact`/`AreaBarrageAttack`은 windup `WaitForSeconds` 완료 후 `bossAlive` 재확인 → 데미지 생략, `_firing = false` 정상 복귀. `ChargeAttack`은 루프에서 `boss.Health.IsAlive` 매 프레임 검사 → break. |
| **점프 중 보스 사망 (슬라임 킹)** | `JumpHopRoutine` 내 `_boss.Health.IsAlive` 검사 → break → kinematic 해제, `_jumping = false` 복귀. 착지 데미지는 적용되지 않음. |
| **`SnapToGround` 레이 미히트** | Environment 레이어 바닥을 찾지 못하면 원래 pos.y 유지. 텔레그래프/임팩트가 바닥과 다른 높이에 표시될 수 있음 → 맵에 Environment 레이어 콜라이더 필수. |
| **`ChargeAttack` 중 TimeStop** | `DropItemEffects.TimeStopActive` → `Rigidbody.linearVelocity`를 0으로 강제(y 유지). TimeStop 해제 후 남은 `_chargeDuration` 재개. `SuspendMovement`는 TimeStop 후에도 true이므로 정지 해제 후 돌진 재개. |
| **`SummonAttack` 중 ObjectPool 풀 고갈** | `ObjectPool.Instance.GetFromPool(EnemyType.Mob)` == null → 그 슬롯 소환 건너뜀. 나머지 count는 정상 소환 시도. `_summonCount=4`이지만 0~4마리 소환될 수 있음. |
| **`BossAttackOrchestrator` 공격 배열 비어있음** | Update 첫 줄에서 조기 반환 → 공격 없이 보스는 BossAI 이동만 수행. |
| **보스 스폰 인빈시빌리티 중 피격** | `EnemyBase._spawnInvincibilityDuration = 0.5초` 동안 `HealthComponent`가 피해를 차단. |
| **StageGate 없는 맵에서 보스 처치** | `FindFirstObjectByType<StageGate>() == null` → 1초 후 `GameManager.ChangeState(Win)` 직접 호출. |
| **Stage3 2단 보스 — 보스1 사망 후 보스2 스폰 전 BossHpBarUI 상태** | `OnBossDied` 수신 즉시 HP 바 비활성화. 보스2 스폰 시 `OnBossSpawned`로 재활성화. 두 이벤트 사이 HP 바가 잠깐 숨겨짐 — 의도된 동작. |
| **보스 처치 후 `_deathDelay(1초)` 동안 게임 상태** | Playing 상태 유지. `DeathSequence` 코루틴이 `WaitForSecondsRealtime`이므로 TimeScale 무관. 이 1초 동안 잡몹은 계속 이동. |
| **`AreaBarrageAttack` 원 일부가 맵 밖** | `Random.insideUnitCircle`은 제한 없음. SnapToGround가 바닥 못 찾으면 원래 y로. 맵 경계 밖 원은 시각만 표시되고 플레이어 OverlapSphere 히트 없음(플레이어가 밖에 없으므로). |

---

## 6. Dependencies

**이 시스템이 의존하는 것 (upstream)**

| 시스템 | 용도 |
|---|---|
| `HealthComponent` | 보스 HP 추적, `IsAlive` 검사, `OnHealthChanged` 이벤트 |
| `DamageDealer` | 공격 데미지 적용(`Deal(DamageInfo, HealthComponent)`) |
| `ObjectPool` | `SummonAttack`의 잡몹 소환(`GetFromPool(EnemyType.Mob)`) |
| `WaveSpawner` | 보스 스폰 트리거 및 `Activate(playerTransform)` 호출 |
| `DropItemEffects` | `TimeStopActive` 조회 — 이동/공격 일시정지 |
| `GameManager` | `CurrentState` 조회 / `ChangeState(Win)` 호출(StageGate 없는 경우) |
| `GoldSystem` | 보스 처치 시 `AddGold(_goldReward=200)` |
| `EnemyBase` | `_isActive`, `_playerTransform` 상태 제공(BossAI가 읽음) |
| `StageDifficulty` | `EnemyHpMultiplier` — 보스 최대 HP 배율 적용처는 WaveSpawner |

**이 시스템에 의존하는 것 (downstream)**

| 시스템 | 관계 |
|---|---|
| `StageGate` | `BossEnemy.OnBossDied` 구독 → `_autoOpenOnBossDied=true`인 게이트 자동 개방 |
| `ArenaPhaseManager` | `BossEnemy.OnBossDied` 구독 → 보스 사망 카운트로 Stage3 페이즈 전이 |
| `BossHpBarUI` | `OnBossSpawned`/`OnBossDied` 구독 → HP 바 표시/숨김 |
| `BossBanner` | `OnBossSpawned`/`OnBossDied` 구독 → 등장/처치 배너 표시 |
| `RunStats` | `FireEnemyDied` → `OnEnemyDied` 구독으로 처치 수 통계 누적 |
| `EnemyBase`(잡몹) | `SummonAttack`이 소환한 잡몹은 `EnemyBase.Activate`로 플레이어 추적 시작 |

**양방향 문서 갱신 필요 (design-docs 규칙)**
- `stage-progression.md` → 이미 `OnBossDied` 의존 기재됨 (Section 6). 본 문서를 upstream 참조에 추가 권장.
- `wave-spawner.md` (신규 작성 시) → `EnemyType.Boss` 스폰 및 `SummonAttack` 잡몹 소환 출처로 본 문서 상호 참조 필요.
- `enemy-ai.md` → `ChaserAI._canJumpSlam`(슬라임 종족 연관 잡몹 점프 슬램 옵션) 언급 추가 권장.

---

## 7. Tuning Knobs

| 노브 | 위치 | 안전 범위 | 영향받는 게임플레이 |
|---|---|---|---|
| `_maxHp` (보스 공통) | 각 보스 프리팹 `HealthComponent` | 500 ~ 3000 (현재 1000) | 전투 지속 시간. ↑ = 라운드 길어짐, 빌드 강도 중요도 상승 |
| `_goldReward` | `BossEnemy._goldReward` | 100 ~ 500 (현재 200) | 보스 처치 후 빌드 구매력. ↑ = 다음 스테이지 준비도 향상 |
| `_deathDelay` | `BossEnemy._deathDelay` | 0.5 ~ 3.0초 (현재 1.0초) | 처치 연출 여유감. 너무 짧으면 씬 전환이 갑작스러움 |
| `_initialDelay` | `BossAttackOrchestrator` | 1.0 ~ 5.0초 (고블린=3.0, 본 로드=1.5) | 보스 등장 직후 여유 시간. ↓ = 즉시 압박 |
| `_intervalBetweenAttacks` | `BossAttackOrchestrator` | 0.5 ~ 3.0초 (고블린=1.0, 본 로드=0.8) | 공격 밀도. ↓ = 쉴 틈 없음, ↑ = 위협도 감소 |
| `_jumpInterval` (슬라임) | `BossAI` | 0.5 ~ 3.0초 (현재 0.9초) | 점프 빈도. ↓ = 착지 AOE 빈도↑, 더 위협적 |
| `_slamRadius` (슬라임) | `BossAI` | 3.0 ~ 12.0m (현재 7.5m) | 착지 AOE 범위. ↑ = 회피 어려움. **8m 초과 시 대시 회피 불가능 구간 발생** |
| `_slamDamage` (슬라임) | `BossAI` | 10 ~ 60 (현재 30) | 착지 피격 패널티. ↑ = 회피 실패 비용↑ |
| `_jumpHeight` (슬라임) | `BossAI` | 3 ~ 15m (현재 9m) | 점프 체공 높이. 시각적 위압감. 기능에는 영향 없음 |
| GroundSlam `_cooldown` (본 로드) | `GroundSlamAttack` | 2 ~ 8초 (현재 4초) | 플레이어 조준 원 빈도 |
| GroundSlam `_damage` (본 로드) | `GroundSlamAttack` | 25 ~ 80 (현재 50) | 회피 실패 비용. **50 이상이면 회피 필수** |
| AreaBarrage `_spread` (본 로드) | `AreaBarrageAttack` | 3 ~ 15m (현재 8m) | 원 분산 범위. ↓ = 안전지대 넓음, ↑ = 어느 방향도 안전하지 않음 |
| AreaBarrage `_circleCount` (본 로드) | `AreaBarrageAttack` | 2 ~ 6 (현재 4) | 동시 타격 지점 수. ↑ = 더 어려움, 성능 주의 |
| Charge `_chargeSpeed` (고블린) | `ChargeAttack` | 10 ~ 30 m/s (현재 20) | 돌진 회피 가능 여부. **25 초과 시 체감 불가 구간** |
| Charge `_windupDuration` (고블린) | `ChargeAttack` | 0.5 ~ 2.0초 (현재 0.9초) | 조준 예고 시간. ↓ = 반응 어려움 |
| Summon `_summonCount` (고블린·본 로드) | `SummonAttack` | 2 ~ 8 (현재 4) | 동시 잡몹 압박. **6 이상이면 풀 고갈 주의** |
| `_contactDamage` (보스 공통) | `BossAI` | 10 ~ 40 (현재 25) | 보스 본체 접촉 위험도. 무기 자동발동 위주 게임이므로 적절 유지 필요 |

비-노브(고정 설계): 3종 보스 구성(스테이지당 1종), 라운드로빈 순서(배열 인덱스 고정), `SnapToGround` 레이어 8 고정, 플레이어 1명 가정.

---

## 8. Acceptance Criteria

QA가 합/불 판정 가능한 체크리스트.

**공통 — 생명주기 및 UI**
- [ ] 보스 스폰 즉시 화면 중앙에 "{BossName} 등장!" 배너가 빨간색으로 표시되고 약 2.6초(페이드인 0.5 + 유지 1.6 + 페이드아웃 0.5) 후 사라진다.
- [ ] 보스 스폰 직후 하단에 보스 HP 바와 이름이 표시된다.
- [ ] 보스가 피해를 받을 때마다 HP 바 fillAmount가 즉시 갱신된다.
- [ ] 보스 처치 시 "보스 처치!" 배너가 노란색으로 표시되고 HP 바가 숨겨진다.
- [ ] 보스 처치 후 1초 이내에 `StageGate`가 열리거나(StageGate 있는 맵) `Win` 상태로 전환된다(StageGate 없는 맵).
- [ ] 보스 스폰 후 0.5초 동안 어떤 피해를 받아도 HP가 감소하지 않는다(스폰 무적).

**슬라임 킹 — 점프 이동 및 착지 슬램**
- [ ] 보스가 걷지 않고 점프로만 이동하며, 점프 사이(0.9초) 동안 느리게 플레이어를 향해 이동한다.
- [ ] 점프 시작과 동시에 착지 목표 지점에 빨간 원이 약 0.8초 동안 표시된다.
- [ ] 플레이어가 착지 원(반경 7.5m) 내에 있을 때 착지 시 30 피해를 받는다.
- [ ] 플레이어가 원 밖으로 이동하면 착지 시 피해를 받지 않는다.
- [ ] `DropItemEffects.TimeStop` 발동 중 보스가 이동·점프를 멈춘다.

**고블린 킹 — 소환 + 돌진**
- [ ] 보스 스폰 3초 후 첫 공격(소환 또는 돌진)이 시작된다.
- [ ] SummonAttack: 윈드업(1.2초) 동안 보스 주변 빨간 원이 표시된 뒤 잡몹 4마리가 원형으로 소환되고 플레이어를 추적한다.
- [ ] ChargeAttack: 보스가 플레이어 방향을 향한 뒤 윈드업(0.9초) → 직선으로 고속 돌진하며 경로 내 플레이어에 35 피해를 1회 입힌다.
- [ ] 돌진 중 보스의 일반 추적 이동이 멈춘다(SuspendMovement).
- [ ] 돌진 완료(0.7초) 후 보스가 다시 일반 추적을 재개한다.
- [ ] 패턴이 소환→돌진→소환→돌진 순서로 반복된다.

**본 로드 — 다중 패턴**
- [ ] 보스 스폰 1.5초 후 첫 공격이 시작된다.
- [ ] GroundSlamAttack: 플레이어 위치에 빨간 원(반경 4m)이 1.5초 표시된 뒤 범위 내 플레이어에 50 피해를 입힌다.
- [ ] AreaBarrageAttack: 플레이어 주변에 빨간 원 4개가 동시에 1.4초 표시된 뒤 각 원 범위가 동시에 터진다. 각 원에 히트 시 35 피해.
- [ ] SummonAttack: 잡몹 4마리가 소환된다.
- [ ] 패턴이 GroundSlam→AreaBarrage→Summon 순서로 반복된다.

**멀티레벨 대응 — SnapToGround**
- [ ] Stage3(멀티레벨 맵) 상층에서 보스 공격 텔레그래프 원이 허공이나 지하가 아닌 실제 바닥 위에 표시된다.
- [ ] ImpactBurst 이펙트가 바닥 표면 위(+0.5m)에서 발생한다.

**Stage3 2단 보스**
- [ ] 1차 보스 처치 후 HP 바가 숨겨지고, 2차 보스 스폰 시 HP 바가 다시 표시된다.
- [ ] 1차 처치 시 게임이 Win으로 전환되지 않는다(ArenaPhaseManager가 처리).
- [ ] 2차(최종) 보스 처치 후 최종 게이트가 열린다.

---

> **문서 완료** (8/8 섹션). 코드 전수 기반 검증. 후속: systems-index 상태 갱신, 의존 GDD(stage-progression, wave-spawner, enemy-ai) 양방향 참조 추가.
