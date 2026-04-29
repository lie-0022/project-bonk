# Editor 핸드오프: 레벨업 선택 UI 패널 구축

> 작성: 2026-04-29
> 관련 GDD: `design/gdd/weapon-selection-ui.md`, `design/gdd/levelup-selection.md`
> 관련 코드: `WeaponSelectionUI.cs` (완성), `LevelupWeaponSelection.cs` (완성)
> **코드 변경 없음** — 모두 Editor 작업

---

## 0. 변경 요약

레벨업 시 무기/패시브 카드 3장이 표시되는 선택 패널을 Scene에 구축한다. 디버거(M/J/H/V/Y/K/Q/E/T/U/O/P, 1/2/3) 졸업 후 정식 흐름으로 무기/패시브를 획득하게 됨.

**MVP 디자인**: 단순 솔리드 박스 + 등급 색상 테두리 + TMP 텍스트. 호버/애니메이션/사운드 없음.
**구조**: Scene 직접 구축, Card_1 만들고 Duplicate.

---

## 작업 대상 Scene
**`Assets/Scenes/GamePlay.unity`**

작업 시작 전 이 Scene 열어두기.

---

## 스텝 1: LevelupWeaponSelection GameObject 추가

레벨업 이벤트를 수신하는 컨트롤러부터 부착.

### 체크리스트
- [ ] 1.1. Hierarchy 빈 곳 우클릭 → `Create Empty`
- [ ] 1.2. 이름 `LevelupWeaponSelection` (위치는 Systems 그룹 또는 최상단 어디든 OK)
- [ ] 1.3. Inspector → Add Component → `LevelupWeaponSelection`
- [ ] 1.4. Inspector에서 `_choiceCount = 3` 확인 (기본값 그대로)

### 검증
- [ ] Play 모드 진입
- [ ] 적 처치하면서 레벨업 발생까지 진행
- [ ] Console에 다음 경고 1줄 (스텝 2에서 사라질 예정):
  ```
  [LevelupWeaponSelection] 선택 UI 구독자 없음. 레벨업 선택 스킵.
  ```
- [ ] 게임 정상 진행 (timeScale=0으로 멈추지 않음)

⚠️ Play 종료 후 다음 스텝 진행.

---

## 스텝 2: Panel + 오버레이 + 타이틀 (카드 없이)

패널 자체를 만들고 활성화될 때 화면이 어두워지면서 "레벨 업!" 타이틀이 보이는지 확인.

### 체크리스트
- [ ] 2.1. Hierarchy에서 **Canvas** 선택 → 우클릭 → `Create Empty`
- [ ] 2.2. 새 GameObject 이름 `Panel_WeaponSelection`
- [ ] 2.3. RectTransform 설정:
  - 앵커 프리셋 `stretch / stretch` (Alt+클릭으로 위치도 같이)
  - Left/Right/Top/Bottom 모두 0
- [ ] 2.4. **Overlay 자식 추가**: Panel_WeaponSelection 우클릭 → UI → Image
  - 이름 `Overlay`
  - RectTransform: stretch 전체 (Left/Right/Top/Bottom = 0)
  - Color: `(0, 0, 0, 0.7)` 검은색 알파 0.7
  - Raycast Target: ✅ ON (배경 클릭으로 게임 입력 차단)
- [ ] 2.5. **Title 자식 추가**: Panel_WeaponSelection 우클릭 → UI → Text - TextMeshPro
  - (TMP Essential Resources 다이얼로그 뜨면 Import)
  - 이름 `Title`
  - 텍스트: `레벨 업!`
  - 폰트 크기 64, **Bold**, Color 흰색
  - Alignment: 가로 가운데, 세로 가운데
  - RectTransform 앵커 `top / center`, Pos Y = -100, Width = 600, Height = 100
- [ ] 2.6. **WeaponSelectionUI 스크립트 부착**
  - Panel_WeaponSelection 선택 → Add Component → `WeaponSelectionUI`
  - Inspector `_panelRoot` 필드 ← Panel_WeaponSelection 자기 자신 드래그
  - `_cards[]` 배열은 다음 스텝에서 매핑 (지금은 그대로 둠)
- [ ] 2.7. **패널 비활성화로 시작**: Panel_WeaponSelection의 Inspector 좌상단 체크박스 OFF
  - (Awake에서 자동 Hide 되지만 안전 차원)

