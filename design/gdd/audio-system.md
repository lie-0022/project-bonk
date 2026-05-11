# Audio System

## Overview
BGM/SFX/UI 3채널 오디오 시스템. AudioManager 싱글턴이 AudioCatalogSO에 등록된 클립을 이벤트 기반으로 재생한다. SFX는 `SfxEvent` enum, BGM은 `BgmTrack` enum으로 식별.

## Player Fantasy
- 메인 메뉴: 판타지 모험의 시작 분위기 (신비로움)
- 게임플레이: 빠른 비트로 액션 텐션 유지, 두 트랙 순환으로 단조로움 회피
- 보스전: 비트 전환으로 위협 신호. 최종 보스는 웅장함으로 클라이맥스

## BGM 트랙 매핑 (현 상태)

| 트랙 enum | 파일명 | 사용처 | 후킹 |
|---|---|---|---|
| `MainMenu` | `BGM_MainMenu.mp3` (메인-판타지+신비로운) | 메인 메뉴 화면 | ❌ (메뉴 Canvas 미빌드) |
| `GameplayStage1` | `BGM_Stage1.mp3` (배경1) | Stage 1 일반 진행 | ✅ 플레이리스트 [Stage1, Stage2] |
| `GameplayStage2` | `BGM_Stage2.mp3` (배경2) | Stage 2 일반 진행 (현재 Stage1과 순환) | ✅ 동상 |
| `BossBattle` | `BGM_Boss.mp3` (보스전-빠른비트) | Stage 1/2 보스 등장 시 | ✅ WaveSpawner.SpawnBoss |
| `BossBattleFinal` | `BGM_Boss_Final.mp3` (보스전-웅장) | Stage 3 진입 시 (보스만 등장) | ❌ Stage 시스템 미도입 |
| `GameOver` | (미수신) | 사망 화면 | ❌ |
| `GameClear` | (미수신) | 클리어 화면 | ❌ |

## Detailed Rules

### 단일 트랙 재생
`AudioManager.Instance.PlayBgm(BgmTrack.X)` — 동일 트랙이면 무시. 다른 트랙이면 즉시 전환. (현재 페이드 없음)

### 플레이리스트 재생
`AudioManager.Instance.PlayBgmPlaylist(BgmTrack.A, BgmTrack.B, ...)` — 첫 트랙 재생 후 끝나면 다음 트랙, 끝까지 가면 첫 트랙으로 순환. 개별 클립의 `Loop` 플래그 무시하고 강제로 끝까지 재생 후 다음으로 넘어감.

### Stage별 BGM 정책
- **Stage 1**: 플레이리스트 [배경1, 배경2] 순환. 보스 등장 시 → 보스전-빠른비트
- **Stage 2**: 동일 플레이리스트 (현재 구현 단계에서). 보스 등장 시 → 보스전-빠른비트
- **Stage 3**: ★처음부터 보스 등장★ → 보스전-웅장만 재생. 일반 BGM 없음
- 클리어 시 → (GameClear 트랙 미수신, 일단 무음 또는 BGM 페이드아웃)
- 게임오버 시 → (GameOver 트랙 미수신, 일단 무음)

### SFX 재생
`AudioManager.Instance.Play(SfxEvent.X)` — 카탈로그에서 클립 조회. 동일 이벤트에 클립 여러 개면 랜덤 선택. `PitchVariance > 0`이면 ±랜덤 피치 적용.

## Formulas

### 실제 볼륨 = 마스터 × 채널 × 베이스 × 엔트리
```
SFX final volume = SettingsService.MasterVolume
                 × SettingsService.SfxVolume
                 × AudioManager._sfxBaseVolume
                 × entry.VolumeScale
                 × playOneShot volumeScale (PlayOneShot 인수)
```
BGM/UI도 동일 패턴.

### 피치 랜덤
```
pitch = 1 + Random.Range(-PitchVariance, +PitchVariance)
```
재생 후 원래 피치로 복원.

## Edge Cases
- 카탈로그 미연결 / 미매핑 이벤트 호출 → 조용히 무시 (로그 없음)
- 동일 BGM 재호출 → 무시 (재시작 안 함)
- 플레이리스트 중 PlayBgm 단일 호출 → 플레이리스트 해제 후 단일 재생
- AudioManager 인스턴스 없음 (다른 씬) → 호출부에서 null 체크 후 무시
- DontDestroyOnLoad — 씬 전환 시 BGM 끊김 없음. 같은 트랙 호출이면 그대로 진행

## Dependencies
- `SettingsService` (마스터/채널 볼륨 영속화)
- `AudioCatalogSO` (`Assets/Data/Audio/AudioCatalog.asset`)
- `GameManager.Start` (Stage1 플레이리스트 시작)
- `WaveSpawner.SpawnBoss` (보스 BGM 전환)

## Tuning Knobs
- `AudioCatalogSO._sfxEntries[].VolumeScale` / `PitchVariance`
- `AudioCatalogSO._bgmEntries[].VolumeScale` / `Loop`
- `AudioManager._bgmBaseVolume` / `_sfxBaseVolume` / `_uiBaseVolume`
- `SettingsService.MasterVolume` / `BgmVolume` / `SfxVolume` / `UiVolume`

## Acceptance Criteria
1. Play ▶ 시 BGM_Stage1 자동 재생
2. BGM_Stage1 끝나면(약 97.6초 후) 자동으로 BGM_Stage2 재생, 그 후 BGM_Stage1로 순환
3. 보스 스폰 시 BGM_Boss로 즉시 전환
4. 동일 BGM 재호출 시 처음부터 다시 재생되지 않음
5. SettingsService 볼륨 변경 → `AudioManager.ApplyVolumes()` 호출 시 즉시 반영

## 변경 이력
- 2026-05-11: 초안 작성. BGM 5트랙 매핑, 플레이리스트 기능 추가, Stage3=BossFinal 전용 정책 확정 (김가연 기획).

## 미수신 / 향후 추가 예정
- 레벨업 SFX (`SfxEvent.LevelUp`) — 김가연 "다시 찾을 거에요"
- 카드/선택지 SFX (`SfxEvent.CardAppear/CardSelect`) — 동상
- 공격/피격/사망/픽업 등 27개 SFX 이벤트 — 미수신
- `GameOver` / `GameClear` BGM — 미수신
