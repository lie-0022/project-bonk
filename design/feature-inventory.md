# 검나쎄짐 (BladeSurge) — 기능 인벤토리 & 구현 기법 정리

> 기획서 작성용 베이스 문서. 코드베이스 전수 조사 + 유저 시나리오 점검 기반 (2026-06-08).
> 표기: ✅ 구현완료 / 🟡 부분(placeholder·미연결) / ❌ 미구현

---

## 0. 개요

- **장르**: 3D 핵앤슬래시 + 뱀서라이크(자동공격·웨이브·레벨업 빌드) 라운드 생존
- **엔진**: Unity 6.3 LTS / URP / C# / New Input System
- **플랫폼**: PC(Windows) MVP
- **코어 루프**: 직업 선택 → 스테이지 입장 → 자동공격으로 웨이브 생존 → XP/골드 수집 → 레벨업/상자로 무기·패시브 빌드 → 보스 처치 → 다음 스테이지 → 3스테이지 클리어 → 누적 점수 + 온라인 랭킹 등록
- **3직업 = 3무기**: 전사=검(근접 부채꼴) / 거너=총(직선 관통) / 마법사=마법(광역 폭발)

---

## 1. 화면/씬 흐름

```
[MainMenu] ──시작──> [CharacterSelect](직업+맵 선택) ──확인──> [GamePlay]
                                                                  │
   (맵0만 입장 시) IntroCutscene(타자기 3컷) ──> 게임 시작        │
                                                                  ▼
                          스테이지 진행(웨이브→보스→게이트) ── 사망 ─> [GameOver]
                                                                  │           (단일 런 결과)
                                                       3스테이지 클리어
                                                                  ▼
                                                            [GameClear]
                                                   (누적 점수 + BEST + 온라인 랭킹 등록)
```
- 씬 5개: MainMenu / CharacterSelect / GamePlay / GameOver / GameClear
- 스테이지 전환은 GamePlay 씬 리로드 + `GameSession.SelectedMapIndex` 교체
- Editor: `PlayModeStartScene` — ▶ 누르면 항상 MainMenu에서 시작(개발 편의)

---

## 2. 기능 전체 리스트 (카테고리별)

### 2-1. 플레이어 / 조작 ✅
- 8방향 이동, 점프(패시브로 다단 점프), **대시(0.2초, 무적, 1.5초 쿨)** — New Input System + CharacterController
- 3인칭 오빗 카메라: 마우스 회전(yaw/pitch), **벽 통과 방지(SphereCast)**, 커서 자동 잠금/해제
- **무기별 캐릭터 모델 자동 전환** (선택 직업에 맞춰 Visual_Root 토글)
- 자동 공격(`BasicAttack`/`WeaponSystem`) — 입력 없이 주기적으로 가까운 적 타격

### 2-2. 무기 시스템 ✅
- **최대 3슬롯**, 각 무기 독립 타이머로 자동 발동 (`WeaponSystem` 싱글턴)
- 무기 레벨 1~15, **마일스톤(4단계)**: Lv1-4 / 5-9 / 10-14 / 15+ 마다 사거리·각도·관통·폭발반경 상승
- 무기 데이터는 **ScriptableObject(`WeaponDataSO`)** — 데미지/쿨다운/마일스톤 테이블
- 검: 부채꼴 회전 타격(다단), 총: 가까운 적 조준 발사(관통), 마법: 느린 발사체 → 명중 시 광역 폭발
- 발사체 풀링(`Projectile`, 관통/폭발/트레일/라이프스틸)

### 2-3. 전투 ✅
- `HealthComponent`(플레이어/적 공용): 회피확률, 무적(대시), 체력배율(웨이브)
- `DamageDealer`(static): **팀 구분**(Player→Enemy만, Enemy→Player만) + 난이도 데미지 배율
- `HitFlash`: 피격 흰색 플래시, 사망 회색 (이벤트 구독)

### 2-4. 진행 (XP/골드/레벨업/등급) ✅
- **XP**: 적 처치 → XP오브 → 수집 시 레벨업. 곡선 `100 + (Lv-1)*50`
- **골드**: 적 타입별(5/10), 상자 구매에 사용 (지수 가격 50×1.5^n)
- **레벨업 카드 선택**: 무기3+패시브12 혼합 풀에서 3장 제시, `timeScale=0` 일시정지, 리롤/스킵
- **등급 시스템**: Common/Epic/Unique/Legend — 무기 강화 보너스 + 패시브 가중치
- **등급 확률 + 행운(Luck) 패시브 반영** (`CardGradeRoller`): 기본 60/25/12/3% → 행운으로 상위등급 재분배

