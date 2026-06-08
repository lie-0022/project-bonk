# 랭킹·점수 시스템 (Ranking & Scoring)

> **System #25** (systems-index) | Category: Meta | Status: Implemented
> 관련 코드: `RunStats.cs`, `RunTotals.cs`, `StageGate.cs`, `GameClearUI.cs`
> 작성: 2026-06-09 (코드 전수 기반)

---

## 1. Overview

랭킹·점수 시스템은 한 번의 런(run)에서 쌓은 처치 수·획득 골드·최고 레벨·클리어 스테이지 수·총 생존 시간을 **가중합산하여 최종 점수**로 환산하고, 이를 **로컬 BEST 점수(PlayerPrefs)와 비교·갱신**하는 Meta 시스템이다.

데이터 흐름은 두 계층으로 분리된다. `RunStats`(MonoBehaviour 싱글톤)는 **한 스테이지 내** 처치 수와 생존 시간을 실시간 추적하고 스냅샷(`Snapshot` 구조체)을 만든다. `RunTotals`(정적 클래스)는 스테이지 게이트 통과 시마다 그 스냅샷을 **3개 스테이지에 걸쳐 누적**하며, 런 종료 시 `Score`를 확정한다. `GameClearUI`가 최종 점수를 화면에 표시하고, `TrySaveBest()`가 신기록 여부를 판정한다.

GameOver 시에는 점수 화면이 별도 표시되지 않는다(현재 구현 범위 외). 온라인 리더보드 연동은 `online-leaderboard.md` 소관이다.

핵심 구성요소 요약:

| 요소 | 역할 |
|---|---|
| `RunStats` | 현재 스테이지 처치 수·생존 시간 실시간 집계, `CaptureCurrent(bool)` 스냅샷 반환 |
| `RunStats.Snapshot` | 스테이지 단위 스냅샷 구조체 (KillCount / SurvivalTime / FinalGold / FinalLevel / IsCleared) |
| `RunTotals` | static 누적 레지스터 (TotalKills / TotalGold / TotalTime / HighestLevel / StagesCleared) + Score 프로퍼티 |
| `StageGate` | 게이트 통과 시 `RunTotals.AddStage(RunStats.Instance.CaptureCurrent(true))` 호출 |
| `GameClearUI` | `RunTotals.Score` 표시, `TrySaveBest()` 호출, 신기록 시 "신기록!" 텍스트 표시 |

---

## 2. Player Fantasy

**"내가 얼마나 강해졌는지 숫자로 보고 싶다."**

플레이어는 한 런을 끝내고 결과 화면에서 자신이 얼마나 많이 죽이고, 얼마나 많은 골드를 벌었으며, 얼마나 높은 레벨에 도달했는지를 **단번에 파악**할 수 있어야 한다. 점수는 단순한 집계가 아니라 "이번 런이 저번보다 나았는가"를 판가름하는 **성장 지표**다.

신기록(BEST 갱신)은 순간적인 성취감의 정점이다. 클리어 화면에 "신기록!" 텍스트가 뜨는 순간, 플레이어는 다음 런에서 그것을 넘으려는 동기를 얻는다. BEST가 로컬에 영속되므로 다음 세션에서도 "지난번 내 최고"와 경쟁할 수 있다.

온라인 리더보드는 별도 단계에서 연동되지만, 로컬 BEST만으로도 "나 자신을 이기는" 루프는 충분히 성립한다.

---

## 3. Detailed Rules

### 3.1 런 시작과 초기화

- 새 런 시작(메인 메뉴→게임 진입) 시 `RunTotals.Reset()`으로 5개 누적 필드를 전부 0으로 초기화한다.
- `RunStats`는 `GameState.Playing` 진입 시 `_running = true`로 전환되어 `SurvivalTime` 누적을 시작한다.
- `KillCount`는 `EnemyBase.OnEnemyDied` 정적 이벤트 구독으로 자동 증가한다(매개변수: xpReward, position — 실제 카운트는 호출 횟수 기준).

### 3.2 스테이지 단위 스냅샷 누적

