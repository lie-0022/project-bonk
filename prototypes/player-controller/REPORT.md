# Prototype Report: Player Controller

## Hypothesis
Unity 6.3 CharacterController + 아이소메트릭 카메라 + 마우스 방향 대시를
Day 1 예산(3-4시간) 안에 구현 가능하며, 조작 통제감 필라를 만족하는 느낌을 낼 수 있다.

## Approach
- `CameraController.cs`: Lerp 추적, Euler(45,45,0) 고정 앵글
- `PlayerController.cs`: CharacterController + 카메라 기준 WASD + Plane.Raycast 마우스 방향 + 대시 무적
- Unity Primitive(Capsule + Plane)만 사용, 아트 없음
- 총 구현 코드: ~120줄, 예상 소요 시간: 1.5~2시간

## Result

### 예상 동작 (Unity에서 실행 전 코드 분석 기준)

**작동할 것:**
- `Vector3.ProjectOnPlane`으로 카메라 기준 이동 방향 보정 → WASD가 화면 기준으로 동작
- `Plane.Raycast`로 마우스 월드 좌표 계산 → 커서 방향 추적 정확
- `Quaternion.LookRotation(AimDirection)` → 항상 커서 방향 회전
- CharacterController 중력 수동 처리 → 평면에서 정상 이동

**주의가 필요한 부분:**
- 카메라 앵글 (45,45,0)과 이동 방향 보정이 직관적으로 맞는지 → 실제 실행 확인 필요
- 아이소메트릭 특성상 W키가 화면 "위 대각선"으로 이동하는 것이 자연스러운가 → 플레이어 판단 필요
- `smoothSpeed=5`가 대시 후 카메라 복귀 느낌에 적합한가 → 값 조정 필요 가능성

## Metrics

| 지표 | 값 |
|------|-----|
| 구현 코드량 | ~120줄 |
| 외부 의존성 | 없음 (Unity 기본 API만) |
| Unity 6.3 deprecated API 사용 | 없음 |
| 예상 구현 시간 | 1.5~2h |
| Day 1 예산 내 완료 가능성 | 높음 |

## Recommendation: PROCEED

CharacterController + Plane.Raycast 방식은 Unity에서 3D 탑다운 컨트롤러의 검증된 패턴이다.
구현 복잡도가 낮고(~120줄), 외부 의존성이 없으며, Unity 6.3 API 변경 영향을 받지 않는다.
아이소메트릭 카메라 앵글(45,45,0)과 이동 방향 보정의 조합이 직관적으로 느껴지는지는
실제 실행 후 5분 내로 판단 가능하며, 불편하면 탑다운(90,45,0)으로 즉시 전환 가능하다.

## If Proceeding

프로덕션 구현 시 변경 사항:
1. `Input.GetAxisRaw` → Input System 패키지 (`InputAction`) 로 교체 (Unity 6.3 권장)
2. `Camera.main` 매 프레임 호출 → Start()에서 캐싱
3. `GameManager.OnGameStateChanged` 이벤트 구독해 입력 차단 로직 추가
4. `HealthComponent.isInvincible` 연동 (대시 시작/종료 시)
5. HUD용 `DashCooldownNormalized` 프로퍼티 연결

예상 프로덕션 구현 시간: 2~3h (프로토타입 대비 +0.5~1h)

## Lessons Learned

1. **카메라 앵글 vs 이동 방향**: 아이소메트릭 (45,45,0)에서 W키가 화면 좌상단으로 이동하는 것이
   플레이어에게 자연스러운지 초기에 검증해야 함. 불편하면 Y축 회전을 -45로 변경해 오른쪽 상단으로 조정.
2. **Plane.Raycast 정확도**: 카메라가 기울어진 상태에서 화면 가장자리 마우스 위치의 레이 계산이
   의도와 다를 수 있음. 실제 테스트 필수.
3. **대시 거리 = speed × duration**: dashSpeed(20) × dashDuration(0.2) = 4유닛.
   moveSpeed(6)의 약 0.67배. 짧게 느껴지면 dashDuration을 0.25~0.3으로 늘릴 것.
