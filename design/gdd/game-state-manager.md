# 게임 상태 관리 (Game State Manager)

> **Status**: Designed
> **Author**: 사용자 + Claude
> **Last Updated**: 2026-03-26
> **Implements Pillar**: 조작의 통제감

## Overview

게임 상태 관리 시스템은 [TBD]의 모든 상태 전환을 중앙에서 관리하는 싱글턴 컴포넌트다. Starting, Playing, Paused, GameOver, Win 다섯 가지 상태를 정의하고, 각 상태에 따라 다른 시스템(카메라, 플레이어, 적 AI 등)의 동작을 활성화하거나 비활성화한다. 게임의 "틀"로서 모든 시스템이 현재 상태를 구독하고 그에 맞게 반응한다.

## Player Fantasy

플레이어는 상태 관리 시스템의 존재를 의식하지 않는다. 게임이 자연스럽게 시작되고, ESC로 언제든 멈출 수 있으며, 죽으면 즉시 결과가 표시되고, R 키 하나로 다시 시작된다. 상태 전환이 매끄러울수록 플레이어는 게임에 몰입할 수 있다.

## Detailed Design

### Core Rules

1. 게임 상태 관리자는 씬에 하나만 존재하는 싱글턴(`GameManager.Instance`)이다
2. 상태는 5가지: `Starting`, `Playing`, `Paused`, `GameOver`, `Win`
3. 상태 변경은 반드시 `GameManager.Instance.ChangeState(newState)`를 통해서만 이루어진다 (직접 필드 변경 금지)
4. 상태 변경 시 `OnGameStateChanged(GameState newState)` 이벤트를 브로드캐스트한다
5. 각 시스템은 이 이벤트를 구독해 자체적으로 반응한다 (GameManager가 개별 시스템을 직접 제어하지 않음)
6. **승리 조건**: 마지막 웨이브(Wave 5) 클리어 시 `Win` 상태로 전환
7. **패배 조건**: 플레이어 체력이 0 이하가 되면 `GameOver` 상태로 전환
8. **일시정지**: Playing 상태에서 ESC 키 입력 시 `Paused`로 전환, 다시 ESC 입력 시 `Playing`으로 복귀
9. **재시작**: GameOver 또는 Win 상태에서 R 키 입력 시 씬을 리로드해 `Starting`으로 초기화

### States and Transitions

| 상태 | 설명 | 진입 조건 | 가능한 전환 |
|------|------|----------|------------|
| `Starting` | 씬 로드 직후 초기화 중 | 씬 시작 | → Playing (초기화 완료) |
| `Playing` | 정상 게임 진행 중 | Starting 완료 / Paused에서 재개 | → Paused / → GameOver / → Win |
| `Paused` | 일시정지 | Playing 중 ESC | → Playing (ESC 재입력) |
| `GameOver` | 플레이어 사망 | 플레이어 HP ≤ 0 | → Starting (R키 재시작) |
| `Win` | 최종 웨이브 클리어 | 마지막 웨이브 완료 | → Starting (R키 재시작) |

### Interactions with Other Systems

- **→ 카메라 시스템**: `OnGameStateChanged` 수신 → Playing일 때 Following, 그 외 Frozen
- **→ 플레이어 이동**: `OnGameStateChanged` 수신 → Playing/Starting 외 상태에서 입력 비활성화
- **→ 웨이브 스폰**: `OnGameStateChanged` 수신 → Playing일 때만 스폰 진행, Paused 시 일시정지
- **→ HUD 시스템**: `OnGameStateChanged` 수신 → GameOver/Win 시 결과 화면 표시
- **← 플레이어 체력 시스템**: 체력 0 이하 이벤트 수신 → `ChangeState(GameOver)` 호출
- **← 웨이브 스폰**: 마지막 웨이브 완료 이벤트 수신 → `ChangeState(Win)` 호출

## Formulas

