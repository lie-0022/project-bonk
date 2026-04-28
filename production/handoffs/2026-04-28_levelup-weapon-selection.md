# Editor 핸드오프: 레벨업 무기 선택 UI

> 작성: 2026-04-28
> 관련 코드: `LevelupWeaponSelection.cs`, `WeaponSelectionUI.cs`
> 관련 GDD: `design/gdd/levelup-selection.md`, `design/gdd/weapon-selection-ui.md`

---

## 0. 변경 요약

레벨업 시 자동으로 무기 3택 UI가 뜨도록 다음 2개 컴포넌트를 추가했다.

- `LevelupWeaponSelection` — XPSystem.OnLevelUp 수신 → 선택지 빌드 → 시간정지 → UI 트리거 → WeaponSystem 호출
- `WeaponSelectionUI` — 패널 + 카드 3장. 클릭 또는 1/2/3 키로 선택

기존 `WeaponDebugger`는 UI 열림 상태에서 1/2/3 키 입력을 무시하도록 가드 추가.

---

## 1. Scene: `GamePlay.unity`

### 1-1. LevelupWeaponSelection GameObject 생성

| 항목 | 값 |
|------|-----|
| GameObject 이름 | `LevelupWeaponSelection` |
| 위치 (Hierarchy) | `Systems` 그룹 하위 (또는 WeaponSystem 옆) |
| 추가할 컴포넌트 | `LevelupWeaponSelection` |
| Inspector 값 | `_choiceCount = 3` |

### 1-2. WeaponSelectionUI 패널 생성

체크리스트:

- [ ] 1. Canvas (없으면 `UI > Canvas` 생성, Render Mode = Screen Space - Overlay)
- [ ] 2. Canvas 하위에 `Panel_WeaponSelection` 빈 GameObject 생성 (RectTransform 전체 화면 stretch)
- [ ] 3. 자식으로 `Image` 컴포넌트 — 반투명 어두운 오버레이 (`Color = (0,0,0, 0.7)`, raycastTarget=on)
- [ ] 4. 그 위에 `Title` 텍스트 (TMP) — "레벨 업!" 상단 중앙
- [ ] 5. 가로 배치 컨테이너 `Cards` (HorizontalLayoutGroup, spacing 20) 화면 중앙
- [ ] 6. `Cards` 하위에 카드 프리팹 3개 (`Card_1`, `Card_2`, `Card_3`)
   - 각 카드 구조:
     - 루트: `Button` 컴포넌트 + `Image` (배경)
     - 자식 `GradeFrame` (Image, 등급 색상 테두리용)
     - 자식 `WeaponName` (TMP, 굵게)
     - 자식 `Grade` (TMP, "[에픽]" 등 표시)
     - 자식 `Description` (TMP, 한 줄 설명)
     - 자식 `State` (TMP, "신규" 또는 "강화 Lv X → Lv X+1")
- [ ] 7. `Panel_WeaponSelection`에 `WeaponSelectionUI` 컴포넌트 추가
- [ ] 8. Inspector에서 매핑:
   - `_panelRoot` ← Panel_WeaponSelection 자기 자신 (또는 비활성화 대상 부모)
   - `_cards[0]` ~ `_cards[2]` 각각 펼쳐서 Card_1/2/3 의 Root/Button/GradeFrame/WeaponNameText/DescriptionText/GradeText/StateText 매핑
- [ ] 9. 등급 색상 기본값 확인 (커먼 회색 / 에픽 보라 / 유니크 노랑 / 레전드 빨강)
- [ ] 10. **Panel_WeaponSelection 비활성화로 시작** (Inspector 체크박스 해제 — Awake에서 자동 Hide되지만 안전 차원)

### 1-3. Canvas 입력 설정

- [ ] 11. EventSystem이 Scene에 존재하는지 확인 (없으면 `UI > Event System` 생성)
- [ ] 12. New Input System 사용 중 → EventSystem 컴포넌트가 `InputSystemUIInputModule` 사용해야 함

---

## 2. 검증 시나리오

- [ ] Play 모드 진입
- [ ] 적 처치 → XP 획득 → 레벨업 발생
- [ ] **시간이 멈추고 카드 3장이 표시됨** (없으면 1~2장)
- [ ] 카드 1번 클릭 또는 키보드 `1` → 첫 무기가 슬롯에 추가/강화
- [ ] 패널 사라지고 전투 재개
- [ ] WeaponSystem 콘솔 또는 행동으로 무기 발동 확인
- [ ] 연속 레벨업 시 첫 선택 후 즉시 다음 카드 표시
- [ ] 모든 무기가 최대 레벨이면 카드 표시 없이 즉시 재개

---

## 3. 본인 → AI 피드백 양식

```
[작업 단계]: 체크리스트 X번
[증상]: ...
[콘솔 에러]: ...
```

---

## 4. 미흡 / 추후 작업

- 스펙(책) 선택지 — 패시브 시스템 확장 시 `BuildChoices()`에 합류 예정
- 카드 호버 애니메이션 (DOTween 또는 Animator)
- 사운드 (레벨업, 카드 선택, 무기 발동)
- WeaponDebugger는 추후 삭제 예정 (현재는 1/2/3 키 가드 적용됨)
