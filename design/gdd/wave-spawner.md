# 웨이브 스폰 (Wave Spawner)

> **Status**: Designed
> **Author**: 사용자 + Claude
> **Last Updated**: 2026-03-26
> **Implements Pillar**: 파워 에스컬레이션

## Overview

웨이브 스폰 시스템은 시간 기반으로 적을 지속적으로 스폰하고 웨이브를 진행하는 시스템이다. 뱀파이어 서바이벌처럼 웨이브가 자동으로 넘어가며, 시간이 지날수록 적의 수와 강도가 증가한다. 총 5개의 웨이브로 구성되며, 마지막 웨이브 완료 시 게임 상태 관리자에 Win 이벤트를 전달한다.

## Player Fantasy

웨이브가 거듭될수록 화면을 가득 채우는 적들. 처음엔 몇 마리 상대하다가, 나중엔 수십 마리가 사방에서 밀려오는 압박감이 파워 에스컬레이션의 핵심이다. 내 스킬 빌드가 완성되어 갈수록 그 압박을 헤쳐나가는 쾌감도 커진다.

## Detailed Design

### Core Rules

1. 게임 시작 시 Wave 1부터 자동으로 진행
2. 각 웨이브는 `waveDuration`(초) 동안 지속되며, 시간이 다 되면 자동으로 다음 웨이브로 전환
3. 웨이브 진행 중 `spawnInterval`마다 적을 스폰
4. 스폰 위치: 플레이어로부터 `spawnRadius` 거리의 랜덤 방향 (화면 밖 스폰)
5. 웨이브가 높아질수록 스폰 수(`spawnCount`)와 적 체력·속도가 증가
6. Wave 5 종료 시 `GameManager.ChangeState(Win)` 호출
7. 게임 상태가 Playing이 아닐 때 스폰 일시정지
8. 적 스폰은 오브젝트 풀에서 꺼내는 방식으로 처리

### Wave 구성표

| 웨이브 | 지속 시간 | 스폰 간격 | 적 구성 | 적 체력 배율 | 적 속도 배율 |
|--------|----------|----------|---------|------------|------------|
| Wave 1 | 60초 | 2.0초 | 추적형 100% | ×1.0 | ×1.0 |
| Wave 2 | 60초 | 1.5초 | 추적형 80% / 돌진형 20% | ×1.2 | ×1.1 |
| Wave 3 | 60초 | 1.2초 | 추적형 70% / 돌진형 30% | ×1.5 | ×1.2 |
| Wave 4 | 60초 | 1.0초 | 추적형 60% / 돌진형 40% | ×1.8 | ×1.3 |
| Wave 5 | 60초 | 0.8초 | 추적형 50% / 돌진형 50% | ×2.2 | ×1.5 |

### States and Transitions

| 상태 | 조건 | 동작 |
|------|------|------|
| Spawning | Playing 상태 + 웨이브 진행 중 | `spawnInterval`마다 적 스폰 |
| WaveTransition | 웨이브 시간 만료 | 짧은 대기(1초) 후 다음 웨이브 시작 |
| Paused | 비Playing 상태 | 스폰 타이머 정지 |
| Complete | Wave 5 종료 | `GameManager.ChangeState(Win)` |

### Interactions with Other Systems

- **← 게임 상태 관리**: Playing 상태에서만 스폰 진행
- **→ 게임 상태 관리**: Wave 5 완료 시 `ChangeState(Win)` 호출
- **→ 적 AI**: 스폰 시 풀에서 꺼내 `EnemyAI.Init()` 호출, 적 체력·속도 배율 적용
- **← 오브젝트 풀링**: 적 인스턴스를 풀에서 요청
- **→ HUD 시스템**: 현재 웨이브 번호, 웨이브 타이머 값 제공

## Formulas

