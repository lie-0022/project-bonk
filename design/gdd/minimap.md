# 미니맵 시스템 (Minimap)

> **System: 미니맵 (신규)** | Category: UI (Alpha tier) | Status: Implemented
> 관련 코드: `MinimapManager.cs`, `MinimapTracker.cs`, `MinimapObjective.cs`
> 작성: 2026-06-09 (코드 전수 기반)

---

## 1. Overview

미니맵 시스템은 플레이어가 현재 위치를 기준으로 주변 오브젝트를 **UI 마커**로 시각화하는 HUD 서브시스템이다. 카메라 렌더 없이 `UnityEngine.UI.Image` 마커만 사용해 퍼포먼스 비용이 낮다. **플레이어 중심·북향 고정** 레이아웃으로, 플레이어 마커는 항상 미니맵 중앙에 고정되고 주변 오브젝트가 상대 위치로 이동한다. 회전(월드 방향 따라 맵 회전)은 없다 — 위쪽이 항상 게임 월드의 +Z(북쪽)다.

시스템은 세 클래스로 분리된다. `MinimapTracker`가 씬 오브젝트에 부착돼 자신의 마커 타입을 선언하고, `MinimapManager` 싱글턴이 등록된 트래커 목록을 매 `LateUpdate`에 순회해 UI 좌표로 변환·갱신하며, `MinimapObjective`(정적 클래스)가 "보스 처치 후 이동해야 할 목표" Transform을 보관하면 `MinimapManager`가 이를 별도 마커+화살표로 표시한다.

핵심 구성요소 요약:

| 요소 | 역할 |
|---|---|
| `MinimapTracker` | 개별 오브젝트에 부착. `MarkerType` 선언. `OnEnable/OnDisable`로 자동 등록/해제 |
| `MinimapManager` | 싱글턴. 마커 풀 관리, `LateUpdate` 위치 갱신, 월드→UI 좌표 변환 |
| `MinimapObjective` | static 홀더. `Target`(Transform). `StageGate`/`ArenaPhaseManager`가 `Set`, 인카운터 시작 시 `Clear` |

## 2. Player Fantasy

**"내가 있는 곳, 적이 있는 곳 — 한눈에."**

플레이어는 넓은 맵에서 길을 잃거나 어디서 적이 오는지 모르는 불안을 느끼지 않아야 한다. 미니맵은 좌측 상단 또는 코너에 항상 떠 있어, 시선을 화면 중앙에 두면서도 주변 위협을 파악하게 한다.

보스를 쓰러뜨린 뒤엔 "다음에 어디 가야 하지?"라는 혼란이 생겨선 안 된다. 게이트가 열리는 순간 **초록 화살표가 목표 방향을 가리켜** 방향성을 즉시 제공한다. Stage3 2단 구조에서도 1차 보스 처치 → 상층 봉인 개방과 함께 화살표가 전환돼 두 단계 흐름을 자연스럽게 안내한다.

마커 색상은 위협(빨강), 보상(노랑/하늘), 목표(초록)의 직관적 코드를 따른다. 플레이어는 별도 학습 없이 색상만으로 정보를 읽는다.

## 3. Detailed Rules

### 3.1 마커 등록과 해제

- 씬 오브젝트에 `MinimapTracker` 컴포넌트를 부착하면 `OnEnable` 시 `MinimapManager.Register(this)` 자동 호출.
- `OnDisable` 시 `MinimapManager.Unregister(this)` 자동 호출 → 해당 마커 `Image` GameObject 즉시 파괴.
- `MinimapManager`가 아직 Awake되지 않은 상태에서 `Register`가 호출되면 `s_pending` 정적 리스트에 큐잉, Awake 시 일괄 등록.

### 3.2 마커 타입과 시각 속성

