# 오브젝트 풀링 (Object Pooling)

> **Status**: Designed
> **Author**: 사용자 + Claude
> **Last Updated**: 2026-03-26
> **Implements Pillar**: (인프라 — 필라 직접 구현 아님)

## Overview

오브젝트 풀링 시스템은 적 오브젝트를 런타임에 반복 생성·파괴하는 대신 미리 생성해두고 재사용하는 인프라 시스템이다. 50+ 적 동시 처리 시 `Instantiate/Destroy` 호출로 인한 GC 스파이크와 성능 저하를 방지한다. MVP에서는 없어도 시작할 수 있지만, 웨이브가 진행될수록 필수가 된다.

## Player Fantasy

플레이어가 직접 인식하지 않는 시스템. 하지만 프레임이 끊기지 않아야 파워 트립이 유지된다.

## Detailed Design

### Core Rules

1. 게임 시작 시 각 적 타입별로 `initialPoolSize`개의 오브젝트를 미리 생성해 비활성화
2. 스폰 요청 시: 풀에서 비활성화된 오브젝트 꺼내기 → 활성화 → `Init()` 호출
3. 사망/반환 시: 비활성화 → 풀에 반환 (Destroy 호출하지 않음)
4. 풀이 비어있을 때: 새 오브젝트 자동 생성 (풀 자동 확장)
5. 풀은 적 타입별로 독립적으로 관리 (추적형 풀, 돌진형 풀)

### Interactions with Other Systems

- **← 웨이브 스폰**: `GetFromPool(enemyType)` 호출
- **← 적 AI**: 사망 시 `ReturnToPool()` 호출

## Formulas

```
초기 풀 크기:
  추적형 initialPoolSize = 50
  돌진형 initialPoolSize = 20

스폰:
  obj = pool.Find(o => !o.activeInHierarchy)
  if obj == null: obj = Instantiate(prefab)  // 풀 확장
  obj.SetActive(true)
  obj.GetComponent<EnemyAI>().Init()
  return obj

반환:
  obj.SetActive(false)
  // 위치/상태는 Init()에서 초기화하므로 여기서 리셋 불필요
```

## Edge Cases

- **풀 고갈**: 자동 확장으로 처리. 확장 시 로그 출력 (디버그용)
- **반환 중복 호출**: `activeInHierarchy` 체크로 이미 반환된 오브젝트 중복 반환 방지

## Dependencies

### 업스트림
없음 (Foundation 레이어)

### 다운스트림
- 웨이브 스폰, 적 AI

### 인터페이스 계약
- `ObjectPool.GetFromPool(EnemyType type)` → `GameObject`
- `ObjectPool.ReturnToPool(GameObject obj)`

## Tuning Knobs

| 변수 | 기본값 | 설명 |
|------|--------|------|
| 추적형 `initialPoolSize` | 50 | 웨이브 5 최대 동시 적 수 이상으로 설정 |
| 돌진형 `initialPoolSize` | 20 | |

## Acceptance Criteria

- [ ] 게임 시작 시 풀이 초기화됨
- [ ] 적 스폰 시 `Instantiate` 없이 풀에서 꺼내서 사용됨
- [ ] 적 사망 시 `Destroy` 없이 풀에 반환됨
- [ ] 풀 고갈 시 자동 확장되고 스폰이 정상 동작함
- [ ] 50마리 동시 처리 시 프레임드롭 없이 60fps 유지

## Open Questions

없음
