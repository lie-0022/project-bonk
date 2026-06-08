# 캐릭터 선택 시스템 (Character Select)

> **System #21** (systems-index) | Category: UI | Status: Implemented
> 관련 코드: `CharacterSelectController.cs`, `CharacterSelectPreview.cs`, `MapSelectController.cs`, `WeaponInfoPanel.cs`, `StartGameButton.cs`, `GameSession.cs`
> 작성: 2026-06-09 (코드 전수 기반)

---

## 1. Overview

캐릭터 선택 시스템은 **MainMenu 씬 → GamePlay 씬 진입 전** 플레이어가 직업(전사/마법사/거너)과 맵(Stage1~3)을 고르는 UI 전용 시스템이다. `CharacterSelect.unity` 전용 씬으로 구성되며, 세 개의 독립 컨트롤러가 C# `event`로 느슨하게 연결된다.

직업 선택(`CharacterSelectController`)은 좌측 패널 3행 버튼으로 이루어지며, 선택 변경 시 `OnCharacterSelected(CharacterType)` 이벤트를 발행한다. 이 이벤트를 두 컴포넌트가 구독한다. `CharacterSelectPreview`는 중앙 영역의 직업별 `SkeletonGraphic`(spine-unity) 중 해당 직업만 활성화하고 나머지를 숨긴다. `WeaponInfoPanel`은 우측 패널의 무기 설명 이미지를 선택 직업에 맞는 스프라이트로 교체한다.

맵 선택(`MapSelectController`)은 하단 3개 썸네일 버튼으로 이루어지며, `StageProgress.IsUnlocked`로 잠금/해금 상태를 판정한다. 잠긴 맵은 `LockedSprite` + 클릭 불가 상태로 표시되고, 해금된 맵만 선택 가능하다. 기본 선택은 `HighestUnlocked`(가장 최근 해금 스테이지)로 자동 설정된다.

확인(`StartGameButton`) 클릭 시 `GameSession.SelectedCharacter`와 `GameSession.SelectedMapIndex`에 값을 기록하고 `RunTotals.Reset()` 후 `GamePlay` 씬을 로드한다.

핵심 구성요소 요약:

| 요소 | 역할 |
|---|---|
| `CharacterSelectController` | 좌측 직업 3행. 선택/하이라이트. `OnCharacterSelected` 발행 |
| `CharacterSelectPreview` | 중앙 Spine SkeletonGraphic 전환. 이벤트 구독 |
| `WeaponInfoPanel` | 우측 무기 설명 패널 스프라이트 스왑. 이벤트 구독 |
| `MapSelectController` | 하단 맵 썸네일 3개. `StageProgress` 연동. `OnMapSelected` 발행 |
| `StartGameButton` | 확인 버튼. `GameSession` 기록 → `GamePlay` 씬 로드 |
| `GameSession` (static) | 씬 간 선택 상태 보관. `SelectedCharacter`, `SelectedMapIndex`, `SelectedWeapon`, `SelectedRace` |

---

## 2. Player Fantasy

**"내 직업을 골랐다"는 정체성 각인.**

플레이어가 직업을 클릭하는 순간 중앙 화면이 그 캐릭터의 살아 있는 2D Spine 애니메이션으로 즉시 전환된다. 단순 아이콘 교체가 아니라 **캐릭터가 내 선택을 기다리며 서 있는 느낌**이어야 한다. 전사·마법사·거너 각각의 포즈와 분위기가 명확히 달라 "나는 이 스타일이다"는 결정 순간의 쾌감을 준다.

맵 선택은 **내가 어디서 싸울지를 결정하는 권한**이다. 잠긴 스테이지가 흐릿하게 보이는 것은 위협감이 아니라 "저걸 열어야겠다"는 동기 부여다. 이미 클리어한 맵을 다시 고르는 것도 의미 있는 선택이어야 한다(어려운 스테이지에 다시 도전하거나, 쉬운 스테이지로 빌드를 시험하거나).

확인 버튼을 누르는 순간은 **출격 직전의 결의감**이다. 선택한 직업의 캐릭터가 화면 중앙에 서 있는 채로 게임으로 진입하는 흐름이 자연스러워야 한다.

