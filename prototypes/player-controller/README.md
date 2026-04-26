# Prototype: Player Controller

## Unity 설정 방법 (5분)

### 1. 씬 준비
1. Unity 6.3 LTS에서 새 씬 생성
2. Plane 오브젝트 추가 (바닥, Scale 10,1,10)
3. Capsule 오브젝트 추가 (플레이어)
   - `CharacterController` 컴포넌트 추가 (자동: RequireComponent)
   - `PlayerController.cs` 추가

### 2. 카메라 설정
1. Main Camera 선택
2. `CameraController.cs` 추가
3. `target` 필드에 Capsule 드래그
4. Camera의 Transform은 스크립트가 자동 설정함

### 3. 실행
- Play 버튼
- WASD: 이동
- 마우스: 캐릭터가 커서 방향 바라봄
- Space: 커서 방향으로 대시

## 테스트 체크리스트

- [ ] WASD 이동이 아이소메트릭 카메라 기준으로 자연스러운가?
- [ ] 카메라 Lerp 추적이 부드러운가? (smoothSpeed 5.0 기준)
- [ ] 마우스 방향이 정확하게 계산되는가?
- [ ] 대시 후 1.5초 쿨타임이 느껴지는가?
- [ ] 대시 중 IsInvincible = true 확인 (Debug.Log)
- [ ] 커서가 플레이어 위치와 겹쳐도 크래시 없는가?

## 튜닝 포인트

Inspector에서 직접 조정:
- `smoothSpeed`: 낮추면 cinematic, 높이면 즉시 추적
- `moveSpeed`: 너무 느리면 답답, 너무 빠르면 적 회피 쉬움
- `dashDuration`: 대시 거리에 직접 영향 (speed × duration)
- `dashCooldown`: 대시 타이밍 긴장감 조절

## 알려진 이슈

- 카메라 앵글이 정확히 45°이면 Unity에서 Y축 회전 방향에 따라
  좌우 이동 축이 반대로 보일 수 있음. 이 경우 `offset`의 Y/Z 값 조정.
- 경사면 없는 평면 아레나 기준. 장애물 있으면 `slopeLimit` 조정 필요.