- 플레이어가 열린 `StageGate` 트리거에 진입하면 1회만(`_passed` 플래그):
  1. `RunStats.Instance.CaptureCurrent(true)` → 현재 스테이지의 스냅샷 생성.
     - `FinalGold`: 스냅샷 시점 `GoldSystem.Instance.CurrentGold`. **GoldSystem은 `DontDestroyOnLoad`가 없어 스테이지 전환(씬 리로드)마다 0으로 리셋된다(Awake). 따라서 `FinalGold`는 "그 스테이지에서 획득·보유한 골드"이며, 합산 누적이 곧 런 전체 골드가 된다(이월 중복 없음 — 코드 검증 완료).**
     - `FinalLevel`: 스냅샷 시점 `XPSystem.Instance.CurrentLevel`. XPSystem도 씬 리로드마다 Lv1로 리셋되므로 `FinalLevel`은 그 스테이지에서 도달한 최고 레벨이다.
  2. `RunTotals.AddStage(snapshot)` → 5개 필드 누적:
     - `TotalKills += s.KillCount`
     - `TotalGold += s.FinalGold`
     - `TotalTime += s.SurvivalTime`
     - `HighestLevel = max(HighestLevel, s.FinalLevel)` (레벨은 합산이 아니라 최댓값)
     - `StagesCleared++`
- **주의**: `RunStats`의 `KillCount`/`SurvivalTime`은 씬 리로드(스테이지 전환) 시 새 인스턴스가 생성되어 0에서 재시작한다. 누적은 `RunTotals`가 담당한다.

### 3.3 최종 점수 확정 (GameClear)

- 최종 스테이지 게이트 통과(`_isFinalStage=true`) → `RunTotals.AddStage(...)` 후 `GameManager.ChangeState(Win)`.
- `GameClearUI.Awake()` → `PopulateScore()` 실행:
  1. `RunTotals.TrySaveBest()` 호출 → 현재 `Score > BestScore`면 PlayerPrefs 갱신 + `true` 반환.
  2. `_scoreText`: `RunTotals.Score` (천 단위 콤마 포맷 `"N0"`).
  3. `_breakdownText`: 스테이지 클리어 수 / 처치 수 / 획득 골드 / 최고 레벨 / 총 시간(M:SS).
  4. `_bestText`: 신기록이면 `"신기록!  BEST {BestScore:N0}"`, 아니면 `"BEST {BestScore:N0}"`.

### 3.4 BEST 점수 영속

- PlayerPrefs 키: `"BladeSurge.BestScore"` (정수, `RunTotals.BestScoreKey` 상수).
- `TrySaveBest()`는 `PlayerPrefs.Save()`까지 호출하여 즉시 디스크에 기록한다.
- BEST는 단조 증가(낮은 점수로 덮어쓰지 않음). 초기화는 PlayerPrefs 직접 삭제 또는 향후 디버그 메뉴 경유.
- BEST 키 없음(최초 실행): `GetInt("BladeSurge.BestScore", 0)` → 0.

### 3.5 GameOver 시 처리

- `GameState.GameOver` 전환 시 `RunStats.FreezeSnapshot(false)` → `LastRun` 스냅샷 동결.
- 현재 GameOver 화면(`GameOverUI`)에는 점수 표시 UI 없음. `RunTotals.Score`는 계산 가능하나 표시 미구현.
- GameOver 시 `RunTotals.TrySaveBest()` 미호출 → BEST 갱신 없음(클리어 조건만 갱신).

---

## 4. Formulas

**변수 정의**

| 변수 | 필드 / 출처 | 의미 | 범위 |
|---|---|---|---|
| `G` | `RunTotals.TotalGold` | 3스테이지 누적 골드 합산 | ≥ 0 (정수) |
| `K` | `RunTotals.TotalKills` | 3스테이지 누적 처치 수 합산 | ≥ 0 (정수) |
| `L` | `RunTotals.HighestLevel` | 3스테이지 중 최고 레벨(max, 합산 아님) | ≥ 0 (통상 1 ~ 50) |
| `S` | `RunTotals.StagesCleared` | 클리어한 스테이지 수 | 0 ~ 3 (정수) |
| `T` | `RunTotals.TotalTime` | 3스테이지 누적 생존 시간(초, float) | ≥ 0 |
| `w_G` | `RunTotals.GoldWeight = 1` | 골드 가중치 | 고정 상수 |
| `w_K` | `RunTotals.KillWeight = 10` | 처치 가중치 | 고정 상수 |
| `w_L` | `RunTotals.LevelWeight = 100` | 레벨 가중치 | 고정 상수 |
| `w_S` | `RunTotals.StageWeight = 1000` | 스테이지 가중치 | 고정 상수 |
| `w_T` | `RunTotals.TimeWeight = 1` | 시간 가중치 | 고정 상수 |