---

## 3. Detailed Rules

### 3.1 직업 선택 (CharacterSelectController)

- 직업은 3종으로 고정: `CharacterType.Warrior`(전사), `CharacterType.Mage`(마법사), `CharacterType.Gunner`(거너).
- `_rows[3]` 배열에 각 행이 직렬화된다. 각 행(`CharacterRow`)은 `Type`, `Button`, `Icon(Image)`, `NameLabel(TMP)`, `IconDefault`, `IconSelected`를 가진다.
- 클릭 → `Select(CharacterType)` 호출. 모든 행을 순회해 선택 행은 `IconSelected` 스프라이트 + `_selectedColor`, 비선택 행은 `IconDefault` + `_unselectedColor` 적용.
- 최초 `Start()`에서 `_defaultSelection`(기본값: `Warrior`)으로 `Select()` 호출 → 화면 진입 시 항상 1개가 선택된 상태.
- `Selected` 프로퍼티로 현재 선택을 외부에서 읽을 수 있다.
- 선택 변경 시마다 `OnCharacterSelected?.Invoke(type)` 발행. 구독자: `CharacterSelectPreview`, `WeaponInfoPanel`.

### 3.2 Spine 프리뷰 (CharacterSelectPreview)

- `_entries[]{CharacterType, SkeletonGraphic}` 배열에 직업별 `SkeletonGraphic` 참조가 직렬화된다.
- `OnEnable`에서 `_controller.OnCharacterSelected += Show` 구독, `OnDisable`에서 해제(메모리 누수 방지).
- `Start()`에서 `_controller.Selected`로 초기 상태 동기화.
- `Show(CharacterType type)`: 배열 전체 순회 → `e.Type == type`이면 `SetActive(true)`, 아니면 `SetActive(false)`.
- `Spine` 필드가 null인 항목은 건너뜀(자산 미배선 직업은 조용히 무시).
- 직업별 `SkeletonGraphic`은 멀티페이지 atlas를 사용한다. Spine 에디터 원본 메시 크기가 ~4400 유닛 규모이므로 표준 픽셀 해상도(캔버스 기준 ~600px)로 보이려면 소수점 스케일이 필요하다. 씬 실측값:
  - `SkeletonGraphic (warrior)`: `m_LocalScale = (0.13, 0.13, 1.0)`
  - `SkeletonGraphic (magician)`: `m_LocalScale = (0.14162368, 0.14162368, 1.0)`
  - `SkeletonGraphic (거너)`: `m_LocalScale = (0.18274426, 0.18274426, 1.0)`
- 각 `SkeletonGraphic`은 `spine-unity`의 `InstantiateSkeletonGraphic` 에디터 메서드로 생성했으며, 이 과정에서 멀티텍스처/멀티페이지 atlas에 필요한 머티리얼이 자동으로 최대 4개 할당된다(단일 atlas 페이지당 1개 머티리얼). `skeletonDataAsset` 참조와 `additiveMaterial` / `multiplyMaterial` 블렌딩 머티리얼도 이때 함께 배선된다.

### 3.3 무기 설명 패널 (WeaponInfoPanel)

- `_infos[3]{Character, PanelSprite}` 배열에 직업별 설명 이미지 스프라이트가 직렬화된다.
- `OnCharacterSelected` 구독 → `UpdatePanel(CharacterType)` → 매칭되는 `PanelSprite`로 `_panelImage.sprite` 교체.
- 패널 스프라이트는 무기 아이콘·이름·설명이 이미 그려진 통 이미지다(런타임 텍스트 없음).

### 3.4 맵 선택 (MapSelectController)

- `_maps[3]` 배열. 각 `MapEntry`는 `Index(0~2)`, `Button`, `Thumbnail(Image)`, `DefaultSprite`, `SelectedSprite`, `LockedSprite`, `BackgroundSprite`를 가진다.
- `Start()`에서 각 맵 항목에 대해 `StageProgress.IsUnlocked(map.Index)` 판정:
  - **잠긴 맵**: `Thumbnail.sprite = LockedSprite`, `Button.interactable = false`. 클릭 리스너 미등록.
  - **해금된 맵**: 클릭 리스너 등록.
