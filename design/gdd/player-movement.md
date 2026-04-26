# 플레이어 이동 (Player Movement)

> **Status**: Designed
> **Author**: 사용자 + Claude
> **Last Updated**: 2026-04-17
> **Implements Pillar**: 조작의 통제감

## Overview

플레이어 이동 시스템은 WASD 키로 오빗 카메라 기준 방향으로 캐릭터를 이동시키고, Shift로 대시, Space로 점프를 실행하는 시스템이다. Unity `CharacterController` 컴포넌트 기반으로 구현하며, 캐릭터는 항상 이동 방향을 바라본다. 대시 중에는 무적 프레임이 적용되며, 낙하 거리가 일정 이상이면 낙하 데미지가 발생한다.

## Player Fantasy

WASD를 누르는 순간 캐릭터가 즉각 반응하며 이동 방향을 바라본다. 마우스로 카메라를 돌려 유리한 각도를 잡고, Shift 대시로 적의 공격을 통과하는 아찔한 쾌감이 있다. Space로 지형을 뛰어넘어 위기를 탈출하는 순간 "내가 이 전장을 완전히 제어하고 있다"는 통제감을 준다.

## Detailed Design

### Core Rules

1. **이동**: WASD 입력을 오빗 카메라 기준 방향으로 보정해 `CharacterController.Move()` 호출
2. **이동 방향 보정**: `CameraController.Forward`, `CameraController.Right` 벡터를 기준으로 입력 벡터 변환
3. **캐릭터 회전**: 이동 방향을 바라봄 (마우스 커서 방향 아님). 이동 입력 없을 때는 마지막 방향 유지
4. **대시**: Shift 키 입력 시 현재 이동 방향(또는 마지막 이동 방향)으로 고속 돌진
5. **대시 무적**: 대시 시작 시 `HealthComponent.IsInvincible = true`, 대시 종료 시 `false`
6. **대시 쿨타임**: 대시 후 쿨타임 동안 재사용 불가
7. **대시 중 이동 입력 무시**: 대시 중 WASD 입력 차단 (대시 방향으로만 이동)
8. **점프**: Space 키 입력 시 수직 속도 부여. 공중에서 재점프(더블 점프) 불가
9. **중력**: 공중에서 `Physics.gravity`에 따라 자연스럽게 낙하
10. **낙하 데미지**: 낙하 시작 높이에서 착지 높이의 차이가 `fallDamageThreshold`를 초과하면 데미지 적용
11. **이동 불가 상태**: 게임 상태가 Playing이 아닐 때 모든 입력 차단

### States and Transitions

| 상태 | 조건 | 이동 | 대시 | 점프 |
|------|------|------|------|------|
| Idle | 입력 없음, 지상 | 정지 | ✅ | ✅ |
| Moving | WASD 입력, 지상 | 이동 방향으로 이동 | ✅ | ✅ |
| Dashing | Shift 입력 | 이동 방향으로 돌진 | ❌ | ❌ |
| DashCooldown | 대시 종료 후 쿨타임 | 정상 이동 | ❌ | ✅ |
| Jumping | Space 입력 후 상승 중 | 공중 이동 가능 | ✅ | ❌ |
| Falling | 하강 중 | 공중 이동 가능 | ✅ | ❌ |
| Disabled | 게임 상태 비Playing | 정지 | ❌ | ❌ |

### Interactions with Other Systems

- **← 카메라 시스템**: `CameraController.Forward`, `CameraController.Right` 벡터로 이동 방향 계산
- **← 게임 상태 관리**: `OnGameStateChanged` 수신 → Playing 외 상태에서 입력 차단
- **→ 체력 시스템**: 대시 시작/종료 시 `IsInvincible` 플래그 설정, 낙하 데미지 `TakeDamage()` 호출
- **→ HUD 시스템**: 대시 쿨타임 값(`DashCooldownNormalized`) 제공

## Formulas

```
이동 벡터 계산 (오빗 카메라 기준):
  inputVector = Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"))
  moveDir     = (CameraController.Forward * inputVector.z
               + CameraController.Right   * inputVector.x).normalized
  velocity    = moveDir * moveSpeed
  CharacterController.Move(velocity * Time.deltaTime)

캐릭터 회전 (이동 방향을 바라봄):
  if moveDir != Vector3.zero:
    targetRot = Quaternion.LookRotation(moveDir)
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime)

대시 이동:
  dashVelocity = _lastMoveDir * dashSpeed
  position    += dashVelocity * Time.deltaTime  // dashDuration 동안
  // _lastMoveDir: 마지막 유효한 이동 방향 (Idle 중 대시 시 현재 facing 방향)

대시 쿨타임:
  dashCooldownRemaining -= Time.deltaTime
  canDash = (dashCooldownRemaining <= 0)

점프 & 중력:
  if (isGrounded && Input.GetKeyDown(Space)):
    _verticalVelocity = jumpForce
  _verticalVelocity += Physics.gravity.y * Time.deltaTime
  CharacterController.Move(Vector3.up * _verticalVelocity * Time.deltaTime)

낙하 데미지:
  // 지상에서 공중으로 전환 시 _fallStartY 기록
  if (!wasGrounded && isGrounded):
    fallDistance = _fallStartY - transform.position.y
    if fallDistance > fallDamageThreshold:
      damage = (fallDistance - fallDamageThreshold) * fallDamageMultiplier
      HealthComponent.TakeDamage(damage)

변수 기본값:
  moveSpeed              = 6.0    // 단위/초
  airSpeedMultiplier     = TBD    // 공중 이동속도 배율 (지상과 별도, 개발 중 결정)
  rotationSpeed          = 720.0  // 도/초 (캐릭터 회전 속도)
  dashSpeed              = 20.0   // 단위/초
  dashDuration           = 0.2    // 초
  dashCooldown           = 1.5    // 초
  jumpForce              = 8.0    // 초기 수직 속도
  fallDamageThreshold    = 6.0    // 이 이상 낙하 시 데미지 발생 (유닛)
  fallDamageMultiplier   = 5.0    // (낙하거리 - threshold) * multiplier = 데미지
```