### 검증
- [ ] Play 모드 진입
- [ ] 레벨업 발생까지 진행
- [ ] **화면이 어두워지고 "레벨 업!" 텍스트 표시**
- [ ] 게임이 멈춤 (timeScale=0)
- [ ] 카드는 아직 없으므로 선택 못 함

⚠️ **이 스텝은 게임 진행 불가 상태로 빠짐. 검증 후 즉시 Play 종료**.

### 문제 발생 시
- 패널이 안 보임 → Panel_WeaponSelection 활성화 상태 확인, RectTransform stretch 재확인
- 글자 안 보임 → Title의 RectTransform 위치 확인, 폰트 색상이 배경과 같은 검정인지

---

## 스텝 3: Card_1 한 장 만들기

카드 1장만 만들어서 표시 흐름 검증. 작업 단계가 가장 많은 스텝.

### 3-1. Cards 컨테이너 생성
- [ ] 3.1.1. Panel_WeaponSelection 우클릭 → `Create Empty` → 이름 `Cards`
- [ ] 3.1.2. RectTransform: 앵커 `middle / center`, Pos (0, 0), Width 800, Height 400
- [ ] 3.1.3. Add Component → `Horizontal Layout Group`
  - Spacing: 20
  - Child Alignment: Middle Center
  - Control Child Size: Width ✅, Height ✅
  - Use Child Scale: 둘 다 OFF
  - Child Force Expand: Width ✅, Height ✅

### 3-2. Card_1 본체 생성
- [ ] 3.2.1. Cards 우클릭 → UI → Image
  - 이름 `Card_1`
  - Color: 어두운 회색 `(0.15, 0.15, 0.15, 1.0)`
- [ ] 3.2.2. Add Component → `Layout Element`
  - Min Width: 240, Min Height: 360
  - Preferred Width: 240, Preferred Height: 360
- [ ] 3.2.3. Add Component → `Button`
  - Target Graphic: Card_1 자기 Image (자동 매핑됨)
  - Transition: Color Tint (기본값 OK)

### 3-3. GradeFrame 자식
- [ ] 3.3.1. Card_1 우클릭 → UI → Image
  - 이름 `GradeFrame`
  - RectTransform: stretch 전체 (Left/Right/Top/Bottom = -4 정도로 카드보다 살짝 크게)
  - Source Image: 비워둠 (단색 사용)
  - Color: 흰색 (코드에서 등급 색상으로 자동 변경됨)
  - Raycast Target: OFF (클릭 가로채지 않게)
- [ ] 3.3.2. **GradeFrame을 Card_1에서 가장 위 자식으로 이동** (Hierarchy에서 드래그) — 다른 자식들이 위에 그려지도록

### 3-4. 텍스트 4개 자식
각각 Card_1 우클릭 → UI → Text - TextMeshPro로 생성.

| 이름 | 텍스트 | 폰트 크기 | 정렬 | RectTransform |
|------|--------|----------|------|---------------|
| `WeaponName` | (비워둠) | 32 Bold | 가운데 | 앵커 top/center, Pos Y -50, W 220, H 50 |
| `Grade` | (비워둠) | 14 | 가운데 | 앵커 top/center, Pos Y -100, W 220, H 24 |
| `Description` | (비워둠) | 16 | 가운데 | 앵커 middle/center, Pos Y 0, W 220, H 60 |
| `State` | (비워둠) | 18 Bold | 가운데 | 앵커 bottom/center, Pos Y 50, W 220, H 30 |

- [ ] 3.4.1. WeaponName 생성
- [ ] 3.4.2. Grade 생성
- [ ] 3.4.3. Description 생성
- [ ] 3.4.4. State 생성

### 3-5. WeaponSelectionUI Inspector 매핑
Panel_WeaponSelection 선택 → WeaponSelectionUI 컴포넌트 펼침 → `_cards[0]` 펼침:

- [ ] 3.5.1. Root ← Card_1 (Hierarchy에서 드래그)
- [ ] 3.5.2. Button ← Card_1 (Button 컴포넌트 인식됨)
- [ ] 3.5.3. GradeFrame ← Card_1/GradeFrame
- [ ] 3.5.4. WeaponNameText ← Card_1/WeaponName
- [ ] 3.5.5. DescriptionText ← Card_1/Description
- [ ] 3.5.6. GradeText ← Card_1/Grade
- [ ] 3.5.7. StateText ← Card_1/State