- `Select(int index)`: `StageProgress.IsUnlocked` 재확인 후 진행. 해금 맵 썸네일을 `SelectedSprite`/`DefaultSprite`로 갱신. `_screenBackground.sprite = chosen.BackgroundSprite`로 전체 화면 배경 교체. `OnMapSelected?.Invoke(index)` 발행.
- 기본 선택 로직(`Start()` 끝):
  1. `def = Clamp(StageProgress.HighestUnlocked, 0, _maps.Length - 1)`
  2. `_defaultMapIndex`가 해금되어 있고 `def`보다 크면 `def = _defaultMapIndex`
  3. `Select(def)` 호출 → 항상 해금된 가장 높은(또는 인스펙터 지정) 맵이 기본 선택.
- `Selected` 프로퍼티 초기값 `-1` → `Start()` 이전에 `StartGameButton`이 읽으면 0이 아닌 -1임에 유의.

### 3.5 확인 → GamePlay 진입 (StartGameButton)

- `StartGame()` 호출 시:
  1. `GameSession.SelectedCharacter = _characterSelect.Selected`
  2. `GameSession.SelectedMapIndex = _mapSelect.Selected`
  3. `RunTotals.Reset()` — 누적 랭킹 통계 초기화(새 런 시작)
  4. `SceneManager.LoadScene(_gamePlaySceneName)` — 기본값 `"GamePlay"`

### 3.6 GameSession — 씬 간 전달

- `GameSession`은 `static class`(MonoBehaviour 아님). 씬 전환에도 값이 유지된다.
- `SelectedCharacter` 기본값: `CharacterType.Warrior`. `SelectedMapIndex` 기본값: `0`.
- 파생 프로퍼티(computed):
  - `SelectedWeapon`: `Warrior → WeaponType.Sword`, `Mage → WeaponType.Magic`, `Gunner → WeaponType.Gun`
  - `SelectedRace`: `0 → EnemyRace.Slime`, `1 → EnemyRace.Goblin`, `2 → EnemyRace.Skeleton`
- `GamePlay` 씬을 Character Select 없이 단독 실행 시 기본값(전사/맵0)으로 진입 가능(에디터 개발 편의 지원).

---

## 4. Formulas

**변수 정의**

| 변수 | 의미 | 범위 |
|---|---|---|
| `ct` | `CharacterType` enum 값 | `{Warrior=0, Mage=1, Gunner=2}` |
| `mi` | 맵 인덱스 | 정수 0 ~ 2 |
| `U` | `StageProgress.HighestUnlocked` | 정수 0 ~ 2 |
| `S_orig` | Spine 에디터 원본 메시 크기 (유닛) | ~4400 유닛 (전사 기준 실측) |
| `S_target` | Canvas 기준 목표 표시 크기 (px) | 약 570~800 px (직업별 다름) |
| `PPU` | Canvas 픽셀-퍼-유닛 환산 | `S_target / S_orig` |

**F1 — 직업 → 시작 무기 매핑**

```
SelectedWeapon(ct) =
  ct == Warrior  → WeaponType.Sword
  ct == Mage     → WeaponType.Magic
  ct == Gunner   → WeaponType.Gun
  _              → WeaponType.Sword  (폴백)
```
- 1:1 대응. 무기 종류 추가 시 `CharacterType` enum과 switch 식 동시 수정 필요.

**F2 — 맵 인덱스 → 적 종족 매핑**

```
SelectedRace(mi) =
  mi == 0  → EnemyRace.Slime
  mi == 1  → EnemyRace.Goblin
  mi == 2  → EnemyRace.Skeleton
  _        → EnemyRace.Slime  (폴백)
```

**F3 — 맵 해금 판정**

```
IsUnlocked(mi) = (mi ≤ U)
```
- 예: U=1 → mi=0,1 해금, mi=2 잠김. (F2의 상세 규칙은 `stage-progression.md` F2 참조)