## Edge Cases

- **대시 중 사망**: 대시 무적이므로 사망 불가. 대시 종료 후 HP 0이면 GameOver
- **Idle 중 대시**: 이동 입력이 없을 때 Shift 입력 시 캐릭터 현재 facing 방향으로 대시
- **대시 연속 입력**: 쿨타임 중 Shift 입력 무시 (큐잉 없음)
- **대시 중 점프 입력**: 대시 중 Space 입력 무시
- **공중 대시**: 공중에서도 대시 가능. 무적 프레임 적용됨. 낙하 중 대시로 착지 데미지 회피 가능
- **대시 중 게임오버**: 외부 요인으로 GameOver 전환 시 대시 즉시 중단, 무적 해제
- **벽/장애물 충돌 중 대시**: `CharacterController`가 충돌 처리 — 벽 방향 슬라이드
- **낙하 데미지 중 무적**: 대시 중(무적) 착지 시 낙하 데미지 무시
- **게임 시작 직후 입력**: `Starting` 상태에서 모든 입력 차단

## Dependencies

### 업스트림
- **카메라 시스템**: `CameraController.Forward`, `CameraController.Right` 벡터
- **게임 상태 관리**: Playing 상태 여부로 입력 활성화

### 다운스트림
- 체력 시스템 (`IsInvincible` 설정, 낙하 데미지)
- 적 AI (`Transform.position` 참조)
- HUD 시스템 (대시 쿨타임 값 제공)

### 인터페이스 계약
- `PlayerMovement.DashCooldownNormalized` — 0~1 float (0=준비됨, 1=방금 사용), HUD 쿨타임 UI용
- `PlayerMovement.IsDashing` — 현재 대시 중 여부

## Tuning Knobs

| 변수 | 기본값 | 안전 범위 | 설명 |
|------|--------|----------|------|
| `moveSpeed` | 6.0 | 3.0 ~ 10.0 | 낮으면 답답함, 높으면 적 회피가 너무 쉬움 |
| `dashSpeed` | 20.0 | 10.0 ~ 30.0 | 낮으면 대시가 짧음, 높으면 화면 밖으로 나갈 수 있음 |
| `dashDuration` | 0.2초 | 0.1 ~ 0.35초 | 길수록 이동 거리 증가 |
| `dashCooldown` | 1.5초 | 0.5 ~ 3.0초 | 짧을수록 대시 남용 가능 |
| `jumpForce` | 8.0 | 5.0 ~ 12.0 | 높을수록 점프가 높아지고 낙하 데미지 위험 증가 |
| `fallDamageThreshold` | 6.0 | 4.0 ~ 10.0 | 낮을수록 점프만 해도 데미지, 높을수록 낙하 데미지가 드묾 |

## Visual/Audio Requirements

- 대시 중: 잔상(Trail) 이펙트 (Vertical Slice)
- 대시 시작: 짧은 이동 사운드 (선택적)
- 착지 낙하 데미지: 충격 이펙트 + 카메라 셰이크 (선택적)
- 대시 쿨타임 완료: HUD 쿨타임 아이콘 복귀 피드백

## UI Requirements

- **대시 쿨타임 아이콘**: HUD에 대시 쿨타임 게이지 표시 (HUD 시스템에서 구현)

## Acceptance Criteria

- [ ] WASD 입력 시 오빗 카메라 기준 방향으로 이동함
- [ ] 캐릭터가 항상 이동 방향을 바라봄
- [ ] Shift 키 입력 시 이동 방향으로 대시하며 무적 프레임 적용됨
- [ ] 대시 후 `dashCooldown`(1.5초) 동안 재사용 불가
- [ ] 대시 쿨타임이 HUD에 표시됨
- [ ] Space 키 입력 시 점프하며, 공중 재점프 불가
- [ ] `fallDamageThreshold`(6.0) 이상 낙하 착지 시 데미지 적용됨
- [ ] 게임 상태가 Playing이 아닐 때 이동/대시/점프 입력이 동작하지 않음
- [ ] 대시 중 적의 공격을 맞아도 HP가 감소하지 않음

## Open Questions

- **대시 무적 프레임 시각화**: 무적 중 캐릭터를 반투명하게 표시. Vertical Slice에서 검토.
- **이동 속도 버프**: 무기 획득으로 이동 속도를 증가시키는 옵션. 무기 시스템 설계 시 반영.
- **공중 이동 속도**: 지상과 별도 배율 적용으로 확정. 수치는 플레이테스트 후 결정 (TBD).