### 3-6. 검증
- [ ] Panel_WeaponSelection 비활성화 확인 (Inspector 체크박스 OFF)
- [ ] Play 모드 진입
- [ ] 레벨업 발생까지 진행
- [ ] **카드 1장 표시** (예: "검 [에픽] 주변 부채꼴 베기 신규")
- [ ] **1키** 눌러 선택 → 패널 사라지고 게임 재개
- [ ] Console 로그에서 무기/패시브 적용 확인 (예: `[WeaponSlot] Sword Lv1`)
- [ ] 다시 레벨업 → 카드 1장 다시 표시
- [ ] **마우스 클릭**으로 선택 → 정상 작동

### 문제 발생 시
- 카드 클릭 안 됨 → Button 컴포넌트 확인, GradeFrame Raycast Target OFF 확인
- 텍스트 자리 어긋남 → 각 TMP의 RectTransform 위치 재조정
- 등급 색상 안 보임 → GradeFrame이 Card_1보다 위에 자식 순서인지 확인

---

## 스텝 4: Card_1 → Card_2, Card_3 복제

### 체크리스트
- [ ] 4.1. Hierarchy에서 Card_1 선택 → Ctrl+D (복제) → 이름 `Card_2`
- [ ] 4.2. 다시 Card_1 선택 → Ctrl+D → 이름 `Card_3`
- [ ] 4.3. WeaponSelectionUI Inspector → `_cards[1]` 펼침
  - Root ← Card_2
  - Button ← Card_2
  - GradeFrame ← Card_2/GradeFrame
  - WeaponNameText ← Card_2/WeaponName
  - DescriptionText ← Card_2/Description
  - GradeText ← Card_2/Grade
  - StateText ← Card_2/State
- [ ] 4.4. `_cards[2]` 펼침 → Card_3로 동일하게 모든 필드 매핑

### 검증
- [ ] Play 모드 진입
- [ ] 레벨업 발생
- [ ] **카드 3장 표시** (서로 다른 무기/패시브, 다른 등급 색상)
- [ ] **1, 2, 3 키** 각각 다른 카드 선택 가능
- [ ] 마우스 클릭으로 각 카드 선택 가능
- [ ] 선택 후 패널 사라지고 게임 재개
- [ ] 연속 레벨업 시 즉시 다음 카드 표시

---

## 스텝 5: 최종 시나리오 검증

### 체크리스트
- [ ] 5.1. **레벨업 표시**: 화면 어두워지고 카드 3장 + "레벨 업!" 타이틀
- [ ] 5.2. **혼합 등장**: 무기 카드와 패시브 카드 둘 다 등장
- [ ] 5.3. **등급 색상 구분**:
  - 커먼 → 회색 테두리
  - 에픽 → 보라 테두리
  - 유니크 → 노랑 테두리
  - 레전드 → 빨강 테두리
- [ ] 5.4. **상태 라벨**: "신규" 또는 "Lv X → Lv X+1" 표시
- [ ] 5.5. **키 입력**: 1/2/3으로 선택 가능
- [ ] 5.6. **마우스 입력**: 클릭으로 선택 가능
- [ ] 5.7. **시간 재개**: 선택 후 timeScale=1, 전투 진행
- [ ] 5.8. **연속 레벨업**: 한 번 선택 후 다음 카드 자동 표시
- [ ] 5.9. **모두 최대 레벨**: 카드 안 뜨고 즉시 재개 (Lv15 모두 도달 후)
- [ ] 5.10. **WeaponDebugger 충돌 없음**: UI 열린 상태에서 1/2/3은 카드 선택 (디버거 동작 안 함)

---

## 본인 → AI 피드백 양식

문제 발생 시:

```
[작업 단계]: 스텝 X-Y-Z 번
[증상]: ...
[Console 에러]: (있으면 그대로 복사)
[추가 정보]: ...
```

---

## 미흡 / 추후 작업

| 항목 | 비고 |
|------|------|
| 카드 호버 효과 (살짝 확대) | 폴리시 단계 |
| 사운드 (레벨업/카드 등장/선택) | AudioSource + 클립 추가 후 |
| 카드 아이콘 (무기 또는 패시브 시각화) | 텍스트만으로 MVP 충분 |
| 키보드 포커스 표시 (선택된 카드 강조) | 게임패드 지원 시 필수 |
| WeaponDebugger / PassiveDebugger 삭제 | UI 안정화 후 정리 |

---

## 작업 후 알려주세요

- 스텝 단위로 검증하면서 막히면 즉시 알려주세요
- 모든 스텝 통과하면 디버거 컴포넌트 정리 진행할지 논의