**F4 — 기본 선택 맵 결정**

```
def = Clamp(U, 0, N-1)                          // N = 맵 수 = 3
if IsUnlocked(_defaultMapIndex) AND _defaultMapIndex ≥ def:
    def = _defaultMapIndex
Select(def)
```
- 예: U=2(전체 해금), `_defaultMapIndex=0` → `def = Clamp(2,0,2) = 2`, `IsUnlocked(0)=true`이지만 `0 < 2`이므로 조건 불충족 → def=2(최고 해금 맵이 기본). 인스펙터에서 `_defaultMapIndex=2`로 고정하면 항상 최고 맵이 기본.
- 예: U=0(Stage1만 해금), `_defaultMapIndex=0` → def=0 → Select(0).

**F5 — Spine 스케일 산출 (기준 공식)**

```
scale = S_target / S_orig
```
- 전사(warrior): `S_orig ≈ 4403유닛`, `S_target ≈ 572px`, `scale = 572 / 4403 ≈ 0.13` (씬 실측: 0.13)
- 마법사(magician): `S_orig` 대비 `scale = 0.14162368` (씬 실측)
- 거너: `scale = 0.18274426` (씬 실측 — 원본 메시가 상대적으로 작아 스케일이 큼)

`S_orig`는 Spine 에디터 원본 Setup Pose의 캐릭터 높이 유닛이다. `InstantiateSkeletonGraphic` 에디터 메서드 실행 시 메시 바운드 단위로 측정해 원하는 화면 픽셀 높이를 나눠 scale을 산출한다.

멀티페이지 atlas 사용으로 `SkeletonGraphic`은 atlas 페이지 수만큼 머티리얼을 필요로 한다(현재 최대 4개). `InstantiateSkeletonGraphic`이 이 머티리얼 배열을 자동 생성·배선하므로, 에디터 외부에서 수동으로 SkeletonGraphic 컴포넌트를 추가하면 머티리얼 배선이 누락돼 렌더링이 깨진다. **반드시 에디터 메뉴에서 `InstantiateSkeletonGraphic`을 사용할 것.**

---

## 5. Edge Cases

| 상황 | 처리 |
|---|---|
| **`_entries`에 해당 `CharacterType`이 없음** | `Show()`가 null 체크 후 건너뜀. 아무 SkeletonGraphic도 활성화되지 않음(빈 중앙 패널). 자산 미완성 직업의 정상 상태 |
| **`CharacterRow.Button`이 null** | `Start()`가 null 체크 후 해당 행 리스너 등록 생략. 클릭 불가 상태로 진행 |
| **`MapSelectController.Selected`가 -1인 채 `StartGame` 호출** | `GameSession.SelectedMapIndex = -1` 기록. `GamePlay`의 `DebugStageSelector`가 -1을 `switch`에서 처리하지 못하면 Slime(폴백) 또는 맵 비활성. **`StartGameButton`은 `Select(0)` 이전에 눌릴 수 없으므로 정상 흐름에서 불발.** 단, 에디터에서 씬을 직접 열고 플레이하면 가능 → GamePlay 단독 실행은 기본값 0으로 보호됨 |
| **`StageProgress.IsUnlocked`가 잠김인 맵을 `Select(index)`로 직접 호출** | `FindMap(index).IsUnlocked` 재확인 후 `return` — 상태 변경 없음 |
| **_defaultMapIndex가 잠긴 맵을 가리킴** | F4 로직에서 `IsUnlocked` 조건 실패 → `def = Clamp(U, ...)` 값으로 폴백, 가장 높은 해금 맵 선택 |
| **해금된 맵이 0개 (U < 0 — 이론상 불가)** | `StageProgress.GetInt(Key, 0)`이 최소 0을 반환하므로 항상 mi=0 해금. 발생하지 않음 |
| **SkeletonGraphic에 skeletonDataAsset 미배선** | spine-unity가 경고 + 렌더링 실패. `Show()`는 `SetActive` 호출하지만 표시 안 됨. 배선 필수 |
| **멀티페이지 atlas 머티리얼이 1개만 배선** | 첫 페이지 텍스처만 렌더, 나머지 atlas 페이지의 부위가 누락됨. `InstantiateSkeletonGraphic` 재실행으로 복구 |
| **`WeaponInfoPanel._infos`에 해당 CharacterType 없음** | `UpdatePanel()`이 foreach 종료 후 `return`. 기존 패널 이미지 유지(스왑 안 됨) |
| **`MapEntry.BackgroundSprite`가 null** | `_screenBackground.sprite` 교체 생략. 이전 배경 유지 |
| **GamePlay 씬 이름이 Build Settings에 없음** | `SceneManager.LoadScene` 에러. `StartGameButton._gamePlaySceneName` 필드와 Build Settings를 일치시킬 것 |