**F1 — 최종 점수**
```
Score = G × w_G + K × w_K + L × w_L + S × w_S + Round(T) × w_T
      = G×1 + K×10 + L×100 + S×1000 + Round(T)×1
```
- `Round(T)` = `Mathf.RoundToInt(TotalTime)` (0.5 이상 올림).
- 예시 계산 (3스테이지 풀클리어, 평균 런):
  - G=800, K=150, L=12, S=3, T=420초(7분)
  - Score = 800×1 + 150×10 + 12×100 + 3×1000 + 420×1
  - = 800 + 1500 + 1200 + 3000 + 420 = **6920**

**F2 — 스테이지 누적 (AddStage)**
```
TotalKills    += s.KillCount
TotalGold     += s.FinalGold
TotalTime     += s.SurvivalTime
HighestLevel   = max(HighestLevel, s.FinalLevel)
StagesCleared += 1
```
- `s` = `RunStats.Snapshot` (CaptureCurrent 반환값).
- 3개 스테이지 게이트 통과마다 1회씩 호출되므로 최대 3회 누적.

**F3 — BEST 갱신 판정**
```
isNewBest = (Score > BestScore)
if isNewBest:
    PlayerPrefs.SetInt("BladeSurge.BestScore", Score)
    PlayerPrefs.Save()
```

**F4 — 총 시간 문자열**
```
t = Floor(TotalTime)
FormattedTotalTime = "{t/60:00}:{t%60:00}"  (예: 420초 → "07:00")
```

**가중치 기여도 비교** (예시 런 기준):

| 항목 | 산식 | 기여 점수 | 비율 |
|---|---|---|---|
| 스테이지 클리어 | 3 × 1000 | 3000 | ~43 % |
| 처치 수 | 150 × 10 | 1500 | ~22 % |
| 레벨 | 12 × 100 | 1200 | ~17 % |
| 시간 | 420 × 1 | 420 | ~6 % |
| 골드 | 800 × 1 | 800 | ~12 % |

- 스테이지 클리어(w_S=1000)가 단일 최대 기여. 클리어를 못 하면 최대 3000점이 증발한다.
- 처치·레벨이 점수 경쟁의 주요 변수. 빠른 클리어와 높은 처치 수 중 어느 쪽이 유리한지는 `w_T` 방향 재검토 시 변동 가능(`TimeWeight` 주석 참조).

---

## 5. Edge Cases

