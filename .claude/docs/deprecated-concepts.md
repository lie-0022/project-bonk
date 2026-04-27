# Deprecated Concepts (참고 금지)

> 초기 기획에서 폐기된 컨셉 모음. 코드/문서/이력에서 언급되어도 **참고 금지**.
> 새 작업 시 이 페이지를 우선 확인해 혼란을 방지한다.

---

## 2026-04-26: "스킬 4개" 컨셉 폐기

### 폐기된 내용

- **스킬 4종**: 대시베기(DashSlash) / 회오리(Whirlwind) / 검기(BladeBeam) / 처형(Execute)
- **Q/W/E/R 슬롯 시스템**
- **`SkillSystem` 클래스 / `ISkillData` / `ISkillSlot` 인터페이스 / `SkillType` enum**
- **검사 1캐릭터 + 4스킬 빌드** 컨셉

### 대체된 것

- **무기 3종**: Sword / Gun / Magic
- **`WeaponSystem` 싱글턴** (최대 3슬롯, 독립 타이머 자동 발동)
- **`WeaponDataSO` ScriptableObject + WeaponSlot + 각 Attack 컴포넌트**
- **무기·스펙 혼합 레벨업 선택** (Q/W/E/R 슬롯 X)

### 잔재 출처

- `production/_archive/sprints/sprint-01.md` — Sprint-01 본체 (아카이브, 그대로 유지)
- 기타 본문에 "스킬" 단어가 일반 명사로 남은 부분 — 정리 진행 중

### 새 작업 시 규칙

| 잘못된 표현 | 올바른 표현 |
|------------|-------------|
| "스킬 4개" / "QWER 스킬" | 사용 금지 |
| "SkillSystem" | `WeaponSystem` |
| "ISkillData" / "ISkillSlot" | `WeaponDataSO` / `WeaponSlot` |
| "SkillType.DashSlash" 등 | 폐기 — 코드에 없음 |
| "스킬 선택 UI" | `무기 선택 UI` (또는 `LevelupSelection`) |
| "스킬 발동" (일반 명사) | `무기 발동` (게임플레이 컨텍스트) |

### 참고 코드

- `src/BladeSurge/Assets/Scripts/Weapons/WeaponSystem.cs` — 무기 시스템 본체
- `src/BladeSurge/Assets/Scripts/Weapons/WeaponSlot.cs` — 슬롯 단위
- `src/BladeSurge/Assets/Scripts/Weapons/WeaponDataSO.cs` — 무기 데이터

---

## 향후 폐기 항목

(추가될 때마다 본 페이지에 누적 기록)