---

## 6. Dependencies

**이 시스템이 의존하는 것 (upstream)**

| 시스템 | 용도 |
|---|---|
| **스테이지 진행 (`StageProgress`)** | `IsUnlocked(index)`, `HighestUnlocked`로 맵 잠금/해금 판정. 단방향 읽기 |
| **무기 시스템 (`WeaponType` enum)** | `GameSession.SelectedWeapon` 파생 프로퍼티가 참조. `CharacterType`과 1:1 매핑 |
| **적 종족 시스템 (`EnemyRace` enum)** | `GameSession.SelectedRace` 파생 프로퍼티가 참조. 맵 인덱스와 1:1 매핑 |
| **랭킹·점수 (`RunTotals`)** | 확인 버튼 클릭 시 `RunTotals.Reset()`. 새 런 시작 초기화 |
| **spine-unity (`SkeletonGraphic`)** | 직업별 2D Spine 프리뷰 렌더링. `com.esotericsoftware.spine-unity` 패키지 필수 |

**이 시스템에 의존하는 것 (downstream)**

| 시스템 | 관계 |
|---|---|
| **스테이지 진행 (`DebugStageSelector`)** | `GameSession.SelectedRace`(=선택 맵)로 어떤 맵 루트를 활성화할지 결정 |
| **무기 시스템 (`WeaponSystem`)** | `GameSession.SelectedWeapon`으로 초기 장착 무기 결정 |
| **게임 매니저 (`GameManager`)** | GamePlay 씬 진입 전 `GameSession`이 기록된 상태를 전제함 |

**양방향 문서 갱신 필요 (design-docs 규칙)**

- `stage-progression.md` Section 6 Downstream 에 이미 `캐릭터 선택 → SelectedRace` 언급 있음 (기존 참조 완비).
- `weapon-system.md` (미작성) 작성 시: "GameSession.SelectedWeapon을 초기 장착 무기 결정에 사용" 언급 추가 필요.
- `game-state-manager.md` (미작성) 작성 시: "CharacterSelect → GamePlay 씬 전환의 게이트가 StartGameButton" 언급 추가 권장.

---

## 7. Tuning Knobs

| 노브 | 위치 | 안전 범위 | 영향 |
|---|---|---|---|
| `_defaultSelection` | `CharacterSelectController` 인스펙터 | `{Warrior, Mage, Gunner}` | 화면 진입 시 기본 선택 직업. 플레이어가 항상 변경 가능하므로 UX 기본값만 영향 |
| `_selectedColor` / `_unselectedColor` | `CharacterSelectController` 인스펙터 | 충분한 명도 대비 확보 (명도차 ≥ 0.25 권장) | 선택/비선택 행 이름 색상 가독성 |
| `_defaultMapIndex` | `MapSelectController` 인스펙터 | 0 ~ 2 (해금된 인덱스 내) | 화면 진입 시 기본 맵. 잠긴 인덱스를 지정하면 F4 폴백 로직으로 최고 해금 맵으로 대체 |
| Spine `m_LocalScale` (직업별) | `CharacterSelect.unity` Hierarchy (`SkeletonGraphic` Transform) | 원본 메시 크기 대비 타깃 표시 픽셀 산출 (F5). 전사 기준 0.10~0.16 | Spine 캐릭터 표시 크기. 너무 작으면 디테일 소실, 너무 크면 패널 밖으로 넘침 |
| `_gamePlaySceneName` | `StartGameButton` 인스펙터 | Build Settings의 실제 씬 이름과 일치 | 진입 씬 대상. 오탈자 시 SceneManager 에러 |
| `GameSession.SelectedCharacter` 기본값 | `GameSession.cs` 코드 | `CharacterType.Warrior` | GamePlay 단독 실행 시 기본 직업. 테스트 편의용 |
| `GameSession.SelectedMapIndex` 기본값 | `GameSession.cs` 코드 | `0` | GamePlay 단독 실행 시 기본 맵(Stage1=Slime) |