```
상태 전환 로직 (의사코드):
  void ChangeState(GameState newState):
    if currentState == newState: return  // 중복 전환 방지
    currentState = newState
    Time.timeScale = (newState == Paused) ? 0 : 1  // 일시정지 시 시간 정지
    OnGameStateChanged?.Invoke(newState)

승리 조건:
  if (currentWave >= totalWaves && waveEnemiesCleared):
    ChangeState(Win)

패배 조건:
  if (player.currentHP <= 0):
    ChangeState(GameOver)
```

## Edge Cases

- **Starting 중 입력**: Starting 상태에서는 플레이어 입력을 완전히 차단. 초기화가 완료되기 전에 이동/스킬 발동 방지
- **동시 패배+승리**: 마지막 웨이브 처치 중 플레이어도 사망하는 경우 → GameOver 우선 (플레이어 HP 이벤트가 웨이브 완료 이벤트보다 먼저 처리)
- **Paused 중 GameOver 불가**: `Time.timeScale = 0`이므로 체력 감소가 없어 자연스럽게 방지됨
- **중복 상태 전환**: 동일 상태로의 전환 요청은 무시 (무한 이벤트 루프 방지)
- **씬 리로드**: 재시작은 씬 전체 리로드(`SceneManager.LoadScene`)로 처리해 상태 완전 초기화 보장

## Dependencies

### 업스트림 (이 시스템이 의존하는 것)
- **없음** — Foundation 레이어

### 다운스트림 (이 시스템에 의존하는 것)
- 카메라 시스템, 플레이어 이동, 웨이브 스폰, HUD 시스템 — 모두 `OnGameStateChanged` 이벤트 구독

### 인터페이스 계약
- `GameManager.Instance.CurrentState` — 읽기 전용 프로퍼티
- `GameManager.Instance.ChangeState(GameState)` — 상태 전환 메서드
- `GameManager.OnGameStateChanged` — C# 이벤트 (Action<GameState>)

## Tuning Knobs

| 변수 | 기본값 | 안전 범위 | 설명 |
|------|--------|----------|------|
| `totalWaves` | 5 | 3 ~ 10 | 승리에 필요한 총 웨이브 수 |
| Starting 유지 시간 | 0.5초 | 0 ~ 2초 | 초기화 후 Playing 전환까지 딜레이 (0이면 즉시) |

## Visual/Audio Requirements

- GameOver 전환 시: 화면 페이드 아웃 효과 (선택적 — MVP에서는 텍스트만도 충분)
- Win 전환 시: 화면 페이드 아웃 또는 간단한 플래시 효과 (선택적)

## UI Requirements

- **GameOver 화면**: "GAME OVER" 텍스트 + "R - 재시작" 안내
- **Win 화면**: "CLEAR!" 텍스트 + 생존 시간 + 킬 수 + "R - 재시작" 안내
- MVP에서는 Canvas UI 텍스트로 단순 구현

## Acceptance Criteria

- [ ] 씬 시작 시 `Starting` → `Playing` 자동 전환됨
- [ ] 플레이어 HP가 0이 되면 `GameOver` 상태로 전환되고 GameOver 화면이 표시됨
- [ ] 마지막 웨이브 클리어 시 `Win` 상태로 전환되고 결과 화면이 표시됨
- [ ] Playing 중 ESC 입력 시 게임이 일시정지되고 (`Time.timeScale = 0`) 적이 멈춤
- [ ] 일시정지 중 ESC 재입력 시 게임이 재개됨
- [ ] GameOver/Win 상태에서 R 입력 시 씬이 리로드되고 처음부터 시작됨
- [ ] 동일 상태로의 중복 전환 시 이벤트가 두 번 발생하지 않음

## Open Questions

- **씬 전환 vs 리셋**: 재시작 시 씬 전체 리로드 대신 오브젝트 상태를 수동 리셋하는 방식도 가능. 현재는 씬 리로드로 단순화. 로딩이 느릴 경우 수동 리셋으로 전환 검토.