### 2-5. 패시브 (12종) ✅
- 생존: MaxHp / HpRegen / Dodge
- 무기: AttackSpeed / CritChance / CritDamage / Lifesteal / ProjectileCount
- 이동: MoveSpeed / ExtraJump
- 메타: Luck(등급 확률↑) / Difficulty(스폰·보상↑)
- **점감 공식(Diminishing Returns)** + 캡(회피60%/크리75%/행운80%), 공속 지수감소(0.95^Lv, floor 0.30)

### 2-6. 적 / 보스 ✅
- **잡몹**(`ChaserAI`): 추적 + 접촉 데미지. 풀링(IPoolable). 슬라임은 **점프 내려치기** 옵션
- **보스 3종 + 종족별 특화 패턴**:

| 보스 | 이동 | 패턴 |
|---|---|---|
| 슬라임 킹 (SLIME KING) | **점프로만 이동**(저속 추적+도약) | 착지 AOE + GroundSlam(단일 원) |
| 본 로드 (BONE LORD, 스켈레톤) | 걷기 | **AreaBarrage(빨간 원 4개 동시)** + Summon(졸개 소환) + GroundSlam |
| 고블린 킹 (GOBLIN KING) | 걷기 | **Summon(소환) + Charge(직선 돌진)** |

- 공격 구조: `BossAttackBase`(텔레그래프→임팩트, **바닥 스냅 SnapToGround**) + `BossAttackOrchestrator`(라운드로빈)
- 텔레그래프(`TelegraphIndicator` 빨간 펄스 원) + 임팩트(`ImpactBurst`)
- 보스 등장/처치 **배너**(`BossBanner`), 보스 HP바(`BossHpBarUI`)

### 2-7. 스폰 / 웨이브 / 스테이지 ✅
- **시간 기반 5웨이브**(`WaveSpawner`), 웨이브마다 스폰간격↓·HP·속도↑, 지정 웨이브 후 보스 스폰, 보스 프리워밍(프레임드롭 방지)
- 스폰 영역(`StageSpawnArea`, 원형/사각) — 맵 안쪽·바닥 보정 클램프
- **봉인 인카운터**(`ArenaEncounterTrigger`): 홀 진입 시 봉인벽 활성 + 웨이브 시작 (위치 폴링)
- **Stage3 2단 보스**(`ArenaPhaseManager`): 보스1 처치→상층 봉인 개방, 보스2 처치→최종 게이트. WaveSpawner **재무장**(BeginEncounter 오버로드)으로 한 씬에서 보스 2회
- **스테이지 게이트**(`StageGate`): 클리어 시 빛기둥 등장 + 미니맵 유도 + 다음 맵/Win
- **난이도 배율**(`StageDifficulty`): 맵별 적 HP/데미지/보상 배율

### 2-8. 아이템 (상자/항아리/드롭) ✅
- **상자**(`Chest`): E키 구매(골드, 지수 가격) → 레벨업 카드 선택. 가까운 1개만 상호작용
- **항아리**(`Jar`): E키 파괴 → 골드/XP 오브 다수 드롭
- **드롭 아이템**(`DropItemEffects`): **자석**(즉시 흡수) / **이속**(×1.5) / **타임스톱**(적 정지) — 5% 확률 드롭, 중첩 시 시간 연장. 시각은 🟡 placeholder 큐브
- XP/골드 오브: 봅 모션 + 흡수 비행(풀링), 자석 시 즉시

### 2-9. UI / HUD ✅
- HUD: HP바 / XP바 / 상단바(시간·골드·킬) / 무기슬롯(등급아이콘+Lv) / 패시브슬롯(아이콘+Lv) / 보스HP바 / 대시쿨
- **미니맵**(`MinimapManager`): 플레이어 중심·북향 고정, UI 마커(플레이어/적/보스/상자/코인/항아리), **보스 후 게이트 유도 마커 + 삼각형 화살표**(`MinimapObjective`)
- 레벨업/상자 카드 선택창, 인벤토리/스탯창(Tab, timeScale=0)
- 일시정지(ESC), 퀘스트 안내(`QuestUI`, 맵별 문구·페이즈 연동)
- **캐릭터 선택 2D Spine 프리뷰**(`CharacterSelectPreview`): 전사/마법사/거너 직업별 전환 (spine-unity SkeletonGraphic, 멀티텍스처)
- 맵 선택(`MapSelectController`): **해금된 맵만 선택**, 잠긴 맵 LockedSprite
- 게임클리어/오버 결과 화면