비-노브(고정 설계): 직업 수 3, 맵 수 3, 직업↔무기 1:1 매핑(F1), 맵↔종족 1:1 매핑(F2), 해금 단조 증가(stage-progression.md F1).

---

## 8. Acceptance Criteria

각 항목은 QA가 합/불 판정 가능해야 한다.

**직업 선택**

- [ ] 씬 진입 시 전사 행이 선택 상태(IconSelected 스프라이트 + SelectedColor)로 표시된다.
- [ ] 마법사 행 클릭 시 마법사가 선택 상태로 전환되고 전사/거너는 비선택 상태가 된다.
- [ ] 직업을 전환할 때마다 중앙 Spine 프리뷰가 해당 직업으로 즉시 교체된다(이전 직업 숨김 확인).
- [ ] 직업을 전환할 때마다 우측 무기 설명 패널 이미지가 해당 직업의 스프라이트로 즉시 교체된다.
- [ ] 동일 직업을 두 번 클릭해도 상태 이상 없이 정상 표시된다.

**Spine 프리뷰**

- [ ] 전사/마법사/거너 세 직업 모두 Spine 애니메이션이 정상 재생된다(잘린 텍스처·검은 패치 없음).
- [ ] 씬 최초 진입 시 전사 SkeletonGraphic만 활성화되고 나머지 2개는 비활성화되어 있다.
- [ ] Spine 캐릭터가 패널 경계 안에 온전히 표시된다(잘림 없음).

**맵 선택**

- [ ] 진행도 초기화 상태(U=0)에서 Stage1 썸네일만 선택 가능하고, Stage2/3은 LockedSprite + 클릭 불가로 표시된다.
- [ ] U=1(Stage2 해금)인 상태에서 씬 진입 시 Stage2가 기본 선택 상태로 표시된다.
- [ ] 해금된 맵 클릭 시 전체 화면 배경이 해당 맵의 BackgroundSprite로 교체된다.
- [ ] 잠긴 맵 버튼 영역을 클릭해도 선택 상태가 변경되지 않는다.

**확인 → 진입**

- [ ] 확인 버튼 클릭 시 `GameSession.SelectedCharacter`가 선택한 직업과 일치한다(콘솔 로그 또는 디버거 확인).
- [ ] 확인 버튼 클릭 시 `GameSession.SelectedMapIndex`가 선택한 맵 인덱스와 일치한다.
- [ ] 확인 버튼 클릭 시 `GamePlay` 씬으로 정상 진입한다(에러 없음).
- [ ] `GamePlay` 씬 진입 후 `DebugStageSelector`가 `SelectedRace`에 맞는 맵 루트를 활성화한다(예: 마법사+Stage2 선택 → 고블린 맵 활성).
- [ ] `GamePlay` 씬 진입 후 선택한 직업에 맞는 무기가 장착된다(전사=Sword, 마법사=Magic, 거너=Gun).

**에지 케이스 검증**

- [ ] `GamePlay` 씬을 Character Select 없이 에디터에서 단독 플레이 시 전사+Stage1(Slime) 기본 설정으로 진입하고 에러가 없다.
- [ ] Spine 자산이 배선되지 않은 직업을 선택해도 콘솔 에러 없이 빈 중앙 패널 상태로 유지된다.

---

> **문서 완료** (8/8 섹션). 코드 전수 기반 검증. 후속: `weapon-system.md` 작성 시 이 문서 상호 참조 추가.
