# 카메라 시스템 (Camera System)

> **Status**: Designed
> **Author**: 사용자 + Claude
> **Last Updated**: 2026-04-17
> **Implements Pillar**: 조작의 통제감

## Overview

카메라 시스템은 플레이어를 중심으로 마우스 수평 이동에 따라 Y축으로 회전하는 오빗(Orbit) 카메라다. 카메라는 플레이어로부터 고정 거리와 고정 앙각을 유지하며, 플레이어가 이동하면 부드럽게 따라간다. 캐릭터는 카메라 방향이 아닌 이동 방향을 바라보며, 카메라는 이 회전에 관여하지 않는다. 모든 이동 방향의 기준축을 제공하므로 Day 1에 가장 먼저 검증해야 한다.

## Player Fantasy

마우스를 좌우로 드래그하면 카메라가 캐릭터 주위를 부드럽게 회전한다. 원하는 각도에서 전장을 내려다보며 적의 움직임을 파악할 수 있다. 카메라 조작이 자연스러울수록 "내가 상황을 통제하고 있다"는 느낌을 준다.

## Detailed Design

### Core Rules

1. **카메라 회전**: 마우스 X축 이동 → 플레이어 중심으로 Y축 회전 (`_yaw += mouseX * rotationSpeed`)
2. **앙각 고정**: 카메라의 X축 회전(Pitch)은 고정값(`pitchAngle = 40°`), 마우스 Y 입력으로 변경 불가
3. **거리 고정**: 플레이어로부터 카메라까지 구면 거리 고정 (`distance = 12`)
4. **카메라 위치 계산**: Yaw와 Pitch를 구면 좌표로 변환해 플레이어 기준 오프셋 계산
5. **플레이어 추적**: 매 프레임 플레이어 `Transform.position`으로 Lerp 추적
6. **LookAt**: 카메라는 항상 플레이어 위치를 바라봄
7. **게임 상태**: Playing일 때만 마우스 입력을 받음. Paused / GameOver / Win 시 회전 입력 차단 (위치 추적은 유지)
8. **커서 잠금**: 게임 중 `Cursor.lockState = CursorLockMode.Locked`로 커서를 화면 중앙에 고정해 무한 회전 지원

### States and Transitions

| 상태 | 조건 | 동작 |
|------|------|------|
| Idle | 게임 시작 | 초기 yaw로 플레이어 바라봄, 즉시 스냅 |
| Following | Playing 상태 | 마우스 입력으로 회전 + 플레이어 Lerp 추적 |
| Frozen | Paused / GameOver / Win | 회전 입력 차단, 위치만 Lerp 추적 |

### Interactions with Other Systems

- **→ 플레이어 이동**: `CameraController.Forward`, `CameraController.Right` 벡터를 WASD 이동 방향 계산에 제공
- **← 게임 상태 관리**: `OnGameStateChanged` 수신 → 회전 입력 활성/비활성

## Formulas

```
매 프레임 카메라 위치 업데이트:

  // 1. 마우스 입력으로 yaw 갱신 (Playing 상태에서만)
  _yaw += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime

  // 2. 구면 좌표로 카메라 위치 계산
  float pitchRad = pitchAngle * Mathf.Deg2Rad
  float yawRad   = _yaw * Mathf.Deg2Rad
  Vector3 offset = new Vector3(
      Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
      Mathf.Sin(pitchRad),
      Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
  ) * distance

  // 3. 카메라 위치 Lerp
  Vector3 targetPos = player.position + offset
  camera.position = Vector3.Lerp(camera.position, targetPos, smoothSpeed * Time.deltaTime)

  // 4. 항상 플레이어를 바라봄
  camera.LookAt(player.position)

  // 5. 이동용 방향 벡터 제공
  Forward = new Vector3(Mathf.Sin(yawRad), 0, Mathf.Cos(yawRad)).normalized
  Right   = new Vector3(Mathf.Cos(yawRad), 0, -Mathf.Sin(yawRad)).normalized

변수 기본값:
  pitchAngle    = 40.0   // 고정 앙각 (도)
  distance      = 12.0   // 구면 반지름 (유닛)
  rotationSpeed = 120.0  // 회전 감도 (도/초)
  smoothSpeed   = 8.0    // 위치 추적 Lerp 속도
```

## Edge Cases

- **게임 시작 시 튀는 현상**: 씬 시작 직후 Lerp 없이 초기 위치로 즉시 스냅 후 Following 전환
- **재시작 시**: `_yaw = 0` 리셋 후 즉시 스냅
- **Paused 중 마우스 입력**: `Time.timeScale = 0`이지만 마우스 입력은 계속 들어올 수 있음 → Playing 상태 체크로 차단
- **프레임레이트 독립성**: 위치 Lerp에 `Time.deltaTime` 사용, 회전에도 `Time.deltaTime` 곱해 프레임 독립적 처리
- **극단적 smoothSpeed 값**: 0에 가까워지면 카메라가 영원히 도달 못함 → 최솟값 1.0 강제

## Dependencies

### 업스트림
- **없음** — Foundation 레이어

### 다운스트림
- **플레이어 이동**: `CameraController.Forward`, `CameraController.Right` 벡터 참조
- **게임 상태 관리**: `OnGameStateChanged` 이벤트 구독

### 인터페이스 계약
- `CameraController.Forward` — 카메라 기준 수평 forward 벡터 (Y=0, normalized)
- `CameraController.Right` — 카메라 기준 수평 right 벡터 (Y=0, normalized)

## Tuning Knobs

| 변수 | 기본값 | 안전 범위 | 설명 |
|------|--------|----------|------|
| `pitchAngle` | 40° | 30 ~ 60° | 낮을수록 수평 시점, 높을수록 탑뷰에 가까움 |
| `distance` | 12.0 | 8 ~ 18 | 짧을수록 캐릭터가 크게 보임, 길수록 전장 파악 유리 |
| `rotationSpeed` | 120.0 | 60 ~ 240 | 낮을수록 둔감, 높을수록 민감 |
| `smoothSpeed` | 8.0 | 1.0 ~ 15.0 | 낮을수록 부드러운 추적, 높을수록 즉시 추적 |

## Visual/Audio Requirements

- 카메라 셰이크: 강한 피격·폭발 시 화면 진동 (Vertical Slice에서 별도 CameraShake 컴포넌트로 추가)

## UI Requirements

- 없음 (카메라 시스템은 UI를 직접 포함하지 않음)

## Acceptance Criteria

- [ ] 마우스 X 이동 시 카메라가 플레이어 중심으로 Y축 회전함
- [ ] 카메라 앙각이 항상 `pitchAngle`(40°)로 고정됨
- [ ] 플레이어 이동 시 카메라가 `smoothSpeed`로 부드럽게 따라감
- [ ] 게임 시작 / 재시작 시 카메라가 초기 위치로 즉시 스냅됨 (튀는 현상 없음)
- [ ] Paused / GameOver 상태에서 마우스를 움직여도 카메라가 회전하지 않음
- [ ] 60fps 기준 카메라 추적이 프레임 독립적으로 동작함

## Open Questions

- **앙각(Pitch) 조절 여부**: 마우스 Y 입력으로 앙각 조절 추가 시 자유도 증가. MVP에서는 고정 앙각으로 단순화, Vertical Slice에서 검토.
- **카메라 충돌 처리**: 카메라가 벽/지형에 가려지는 경우. MVP 오픈 아레나에서는 발생하지 않음. 지형 추가 시 별도 처리 필요.