### 2-10. 메타 (저장/랭킹/해금/온라인) ✅
- **런 통계**(`RunStats`): 처치/생존시간/골드/레벨 → 종료 시 스냅샷
- **누적 점수**(`RunTotals`): 3스테이지 합산 = 골드×1 + 처치×10 + 최고레벨×100 + 스테이지×1000 + 시간(초)×1. **BEST는 PlayerPrefs 로컬 최고기록**
- **스테이지 해금**(`StageProgress`, PlayerPrefs): 처음 1스테이지만 → 클리어 시 다음 해금
- **온라인 리더보드**(`OnlineLeaderboard` + Firebase Realtime DB REST): 닉네임(3글자) + 누적점수 등록 → 상위 10 표시. 패키지 0개(UnityWebRequest)
- 옵션 영속(`SettingsService`, PlayerPrefs): 감도·음량
- **커스텀 커서**(`CursorManager`): 오버워치 커서, 기본/클릭 2상태(ForceSoftware)

### 2-11. 오디오 / VFX
- **BGM**(`AudioManager`, 김가연 5트랙): Stage1/보스/최종보스 ✅ 활성, MainMenu/GameOver 🟡 미후킹
- SFX: 점프/피격/사망/골드/상자/레벨업/카드 ✅, **공격음(검/총/마법) 🟡 비활성**
- VFX ✅: 검 슬래시(각도추종), 총 머즐, 마법 발사·폭발, 텔레그래프 AOE, 코인(VFX Graph)
- VFX ❌: 피격 파티클, 적 사망 파티클, 레벨업 발광, 드롭 아이템 시각

### 2-12. 연출 ✅
- **인트로 컷신**(`IntroCutscene`): 맵0 시작 전 검은 화면 타자기 3컷(스토리→타이틀), 클릭/ESC 진행, 로드 안정화
- 보스 등장/처치 배너, 클리어 빛기둥

---

## 3. 구현 기법 / 기믹 정리

| 기법 | 적용 사례 |
|---|---|
| **상태머신(Enum+Event)** | `GameManager.GameState`(Starting/Playing/Paused/GameOver/Win) 단일 진입점 |
| **싱글턴** | WeaponSystem, XPSystem, GoldSystem, ChestSystem, PlayerStats, AudioManager, ObjectPool, RunStats, MinimapManager, OnlineLeaderboard, CursorManager |
| **static 홀더(씬 유지)** | GameSession, RunTotals, StageProgress, StageDifficulty, SettingsService, MinimapObjective, DropItemEffects |
| **PlayerPrefs 영속** | 최고점수, 해금진행, 옵션, 닉네임 |
| **이벤트 구독(느슨한 결합)** | OnGameStateChanged, OnEnemyDied, OnBossSpawned/Died, OnGoldChanged, OnLevelUp, OnWeaponsChanged, OnStatsChanged, OnEncounterStarted |
| **객체 풀링(IPoolable)** | 잡몹, 발사체, XP/골드 오브 (`ObjectPool`) |
| **ScriptableObject(데이터 주도)** | 무기 데이터(마일스톤 테이블), 오디오 카탈로그 |
| **코루틴** | 보스/슬라임 포물선 점프, 돌진, 검 스윙, 텔레그래프, 컷신 타자기, 배너 페이드, 결과 화면 |
| **텔레그래프→임팩트** | 모든 보스 AOE: 예고 원 → windup → 타격. **SnapToGround**로 멀티레벨 바닥 보정 |
| **라운드로빈** | 보스 공격 패턴 순환(`BossAttackOrchestrator`) |
| **재무장(BeginEncounter 오버로드)** | 단일 WaveSpawner로 Stage3 2단 보스 |
| **위치 폴링** | 봉인 인카운터 진입 판정(CharacterController 트리거 불안정 회피) |
| **점감 공식 + 캡** | 패시브 스탯(회피/크리/행운), 공속 지수감소 |
| **가중치 샘플링** | 카드 등급 추첨(행운 재분배) |
| **Time.timeScale=0** | 일시정지/레벨업/스탯창/컷신 |
| **UnityWebRequest REST** | Firebase 리더보드(패키지 없이) |
| **Spine SkeletonGraphic** | 캐릭터 선택 2D 애니메이션(멀티텍스처) |
| **저폴리 콜라이더 프록시** | Stage3 거대 벽메시는 MeshCollider 금지 → 저폴리 Cube/Stairs + Environment 레이어(카메라 충돌) |

---

## 4. 유저 시나리오 워크스루 (점검)