```
스폰 타이머:
  spawnTimer -= Time.deltaTime
  if spawnTimer <= 0:
    SpawnEnemy()
    spawnTimer = currentWave.spawnInterval

스폰 위치:
  angle = Random.Range(0, 360) degrees
  spawnPos = player.position + Vector3(cos(angle), 0, sin(angle)) * spawnRadius

적 타입 결정:
  roll = Random.value (0.0 ~ 1.0)
  if roll < currentWave.chaserRatio: spawn Chaser
  else: spawn Rusher

웨이브 전환:
  waveTimer -= Time.deltaTime
  if waveTimer <= 0:
    currentWave++
    if currentWave > totalWaves: GameManager.ChangeState(Win)
    else: StartNextWave()

적 스탯 적용:
  enemy.maxHP    = baseMaxHP    * currentWave.hpMultiplier
  enemy.moveSpeed = baseMoveSpeed * currentWave.speedMultiplier

기본값:
  spawnRadius = 15.0  // 플레이어로부터 스폰 거리
  totalWaves  = 5
  waveTransitionDelay = 1.0초
```

## Edge Cases

- **웨이브 전환 중 적 처리**: WaveTransition 1초 동안 기존 적은 계속 활동. 스폰만 잠시 멈춤
- **스폰 위치가 장애물 내부**: MVP에서는 장애물이 없는 평면 아레나이므로 발생하지 않음
- **풀이 비어있을 때 스폰 요청**: 풀 확장 또는 스킵 처리 (오브젝트 풀링 GDD에서 정의)
- **게임 오버 후 웨이브 타이머**: 게임 상태가 GameOver로 전환되면 즉시 모든 타이머 정지
- **Wave 5 종료 시 적이 남아있는 경우**: Win 상태로 전환하되 남은 적은 비활성화

## Dependencies

### 업스트림
- **적 AI**: `EnemyAI.Init()` 인터페이스
- **오브젝트 풀링**: 적 인스턴스 요청
- **게임 상태 관리**: Playing 상태 여부, Win 이벤트 전달

### 다운스트림
- HUD 시스템 (웨이브 번호, 타이머 제공)

### 인터페이스 계약
- `WaveSpawner.CurrentWave` — 현재 웨이브 번호 (HUD용)
- `WaveSpawner.WaveTimeRemaining` — 현재 웨이브 남은 시간 (HUD용)

## Tuning Knobs

| 변수 | 기본값 | 안전 범위 | 설명 |
|------|--------|----------|------|
| `waveDuration` | 60초 | 30 ~ 120초 | 짧을수록 빠른 진행, 길수록 생존 압박 증가 |
| `spawnRadius` | 15.0 | 10 ~ 20 | 너무 가까우면 즉사 위험, 너무 멀면 긴장감 감소 |
| Wave 5 `spawnInterval` | 0.8초 | 0.5 ~ 1.5초 | 최종 웨이브 밀도 조절 |
| `waveTransitionDelay` | 1.0초 | 0 ~ 3.0초 | 0이면 즉시 전환, 길수록 숨 돌릴 시간 |

## Visual/Audio Requirements

- 웨이브 전환 시: "WAVE 2!" 텍스트 화면 중앙에 잠깐 표시
- 스폰 이펙트: 스폰 위치에서 간단한 이펙트 (선택적, MVP 후순위)

## UI Requirements

- **웨이브 번호**: HUD 상단에 "WAVE 1 / 5" 표시
- **웨이브 타이머**: 남은 시간 초 단위 표시 (선택적)

## Acceptance Criteria

- [ ] 게임 시작 시 Wave 1이 자동으로 시작되고 적이 스폰됨
- [ ] 60초 후 Wave 2로 자동 전환됨
- [ ] 웨이브가 높아질수록 스폰 간격이 짧아지고 적이 더 많이 나옴
- [ ] Wave 5 종료 후 Win 상태로 전환됨
- [ ] 게임 상태가 Paused일 때 스폰이 멈춤
- [ ] 적이 항상 플레이어로부터 `spawnRadius` 거리에서 스폰됨

## Open Questions

- **웨이브 간 레벨업 보상**: 웨이브 전환 시 자동으로 레벨업 한 번 주는 방식도 고려 가능. 경험치 시스템과 연계해 검토.