| `MarkerType` | 기본 크기(px) | 기본 색상 | 설명 |
|---|---|---|---|
| `Player` | 14 | 하늘 (#66D9FF) | 항상 미니맵 중앙 고정 |
| `Enemy` | 6 | 빨강 (#E63333) | 잡몹 |
| `Boss` | 16 | 진빨강 (#FF1A1A) | 보스 |
| `Chest` | 10 | 금 (#FFD933) | 상자 |
| `Coin` | 4 | 연노랑 (#FFF266) | 코인 |
| `Jar` | 6 | 하늘 (#4D99FF) | 항아리 |
| Objective 게이트 마커 | 13 | 초록 (#4DFF73) | 목표 게이트 위치 (상시 클램프) |
| Objective 화살표 | 9×26 | 초록 (#4DFF73) | 플레이어 중심, 목표 방향 회전 |

크기·색상은 Inspector의 `_playerSize` 등 인스펙터 필드로 전량 튜닝 가능하다.

### 3.3 위치 갱신 (LateUpdate)

- `LateUpdate`에서 `_player` 트래커가 null이면 갱신 생략(플레이어 미등록 시 방어).
- `_markerLayer`(RectTransform)의 `rect.size`를 매 프레임 읽어 `halfX`, `halfY` 계산 → 해상도 변경에 자동 대응.
- 플레이어 마커: `anchoredPosition = Vector2.zero`(중앙 고정).
- 나머지 마커: 월드 차분 `d = target.position - player.position`에서 `(d.x, d.z)`를 픽셀로 변환. Y(높이) 무시 — 미니맵은 XZ 평면 평면도.

### 3.4 범위 밖 마커 처리 (`_clampToEdge`)

- `_clampToEdge = false`(기본): 변환 후 `|ui.x| > halfX` 또는 `|ui.y| > halfY`이면 `img.enabled = false`(숨김).
- `_clampToEdge = true`: 범위 밖이어도 `halfX/halfY`로 클램프해 가장자리에 표시.
- Objective 게이트 마커는 `_clampToEdge` 설정과 무관하게 **항상 클램프** 표시(목표 방향을 절대 숨기지 않음).

### 3.5 목표 유도(MinimapObjective)

- `MinimapObjective.Target`이 `null`이면 게이트 마커와 화살표 모두 `enabled = false`.
- `Target`이 설정되면 매 `LateUpdate`에서 게이트 마커 위치와 화살표 회전을 업데이트.
- 게이트 마커: 목표 위치를 클램프해 표시.
- 화살표: `anchoredPosition = Vector2.zero`(플레이어 중심 고정), `pivot = (0.5, 0)` 바닥 피벗 — 아래 끝이 중앙에 걸리고 위가 목표 방향을 향해 뻗음. 회전 공식은 §4 참조.
- `Set`/`Clear` 호출 주체: `StageGate.Open()` → 게이트 Transform 전달. `ArenaPhaseManager` → 1차 보스 처치 시 상층 진입로 Transform 전달. 인카운터(봉인) 시작 시 `Clear`.
- 씬 리로드 시 Target이 파괴된 씬 오브젝트를 가리키므로 자동으로 null 참조가 되어 표시가 꺼진다(유니티 씬 오브젝트 파괴 시 `Transform == null` 판정).

## 4. Formulas

**변수 정의**

| 변수 | 의미 | 범위 |
|---|---|---|
| `W` | 월드 반경(`_worldViewRadius`) — 미니맵에 표시되는 월드 단위 반경 | >0, 기본 25m |
| `L` | `_markerLayer.rect.size`의 단축(`min(layerWidth, layerHeight)`) | px, >0 |
| `halfX` | `layerWidth * 0.5` | px |
| `halfY` | `layerHeight * 0.5` | px |
| `R` | 월드→픽셀 스케일 팩터 | px/m |
| `d_x`, `d_z` | 타깃 월드 X·Z − 플레이어 월드 X·Z | m, 범위 없음 |
| `u_x`, `u_y` | UI anchoredPosition (픽셀, 중앙 원점) | px |
| `θ` | 화살표 Z축 회전 | 도(°) |

**F1 — 월드→픽셀 스케일 팩터**

```
R = min(halfX, halfY) / max(W, 0.01)
```

- `max(W, 0.01)` : 0 나누기 방어.
- 예: layerSize=(120,120), W=25 → halfX=halfY=60, R = 60 / 25 = 2.4 px/m

**F2 — 월드 차분 → UI 좌표**

```
d_x = target.world.x - player.world.x
d_z = target.world.z - player.world.z

u_x = d_x × R
u_y = d_z × R
```

- 미니맵은 XZ 평면 투영. 월드 Y(높이) 무시.
- 미니맵 +Y = 월드 +Z(북쪽). 미니맵 +X = 월드 +X(동쪽).
- 예: 타깃이 플레이어 동쪽 10m, 북쪽 5m → d_x=10, d_z=5, R=2.4 → u_x=24px, u_y=12px

**F3 — 클램프 (범위 밖 마커, `_clampToEdge=true` 또는 Objective 항상)**

```
u_x_clamped = clamp(u_x, -halfX, halfX)
u_y_clamped = clamp(u_y, -halfY, halfY)
```

- 예: u_x=90px, halfX=60 → u_x_clamped=60px (오른쪽 가장자리)

**F4 — 숨김 조건 (`_clampToEdge=false`)**

```
hidden = (|u_x| > halfX) OR (|u_y| > halfY)
```

- 숨김이면 `img.enabled = false`, 범위 복귀 시 `enabled = true`.

**F5 — Objective 화살표 회전**

```
θ = atan2(u_y, u_x) × (180/π) - 90
objArrowRT.localRotation = Quaternion.Euler(0, 0, θ)
```

- `atan2(u_y, u_x)`: UI 좌표계에서 목표 방향 각도 (x축 기준).
- `-90` 보정: 스프라이트가 위(`+Y`)를 향하도록 기준 오프셋.
- `pivot = (0.5, 0)`: 화살표 바닥이 플레이어 중심(원점)에, 화살촉이 목표 방향으로 뻗음.
- 예: 타깃이 정북(u_x=0, u_y=+30) → atan2(30,0)=90°, θ=90-90=0° → 화살표 그대로 위를 향함.
- 예: 타깃이 정동(u_x=+30, u_y=0) → atan2(0,30)=0°, θ=0-90=-90° → 화살표 오른쪽.
- 예: 타깃이 남서(u_x=-20, u_y=-20) → atan2(-20,-20)=-135°, θ=-135-90=-225°(=135°) → 화살표 좌하단.
- `ui.sqrMagnitude <= 0.0001` 이면 회전 갱신 생략(플레이어가 목표 위에 있을 때 불안정 회전 방지).

## 5. Edge Cases

| 상황 | 처리 |
|---|---|
| **`MinimapManager` Awake 전 `Register` 호출** | `s_pending`에 큐잉. Awake에서 일괄 `AddInternal`. 중복 등록 방지(`_markers.ContainsKey`). |
| **같은 `MinimapTracker`를 두 번 `Register`** | `_markers.ContainsKey(t)` 체크로 조기 반환 — 마커 중복 생성 없음. |
| **`MinimapTracker`가 파괴된 채 LateUpdate 진입** | `kv.Key == null` 체크로 건너뜀. 단, Unregister가 정상 호출되면 파괴 전 딕셔너리에서 제거됨. |
| **`_markerLayer` 미할당** | `Awake`의 `CreateObjectiveUI` 조기 반환. `LateUpdate` 에서 null 체크 후 갱신 생략. 마커 전혀 표시 안 됨 — **배선 필수**. |
| **`_player`가 null(플레이어 미등록/사망)** | `LateUpdate` 전체 건너뜀 — 마커 갱신 정지. 플레이어 오브젝트가 `MinimapTracker(Player)` 없이 씬에 배치되면 항상 null. |
| **`_worldViewRadius = 0`** | `max(W, 0.01)` 방어 → R이 극대값이 되어 마커 전부 클램프/숨김. R 설정 오류 방지 차원에서 0 미만 값 비권장. |
| **`MinimapObjective.Target`이 씬 리로드로 파괴** | Unity가 파괴된 씬 오브젝트를 `null`로 반환(`UnityEngine.Object.==null` 오버라이드) → `show = false` → 마커·화살표 자동 숨김. `Clear()` 명시 호출 없어도 안전. |
| **`Target`이 플레이어와 같은 위치** | `ui.sqrMagnitude <= 0.0001f` → 화살표 회전 갱신 생략(이전 회전 유지). 게이트 마커는 중앙(0,0)에 표시. |
| **`_arrowSprite` 미할당** | `_markerSprite`(기본 사각형)를 폴백으로 사용. 화살표 방향은 회전으로 표현되므로 막대 모양으로도 동작. |
| **레이어 크기가 동적으로 변함(해상도 변경)** | `layerSize = _markerLayer.rect.size`를 매 `LateUpdate` 재계산 — 고정값 캐싱 없음. 해상도 변경에 자동 대응. |
| **`_clampToEdge=false`인데 Objective 마커가 범위 밖** | 일반 마커와 달리 Objective 마커는 `UpdateObjective` 내에서 항상 클램프 적용 — 절대 숨겨지지 않음. |
| **`s_pending`에 쌓인 채 Instance가 생성 안 됨** | `s_pending`은 static → 씬 리로드 후에도 잔류 가능. 신규 Awake 시 일괄 처리되므로 문제없으나, 파괴된 트래커가 pending에 남으면 `AddInternal` 내 null 체크로 방어. |

## 6. Dependencies

**이 시스템이 의존하는 것 (upstream)**

| 시스템 | 용도 |
|---|---|
| `MinimapTracker` (개별 MonoBehaviour) | 마커 타입 선언 + `OnEnable/OnDisable` 자동 등록/해제 소스 |
| `MinimapObjective` (static) | Objective `Target` 공급 — `MinimapManager`가 Target을 읽어 게이트 마커+화살표 업데이트 |
| `UnityEngine.UI` (uGUI) | `Image`, `RectTransform` — UI 마커 렌더링 |
| 게임 오브젝트 Transform | 모든 추적 대상의 월드 위치 소스 |

**이 시스템에 의존하는 것 (downstream)**

| 시스템 | 관계 |
|---|---|
| `StageGate` | 게이트 개방 시 `MinimapObjective.Set(gateTransform)` 호출 — 목표 유도 활성화 |
| `ArenaPhaseManager` | 1차 보스 처치 시 상층 진입로 Transform으로 `MinimapObjective.Set` 호출 → 화살표 방향 전환 |
| `Player` 프리팹 | `MinimapTracker(Player)` 부착 필수 — 미부착 시 전체 미니맵 갱신 중단 |
| 잡몹/보스 프리팹 | `MinimapTracker(Enemy/Boss)` 부착 → 스폰·해제 자동 등록/해제 |
| `Chest`/`Coin`/`Jar` | `MinimapTracker(Chest/Coin/Jar)` 부착 → 수집·파괴 시 `OnDisable`로 자동 해제 |

**양방향 문서 갱신 필요**

- `stage-progression.md` §6 downstream → 이미 "미니맵(`MinimapObjective`) — 게이트·상층 진입로 유도 마커" 기재됨 (양방향 완료).
- `stage-progression.md` §3.2/§3.5 → `MinimapObjective.Set` 호출 출처 기재됨.
- 신규 작성 예정 GDD: `boss-monster.md` — "보스 처치 시 `StageGate` / `ArenaPhaseManager` 경유 `MinimapObjective.Set` 호출" 언급 필요.

## 7. Tuning Knobs

| 노브 | 필드 위치 (MinimapManager Inspector) | 안전 범위 | 영향 |
|---|---|---|---|
| `_worldViewRadius` | `[Header("Tunables")]` | 10 ~ 60m (기본 25m) | 미니맵 가시 반경. ↑ = 더 넓은 범위 표시, 마커 밀집·작아보임. ↓ = 근거리만 표시, 원거리 정보 손실 |
| `_clampToEdge` | 동일 | true/false (기본 false) | true: 범위 밖 마커를 가장자리에 클램프. false: 범위 밖 마커 숨김. 적 방향 인식 vs 맵 클리너함 트레이드오프 |
| `_playerSize` | `[Header("Marker Style")]` | 8 ~ 20px (기본 14px) | 플레이어 마커 시인성 |
| `_enemySize` | 동일 | 4 ~ 12px (기본 6px) | 잡몹 밀집 지역 가독성. 너무 크면 마커 중첩 |
| `_bossSize` | 동일 | 10 ~ 24px (기본 16px) | 보스 강조도. Player보다 크게 유지 권장 |
| `_chestSize` | 동일 | 6 ~ 16px (기본 10px) | 상자 탐색 편의성 |
| `_coinSize` | 동일 | 2 ~ 8px (기본 4px) | 코인 밀집 시 노이즈 vs 정보. 너무 크면 미니맵 어수선 |
| `_jarSize` | 동일 | 4 ~ 10px (기본 6px) | 항아리 밀집 시 가독성 |
| `_gateSize` | `[Header("Objective")]` | 8 ~ 20px (기본 13px) | 목표 게이트 마커 시인성 |
| `_arrowSize` | 동일 | x: 6~16, y: 16~36 (기본 9×26) | 유도 화살표 굵기/길이. y가 너무 짧으면 방향 인식 어려움 |
| `_playerColor` … `_jarColor` | 동일 | 자유 (단 색맹 접근성 고려) | 마커 타입 구별력. 위협(빨강), 보상(노랑계), 목표(초록) 코드 유지 권장 |
| `_gateColor` / `_arrowColor` | 동일 | 자유 (기본 초록 #4DFF73) | 목표 가시성. Enemy 빨강과 혼동되지 않는 색 유지 |
| `_markerSprite` | 동일 | 단색 Sprite 또는 null | null이면 Default-UI 흰 사각형 사용. 원형/다이아 등 커스텀 가능 |
| `_arrowSprite` | 동일 | 위를 향한 화살표 Sprite 또는 null | null이면 `_markerSprite`(막대) 폴백. 교체 시 pivot=(0.5,0) 기준 디자인 필요 |

비-노브(고정 설계): 플레이어 중심 고정, 북향 고정(회전 없음), XZ 평면 투영(Y 무시), Objective 항상 클램프, 화살표 pivot=(0.5,0).

## 8. Acceptance Criteria

각 항목은 QA가 합/불 판정 가능해야 한다.

**마커 표시 기본**

- [ ] 게임 시작 후 미니맵 중앙에 플레이어 마커(하늘색)가 항상 고정 표시된다.
- [ ] 플레이어 이동 시 미니맵 중앙이 유지되고 주변 마커들이 상대적으로 이동한다(카메라 회전 시에도 미니맵 방향은 고정 — 회전 없음).
- [ ] 잡몹 스폰 즉시 빨간 마커가 등장하고, 잡몹 사망(OnDisable) 즉시 마커가 사라진다.
- [ ] 보스가 진빨간 마커로, 상자가 금색 마커로, 코인이 연노랑 마커로, 항아리가 하늘색 마커로 구분 표시된다.

**좌표 변환 정확성**

- [ ] 플레이어 정북 25m(월드 +Z)에 있는 오브젝트 마커가 미니맵 상단 가장자리 근처에 표시된다(`_worldViewRadius=25`, `_clampToEdge=false`).
- [ ] 플레이어 기준 25m 초과 거리의 오브젝트 마커가 `_clampToEdge=false`일 때 미니맵에서 사라진다.
- [ ] `_clampToEdge=true`로 변경 시 동일 오브젝트가 가장자리에 고정 표시된다(사라지지 않음).

**목표 유도 (MinimapObjective)**

- [ ] 보스 처치 후 `StageGate.Open()`이 호출되면 미니맵에 초록 게이트 마커와 플레이어 중심 초록 화살표가 동시에 등장한다.
- [ ] 화살표가 게이트 방향을 정확히 가리킨다: 플레이어가 게이트를 향해 이동할수록 화살표가 위쪽(정북)에 수렴한다(플레이어-게이트 정렬 시).
- [ ] 게이트 마커는 게이트가 미니맵 표시 반경 밖이어도 가장자리에 클램프 표시된다(숨겨지지 않음).
- [ ] Stage3 1차 보스 처치 시 화살표가 상층 진입로 방향으로 전환된다(게이트 → 상층 봉인 입구).
- [ ] `MinimapObjective.Clear()` 호출 후(또는 씬 리로드 후) 게이트 마커와 화살표가 모두 숨겨진다.

**엣지 케이스 검증**

- [ ] `_markerLayer`를 미할당한 채 씬 진입 시 콘솔 에러 없이 미니맵 미표시 상태로 동작한다(NullReferenceException 없음).
- [ ] Player에 `MinimapTracker(Player)` 컴포넌트가 없으면 미니맵이 갱신되지 않고 콘솔 에러가 발생하지 않는다.
- [ ] 씬 리로드 후 이전 씬의 Target이 파괴되었을 때 게이트 마커/화살표가 자동으로 숨겨진다(수동 `Clear` 불필요).
- [ ] 동일 `MinimapTracker`를 `Register` 두 번 호출해도 마커가 하나만 생성된다.

---

> **문서 완료** (8/8 섹션). 코드 전수 기반 검증. 후속: `boss-monster.md` 신규 작성 시 `MinimapObjective.Set` 호출 출처 양방향 언급 필요.
