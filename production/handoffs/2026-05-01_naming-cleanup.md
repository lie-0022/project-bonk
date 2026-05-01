# Editor 핸드오프: 네이밍·구조 정리

> 작성: 2026-05-01
> 코드 변경 **없음** — 모두 Unity Editor 작업
> 관련 계획: 코드 일관성·네이밍·구조 점검

---

## 0. 변경 요약

다음 신규 기능 작업 전 자잘한 정리:
- Scene Hierarchy의 trailing space 9개 제거
- 그룹 헤더 1개 형식 통일
- Weapon Data 파일 3개 prefix 추가 (`Weapon_*`)
- 폐기된 `Data/Skills/` 폴더 삭제

각 단계마다 Play 검증.

---

## 스텝 1: Scene Hierarchy trailing space 제거

`GamePlay.unity` 열고 Hierarchy에서 다음 GameObject 이름 끝 공백 제거.

### 체크리스트

- [ ] 1.1. `Cards ` → `Cards`
- [ ] 1.2. `Card1 ` → `Card1`
- [ ] 1.3. `Card1/GradeFrame ` → `Card1/GradeFrame`
- [ ] 1.4. `Card1/WeaponName ` → `Card1/WeaponName`
- [ ] 1.5. `Card2/GradeFrame ` → `Card2/GradeFrame`
- [ ] 1.6. `Card2/WeaponName ` → `Card2/WeaponName`
- [ ] 1.7. `Card3/GradeFrame ` → `Card3/GradeFrame`
- [ ] 1.8. `Card3/WeaponName ` → `Card3/WeaponName`
- [ ] 1.9. `LevelupWeaponSelection ` → `LevelupWeaponSelection`
- [ ] 1.10. **Ctrl+S 저장**

### 작업 방법
- 각 GameObject 클릭 → F2 → 이름 끝의 공백을 Backspace로 삭제 → Enter

### 검증
- Play ▶ → 적 처치 → 레벨업 → 카드 정상 표시
- Console missing reference 에러 없음

---

## 스텝 2: 그룹 헤더 통일

다른 그룹 헤더(`── Camera ── `, `── World ── ` 등)는 끝에 공백이 있는데 `── Managers ──` 만 없음.

### 체크리스트
- [ ] 2.1. Hierarchy `── Managers ──` 선택 → F2 → 끝에 공백 1개 추가 → `── Managers ── ` (Enter)
- [ ] 2.2. **Ctrl+S 저장**

> 이건 미관 통일이라 굳이 안 해도 됨. 신경 쓰이면 처리.

---

## 스텝 3: Weapon Data 파일 이름 변경

⚠️ **Unity Project 창에서만 rename**. Windows 탐색기 / Finder로 변경 금지 (.meta 짝 깨짐).

### 체크리스트
- [ ] 3.1. Project 창 → `Assets/Data/Weapons/` 폴더 열기
- [ ] 3.2. `Sword_Data.asset` 우클릭 → **Rename** → `Weapon_Sword_Data` (확장자 .asset 자동)
- [ ] 3.3. `Gun_Data.asset` → `Weapon_Gun_Data`
- [ ] 3.4. `Magic_Data.asset` → `Weapon_Magic_Data`
- [ ] 3.5. **Ctrl+S 저장**

### 검증
**필수** — GUID 추적 확인:
- [ ] Hierarchy에서 `WeaponSystem` 선택
- [ ] Inspector의 **Sword Data**, **Gun Data**, **Magic Data** 필드가 새 이름 (Weapon_...)으로 표시되는지 확인
- [ ] `Missing (Weapon Data SO)` 라고 떠 있으면 GUID 추적 실패 → 다시 드래그해서 매핑
- [ ] Play ▶ → 무기 1/2/3으로 추가 → 발동 정상 (검 휘두름, 총 발사, 마법 발사)

---

## 스텝 4: 폐기된 Data/Skills 폴더 삭제

"스킬 4개" 컨셉이 폐기됐고 폴더는 비어있음.

### 체크리스트
- [ ] 4.1. Project 창 → `Assets/Data/Skills/` 우클릭 → **Delete**
- [ ] 4.2. 다이얼로그 확인 → Delete
- [ ] 4.3. **Ctrl+S 저장**

### 검증
- Console에 에러 없음 (참조하던 곳이 없으니까 OK)

---

## 본인 → AI 피드백 양식

문제 발생 시:
```
[작업 단계]: 스텝 X-Y 번
[증상]: ...
[Console 에러]: (있으면 그대로 복사)
```

특히 스텝 3 (Weapon Data rename) 후 Missing 에러 뜨면 즉시 알려주세요.

---

## 작업 후

전체 완료되면 알려주세요:
- 검증 모두 통과 → 커밋·푸시
- 다음 단계: 무기 슬롯 HUD 또는 캐릭터 스탯창 (선택)