| 상황 | 처리 |
|---|---|
| **GameOver 시 BEST 갱신** | `TrySaveBest()` 미호출. BEST는 풀클리어(Win) 조건에서만 갱신된다. |
| **1~2스테이지만 클리어 후 GameOver** | `StagesCleared`가 1 또는 2로 고정. Win이 아니면 `TrySaveBest()` 미호출 → BEST 미갱신. |
| **`RunTotals.Reset()` 미호출 재시작** | 이전 런 통계가 누적에 더해져 점수가 부풀어 오른다. 반드시 새 런 진입 시 호출 필요(현재 책임 소재 확인 필수). |
| **`RunStats.Instance == null` (게이트 통과 시)** | `StageGate.OnTriggerEnter`에서 null 체크 후 `AddStage` 생략. 통계 누락 + 점수 0 처리. |
| **`GoldSystem.Instance == null` (스냅샷 시)** | `CaptureCurrent`에서 `FinalGold = 0` 처리. 골드 기여분 손실. |
| **`XPSystem.Instance == null` (스냅샷 시)** | `CaptureCurrent`에서 `FinalLevel = 1` 처리. 레벨 기여분은 100점 고정. |
| **같은 점수로 BEST 동점** | `Score <= BestScore` 조건이므로 동점은 갱신하지 않는다(엄격한 초과 조건). |
| **PlayerPrefs 키 없음 (최초 실행)** | `GetInt("BladeSurge.BestScore", 0)` → BEST=0. 첫 클리어 시 무조건 신기록. |
| **`_scoreText`/`_breakdownText`/`_bestText` 미할당** | `GameClearUI.PopulateScore`에서 null 체크 후 해당 항목만 건너뜀. 나머지 UI 정상 표시. |
| **TotalTime = 0 (런 시간 미집계)** | `Round(0) × 1 = 0`. 시간 기여분만 0점. 다른 항목은 정상 계산. |
| **StagesCleared = 0 (게이트 통과 없이 Win 전환)** | Stage 점수 0. 정상 게임플레이에서는 불가능하나 디버그 강제 Win 시 발생 가능. |
| **골드 합산의 정확성** | GoldSystem은 씬 리로드마다 0으로 리셋되므로(DontDestroyOnLoad 없음), 각 스테이지 `FinalGold`는 그 스테이지 획득분이다. `TotalGold += s.FinalGold` 합산은 런 전체 골드와 일치하며 이월 중복이 없다(2026-06-09 코드 검증 완료). 단, 골드를 상자 구매로 소비하면 스냅샷이 줄어 점수도 줄어든다(소비=점수 트레이드오프, 의도된 동작). |

---

## 6. Dependencies

**이 시스템이 의존하는 것 (upstream)**

| 시스템 | 용도 |
|---|---|
| 골드(`GoldSystem`) | 스냅샷 시 `CurrentGold` 읽기 (FinalGold) |
| 경험치·레벨(`XPSystem`) | 스냅샷 시 `CurrentLevel` 읽기 (FinalLevel) |
| 적 사망 이벤트(`EnemyBase.OnEnemyDied`) | `RunStats.KillCount` 증가 트리거 |
| 스테이지 진행(`StageGate`) | 게이트 통과 시 `RunTotals.AddStage` 호출 |
| 게임 상태(`GameManager.OnGameStateChanged`) | Playing/Paused/Win/GameOver 전환 시 타이머 제어·스냅샷 동결 |
| 씬 시스템(`GameClear` 씬) | Win 전환 후 `GameClearUI`가 로드되어 점수 표시 |

**이 시스템에 의존하는 것 (downstream)**

| 시스템 | 관계 |
|---|---|
| `GameClearUI` | `RunTotals.Score`, `BestScore`, `TrySaveBest()` 읽기·호출 |
| `GameOverUI` | `RunStats.LastRun` 스냅샷 읽기 (현재 점수 표시 미구현) |
| 온라인 리더보드(`LeaderboardPanelUI` / `online-leaderboard.md`) | `RunTotals.Score` 및 `BestScore`를 업로드 소스로 참조 예정 |

**양방향 문서 갱신 필요 (design-docs 규칙)**
- `stage-progression.md` → 6절 Dependencies "랭킹·점수(RunStats/RunTotals)" 항목에 본 문서 상호 참조 이미 기재됨 (확인 완료)
- 신규 작성 예정: `gold-system.md` → "랭킹 스냅샷 소스" 의존 언급 추가 필요
- 신규 작성 예정: `xp-leveling.md` → "랭킹 스냅샷 소스" 의존 언급 추가 필요
- `online-leaderboard.md` 작성 시 → `RunTotals.Score`/`BestScore` 참조 관계 명기 필요

---

## 7. Tuning Knobs

