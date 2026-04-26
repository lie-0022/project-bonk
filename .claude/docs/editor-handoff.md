# Unity Editor 핸드오프 표준

> AI가 코드 작성을 끝낸 뒤 본인이 Unity Editor에서 실제로 적용해야 하는 작업의 표준 양식.
> AI는 코드 작업 종료 시 **반드시** 이 양식 중 하나(또는 둘 다)로 핸드오프를 제공한다.

---

## 양식 1: 표 양식 (정형 데이터)

GameObject 1개 + 컴포넌트 추가 같은 단발성 작업에 사용.

| 항목 | 값 |
|------|-----|
| GameObject 이름 | `Player` |
| 위치 (Hierarchy) | `GamePlay > ── Gameplay ── > Player` |
| Tag | `Player` |
| Layer | `8 (Player)` |
| 추가할 컴포넌트 | `PlayerController`, `Rigidbody`, `CapsuleCollider` |
| Inspector 값 | `PlayerController.MoveSpeed = 5.0`<br>`Rigidbody.Mass = 1`, `Rigidbody.UseGravity = true`<br>`CapsuleCollider.Height = 2`, `Radius = 0.5` |
| 참조 연결 | `PlayerController.cameraTransform → Main Camera` |
| Prefab 저장 위치 | `Assets/Prefabs/Player_Swordsman.prefab` |

---

## 양식 2: 체크리스트 양식 (단계별 작업)

Scene 다중 객체 배치, 복잡한 셋업, 새로운 기능 통합 시 사용.

```
- [ ] 1. Scene 'GamePlay' 열기
- [ ] 2. ── Gameplay ── 그룹 하위에 빈 GameObject 'Player' 생성
- [ ] 3. Tag = Player, Layer = Player(8) 설정
- [ ] 4. PlayerController 컴포넌트 Add Component
- [ ] 5. Rigidbody Add (Use Gravity ✅, Freeze Rotation X/Z ✅)
- [ ] 6. CapsuleCollider Add (Height 2, Radius 0.5)
- [ ] 7. Inspector에서 PlayerController.MoveSpeed = 5.0
- [ ] 8. cameraTransform 필드에 Main Camera 드래그
- [ ] 9. Assets/Prefabs/ 로 드래그하여 Prefab 저장
- [ ] 10. Play 모드 진입 → WASD 이동 확인
```

각 항목은 **하나의 클릭/입력 액션** 단위로 쪼갠다.

---

## 핸드오프 시 AI가 반드시 명시하는 것

1. **어떤 Scene/Prefab에서 작업?** (e.g. `GamePlay.unity`, `Player_Swordsman.prefab`)
2. **어떤 GameObject에 영향?** (정확한 Hierarchy 경로)
3. **어떤 컴포넌트를 추가/제거?**
4. **Inspector 값** — public 필드 + `[SerializeField]` 표시된 private 필드만
5. **참조 연결** — 어떤 필드에 어떤 객체를 드래그하는지
6. **검증 방법** — Play 모드에서 어떻게 동작 확인하는지

> ⚠️ **존재하지 않는 메뉴/방법 추측 금지** (memory 룰).
> Unity 6.3 LTS에서 실제 존재하는 메뉴/방법만 안내한다.

---

## 본인 → AI 피드백 양식

Editor 작업 중 문제 발생 시 본인이 AI에게 보고하는 양식:

```
[작업 단계]: 체크리스트 4번
[증상]: PlayerController가 컴포넌트 목록에 안 보임
[콘솔 에러]: (있으면 그대로 복사)
[추가 정보]: (있으면)
```

---

## 예시: PlayerController 적용 핸드오프

### 컨텍스트
AI가 `Assets/Scripts/Player/PlayerController.cs`를 작성했다.
본인은 이걸 Scene에 적용해야 한다.

### AI가 제공한 핸드오프

**대상 Scene**: `GamePlay.unity`
**대상 GameObject**: `── Gameplay ── > Player`
**Prefab**: 작업 후 `Assets/Prefabs/Player_Swordsman.prefab` 으로 저장

| 작업 | 값 |
|------|-----|
| Tag | `Player` |
| Layer | `8 (Player)` |
| Components | `PlayerController`, `CharacterController`, `Animator` |

**Inspector 설정**:
- `PlayerController._moveSpeed` = `5.0`
- `PlayerController._dashSpeed` = `15.0`
- `PlayerController._jumpForce` = `6.0`
- `PlayerController._cameraTransform` ← **drag** Main Camera

**검증**:
1. Play 모드 진입
2. WASD → 카메라 기준 이동
3. Shift → 0.3초간 가속 + 무적 (콘솔 로그 `[Dash] start/end` 확인)
4. Space → 점프

문제 발생 시 위 "본인 → AI 피드백 양식" 사용.