### 시나리오 A — 신규 유저 첫 플레이
1. 앱 실행 → **MainMenu** (커서 표시, BGM 🟡 미후킹) → 시작
2. **CharacterSelect**: 직업 클릭 → 중앙 2D Spine 캐릭터 전환(전사/마법사/거너) ✅ / 맵 선택 → **1스테이지만 해금, 2·3 잠금** ✅ → 확인
3. **GamePlay(맵0)** 진입 → **타자기 컷신** 3컷 → 클릭/스킵 → 게임 시작 ✅
4. 자동공격으로 웨이브 생존, XP/골드 수집, 레벨업 카드로 무기·패시브 빌드 ✅
5. 5웨이브 후 **슬라임 킹**(점프 이동 + 착지 AOE, 등장 배너) ✅
6. 보스 처치 → 빛기둥 + 미니맵 화살표로 게이트 유도 → 통과 → **2스테이지 해금** ✅
- **점검 결과**: 흐름 정상. ⚠️ MainMenu BGM 미후킹.

### 시나리오 B — 진행 & 빌드 심화
1. 2스테이지(고블린: 소환+돌진), 3스테이지(스켈레톤: 원거리 범위+소환) ✅
2. 상자(골드 구매)로 추가 카드, 등급 운(행운 패시브) ✅
3. **Stage3 2단 보스**: 중앙홀 보스1 → 상층 봉인 개방 → 위층 보스2(강화) → 최종 게이트 ✅
- **점검 결과**: 보스 차별화 명확(점프/돌진/원거리). ⚠️ **Stage2 맵 GamePlay 미통합 가능성**(조사상 모델만 도입) — 실제 2스테이지 플레이 경로 확인 필요.

### 시나리오 C — 클리어 & 랭킹
1. 3스테이지 클리어 → **GameClear**: 좌측 누적 점수/내역(어두운 패널), 우측 온라인 랭킹 ✅
2. 닉네임(3글자) 입력 → 등록 → Firebase 저장 → 상위 10 표시 ✅ (연결 검증됨)
3. BEST(로컬 최고) 갱신 시 "신기록!" ✅
- **점검 결과**: 온라인 등록/조회 정상. ⚠️ 치팅 가능(공개 쓰기 규칙) — 캐주얼 감수.

### 시나리오 D — 사망 / 재시작
1. 체력 0 → **GameOver**: 단일 런 통계 → 재시작(런 리셋) / 메인 ✅
- **점검 결과**: 정상. ℹ️ GameOver는 누적점수 미표시(완주 시 GameClear에서만) — 의도.

### 시나리오 E — 메타/영속
1. 해금·BEST·옵션·닉네임은 **PC 로컬(PlayerPrefs/레지스트리)** 유지, 다른 PC 비동기화 ✅
- **점검 결과**: 정상. (디버그: `BladeSurge ▸ Stage Progress` 메뉴로 해금 초기화/전체해금)

---

## 5. 구현 상태 요약 (현재)

| 영역 | 상태 |
|---|---|
| 코어 루프·웨이브·무기·레벨업·HUD | ✅ |
| 3직업 + 무기별 모델 + **2D Spine 캐릭터 선택(3종)** | ✅ |
| 적/보스 3종 + 종족별 패턴(점프/돌진/원거리/소환) | ✅ |
| Stage3 2단 보스 아레나(MAP3_v2) | ✅ |
| 인트로 컷신 / 보스 배너 / 미니맵 유도 / 빛기둥 | ✅ |
| 랭킹(누적점수 + 로컬 BEST) + **온라인 리더보드(Firebase)** | ✅ |
| 스테이지 해금 진행(로컬) | ✅ |
| 커스텀 커서(오버워치) | ✅ |
| 난이도 배율 구조(StageDifficulty) | ✅ 구조 / 🟡 수치 튜닝 |
| **Stage2 맵 GamePlay 통합** | 🟡 미확인/미통합 |
| 마법사·거너 **3D 인게임 모델** | 🟡 (선택화면 Spine은 완료) |
| 공격 SFX / MainMenu·GameOver BGM | 🟡 미후킹 |
| 피격·사망·레벨업 VFX, 드롭 아이템 시각 | ❌ placeholder |
| 옵션 메뉴 UI / 튜토리얼 / 크레딧 | ❌ |
| 밸런싱(5분 라운드) 수치 | 🟡 플레이테스트 튜닝 필요 |

---

## 6. 기획서 작성 시 다음 단계 (제안)
- 본 인벤토리를 토대로 시스템별 GDD(8섹션) 갱신/신규 (랭킹·온라인·해금·Spine·2단보스는 신규 문서 필요)
- 밸런싱 표 정식화(웨이브/난이도/점수 가중치)
- 미완 항목(Stage2 통합, 인게임 모델, SFX/VFX) 로드맵 반영