| 노브 | 위치 | 현재 값 | 안전 범위 | 영향 |
|---|---|---|---|---|
| `GoldWeight` | `RunTotals.GoldWeight` (const) | 1 | 1 ~ 5 | 골드 파밍 빌드의 점수 기여. ↑ = 탐험(항아리 파밍) 전략 우대 |
| `KillWeight` | `RunTotals.KillWeight` (const) | 10 | 5 ~ 50 | 공격적 빌드의 점수 기여. ↑ = 처치 수가 점수 경쟁 주요 변수화 |
| `LevelWeight` | `RunTotals.LevelWeight` (const) | 100 | 50 ~ 500 | 레벨업(경험치 집중) 전략 우대. ↑ = 고레벨 도달 플레이어 대폭 유리 |
| `StageWeight` | `RunTotals.StageWeight` (const) | 1000 | 500 ~ 5000 | 클리어 여부가 점수에 미치는 비중. 너무 높으면 부분 클리어 런 무의미화 |
| `TimeWeight` | `RunTotals.TimeWeight` (const) | 1 | -5 ~ 10 | 생존 시간의 기여 방향. 양수=오래 살수록 유리, 음수=빠른 클리어 보너스 (코드 주석: "방향 재검토 가능"). **음수 전환 시 Score 프로퍼티 재검토 필수** |

비-노브(설계 고정):
- `HighestLevel` 집계 방식: 합산이 아닌 max(스테이지별 리셋 구조 반영).
- BEST 갱신 조건: 엄격한 초과(`>`). 동점은 갱신 안 함.
- BEST 저장: 클리어(Win) 조건에서만 갱신. GameOver는 제외.

---

## 8. Acceptance Criteria

각 항목은 QA가 합/불 판정 가능해야 한다.

**점수 계산**
- [ ] 3스테이지 풀클리어 후 GameClear 화면에서 `Score = TotalGold×1 + TotalKills×10 + HighestLevel×100 + StagesCleared×1000 + Round(TotalTime)×1` 공식이 정확히 계산되어 표시된다.
- [ ] `_breakdownText`에 스테이지 클리어 수 / 처치 수 / 획득 골드 / 최고 레벨 / 총 시간(MM:SS) 5개 항목이 모두 표시된다.
- [ ] 처치 수가 0인 상태로 클리어해도 Score가 음수가 되지 않는다.
- [ ] `TotalTime`이 소수점을 포함할 때 `Mathf.RoundToInt`로 반올림된 정수가 TimeWeight에 곱해진다.

**누적 집계 정확성**
- [ ] Stage1→2→3 순서로 전부 클리어 시 `StagesCleared == 3`이 된다.
- [ ] Stage2 게이트 통과 후 `TotalKills`가 Stage1 처치 수 + Stage2 처치 수의 합과 일치한다.
- [ ] 3개 스테이지 중 가장 높은 레벨에 도달한 스테이지 값이 `HighestLevel`에 반영된다(중간 스테이지에서 레벨이 낮아져도 최댓값 유지).

**BEST 점수 갱신**
- [ ] 처음 풀클리어 시 `_bestText`에 "신기록!" 텍스트가 포함된 문자열이 표시된다.
- [ ] 두 번째 런에서 이전 점수보다 낮게 클리어 시 "신기록!" 텍스트가 표시되지 않고 기존 BEST 점수가 유지된다.
- [ ] 동일 점수로 재클리어 시 BEST 점수가 갱신되지 않는다(동점은 신기록 아님).
- [ ] 앱을 완전히 종료 후 재실행해도 BEST 점수가 유지된다(PlayerPrefs `"BladeSurge.BestScore"` 영속).
- [ ] GameOver로 런이 종료되면 BEST 점수가 갱신되지 않는다.

**UI 연결**
- [ ] `_scoreText` 미할당 상태에서도 NullReferenceException 없이 `_breakdownText`와 `_bestText`가 정상 표시된다.
- [ ] `RunStats.Instance == null`인 환경(씬 설정 오류)에서 `StageGate` 통과 시 크래시 없이 통과 처리된다(통계 누락 경고 허용).

**새 런 초기화**
- [ ] 메인 메뉴에서 게임을 재시작하면 `RunTotals.Reset()` 호출로 이전 런 통계가 전부 0으로 초기화된다.
- [ ] 초기화 후 진행하는 런에서 이전 런의 TotalKills / TotalGold / HighestLevel이 점수에 포함되지 않는다.

---

> **문서 완료** (8/8 섹션). 코드 전수 기반 검증. 후속: gold-system.md / xp-leveling.md 작성 시 본 문서 양방향 참조 추가, GameOverUI 점수 표시 구현 시 Section 3.5 및 AC 갱신.
