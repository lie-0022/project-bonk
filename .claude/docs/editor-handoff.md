# Unity Editor 핸드오프 표준 양식

> AI가 코드 작성을 끝낸 뒤 본인이 Unity Editor에서 수행해야 할 작업 안내 형식.
> **모든 핸드오프는 본 문서의 "표준 양식" 그대로 따른다.** 임의 변형 금지.

---

## 베이스라인 (전제 — 절대 설명하지 말 것)

본인은 Unity 6.3 LTS를 일상적으로 사용하는 개발자다.
다음은 **모두 안다고 가정하며, AI는 절대 설명하지 않는다**:

- 씬 열기 / 저장 / 전환
- Hierarchy에서 GameObject 생성·삭제·이름변경(F2)·우클릭 메뉴
- Ctrl+D 복제, 부모-자식 드래그
- Inspector에서 `Add Component` 버튼 위치
- RectTransform 다루기 (Anchor Preset, Pos/Width/Height)
- TMP Importer 첫 등장 시 `Import TMP Essentials` 누르는 것
- Play 모드 ▶ 버튼 위치
- Project 창에서 파일 찾기·드래그
- Prefab 저장 (Hierarchy → Project 드래그)
- Tag / Layer 메뉴 위치
- Console 창 위치 / 에러 색상
- Build Profile / Build 메뉴

> 이런 단계가 핸드오프에 1줄이라도 들어가면 양식 위반이다.

---

## 표준 양식 (이 5섹션만 사용)

```markdown
## Editor 핸드오프 — {기능명}

### 대상
- **Scene/Prefab**: {파일명}
- **부모 경로**: {Hierarchy 경로}

### Hierarchy 구성
{ASCII 트리 한 덩어리}

### 컴포넌트 / RectTransform
| GameObject | 컴포넌트 / 설정 |
|------------|----------------|
| ... | ... |

### Inspector 연결
| 필드 | 연결 대상 |
|------|----------|
| ... | ... |

### 검증
1. ...
2. ...
```

---

## 섹션별 작성 규칙

### 대상
- Scene 파일명 또는 Prefab 경로 1줄
- 영향받는 GameObject의 Hierarchy 경로 1줄
- 그 외 잡설 금지

### Hierarchy 구성
- ASCII 트리로 **신규로 만들 구조만** 표시
- 기존 GameObject는 최상단 1개만 컨텍스트로 보여주고 나머지 생략
- 인덴트는 공백 3칸 또는 ` └─ ` 사용

### 컴포넌트 / RectTransform
- 표 1개. `GameObject | 컴포넌트 / 설정` 2열
- 컴포넌트 이름은 정확하게 (`Vertical Layout Group`, `TextMeshProUGUI`, `Rigidbody` 등)
- RectTransform 값은 한 줄로: `Anchor=bottom-right, Pos(-20,20), Size(200,100)`
- 선택사항은 `(선택)` 접미사

### Inspector 연결
- 표 1개. `필드 | 연결 대상` 2열
- 필드 경로는 코드의 `[SerializeField]` 이름 그대로 (`_slots[0].Root`)
- 연결 대상은 Hierarchy 경로 (`Slot0`, `Slot0/Label`)
- "드래그하세요" 같은 동사 생략

### 검증
- 번호 매긴 1줄짜리 체크 항목
- "Play 진입 후 X → Y가 보임" 형태
- Console 에러 없을 것은 마지막 항목 1줄

---

## 절대 쓰지 말 것

- "Unity 상단 메뉴: File > Open Scene"
- "Hierarchy 창에서 우클릭 → Create Empty"
- "F2 눌러서 이름 바꾸기"
- "Ctrl+D 두 번 눌러서 복제"
- "Inspector 맨 아래 Add Component 버튼 클릭"
- "검색창에 X 입력"
- "▶ Play 버튼"
- 친절한 어조의 도입부 ("자, 이제 시작하자" 같은 거)
- 각 단계마다 "잘 됐으면 다음으로" 같은 멘트
- 이모지

---

## 본인 → AI 피드백 양식 (문제 발생 시)

```
[Step]: 어디서 막혔는지 (e.g. "Inspector 연결 표 3행")
[증상]: 무엇이 안 보이거나 에러
[Console 에러]: (있으면 그대로 복사)
```

---

## 예시 (참고용)

### 좋은 예

```markdown
## Editor 핸드오프 — 무기 슬롯 HUD

### 대상
- **Scene**: GamePlay.unity
- **부모 경로**: Canvas

### Hierarchy 구성
Canvas
 └─ WeaponSlotsHUD
    ├─ Slot0
    │  └─ Label
    ├─ Slot1
    │  └─ Label
    └─ Slot2
       └─ Label

### 컴포넌트 / RectTransform
| GameObject | 컴포넌트 / 설정 |
|------------|----------------|
| WeaponSlotsHUD | RectTransform: Anchor=bottom-right, Pos(-20,20), Size(200,100). `WeaponSlotsHUD` 컴포넌트. `Vertical Layout Group` (선택) |
| Slot0/1/2 | RectTransform 기본 |
| Label | TextMeshProUGUI (폰트 크기 임시 18~24) |

### Inspector 연결
| 필드 | 연결 대상 |
|------|----------|
| _slots[0].Root | Slot0 |
| _slots[0].Label | Slot0/Label |
| _slots[1].Root | Slot1 |
| _slots[1].Label | Slot1/Label |
| _slots[2].Root | Slot2 |
| _slots[2].Label | Slot2/Label |

### 검증
1. Play 진입 시 슬롯 0개 — 화면에 아무것도 안 보임
2. 레벨업 → 무기 1개 선택 → 첫 줄에 `검 Lv 1`
3. 같은 무기 재선택 → `검 Lv 2`
4. 다른 무기 추가 → 두 번째 줄 활성
5. Console 에러 없음
```

### 나쁜 예 (양식 위반)

> "Unity 켜고 GamePlay.unity 더블클릭으로 열어. Hierarchy에서 Canvas 우클릭 → Create Empty 하고 이름을 WeaponSlotsHUD로 바꿔(F2). 그다음에 Inspector에서 RectTransform의 Anchor Preset을 클릭해서..."

→ 베이스라인 위반. 5섹션 양식 위반.
