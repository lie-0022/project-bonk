# 인트로 컷신 시스템 (Intro Cutscene)

> **System: 인트로 컷신 (신규)** | Category: UI/연출 | Status: Implemented
> 관련 코드: `IntroCutscene.cs`
> 작성: 2026-06-09 (코드 전수 기반)

---

## 1. Overview

인트로 컷신 시스템은 첫 스테이지(맵0) 진입 시 **검은 화면 위에서 타자기 효과로 3개의 텍스트 컷을 순차 출력**하는 UI/연출 시스템이다. `IntroCutscene.cs`가 씬 로드 즉시 `Time.timeScale=0`으로 게임을 정지한 뒤, unscaled 시간 기준으로 글자를 한 자씩 출력한다. 플레이어는 좌클릭·스페이스·엔터로 타이핑을 즉시 완성하거나 다음 컷으로 진행하고, ESC로 전체 스킵할 수 있다. 3컷(스토리 1, 스토리 2, 타이틀 "—— 검나쎄짐 ——") 출력 후 CanvasGroup 페이드아웃과 함께 `Time.timeScale=1`로 복귀해 게임이 시작된다. 맵0 외 스테이지 진입 시에는 즉시 비활성화되어 재생하지 않는다.

핵심 구성요소 요약:

| 요소 | 역할 |
|---|---|
| `_lines[]` | 컷(문단) 목록. 3개 텍스트, 인스펙터에서 자유롭게 편집 |
| `_charInterval` | 글자 출력 간격(초, unscaled). 타이핑 속도 제어 |
| `_startDelay` | 씬 로드 직후 워밍업 대기(초). 첫 프레임 delta 튐 방지 |
| `_fadeOut` | 종료 페이드아웃 시간(초) |
| `_onlyOnMapIndex` | 컷신을 재생할 맵 인덱스(기본 0=첫 스테이지) |
| `CanvasGroup (_group)` | 검은 화면+텍스트 루트. 페이드 및 레이캐스트 차단 |
| `TMP_Text (_label)` | 타자기 효과 텍스트 출력 대상 |

---

## 2. Player Fantasy

**"검이 녹슬어가던 시대" — 첫 검격 전, 세계를 한 줄씩 들여다보는 순간.**

플레이어가 처음 게임을 켜는 순간은 게임이 자신을 소개하는 단 한 번의 기회다. 화면 전체를 채우는 검은 배경 위에서 글자가 한 자씩 박혀나오는 타자기 연출은 "이 세계에 무언가가 쓰여지고 있다"는 감각을 준다. 기다림 없이 클릭으로 빠르게 넘길 수도 있고, 천천히 타이핑을 지켜볼 수도 있다 — 연출의 템포를 플레이어가 쥔다.

마지막 컷 "—— 검나쎄짐 ——" 이 출력되는 순간, 타이틀이 고지(告知)가 아니라 **선언**처럼 느껴져야 한다. 이후 페이드아웃과 함께 세계가 열리면서 플레이어는 이미 이야기의 안쪽에 있다.

컷신은 맵0에서 단 한 번만 보인다. 재도전·2스테이지 이후 진입 시에는 즉시 건너뛰어 리듬을 방해하지 않는다.

---

## 3. Detailed Rules

### 3.1 재생 조건
- `IntroCutscene.Start()`에서 `GameSession.SelectedMapIndex != _onlyOnMapIndex`이면 `ReleaseImmediate()`를 호출하고 즉시 종료한다. CanvasGroup alpha=0, blocksRaycasts=false, gameObject 비활성화.
- 맵0 진입 시에만 코루틴 `Play()`를 시작한다.

### 3.2 타임스케일 제어
- `Play()` 시작 즉시 `Time.timeScale = 0f`. 게임 로직은 정지.
- 이후 모든 시간 측정은 `Time.unscaledDeltaTime` 사용.
- 컷신 종료(`Release()`) 완료 후 `Time.timeScale = 1f` 복구.

### 3.3 워밍업 단계 (_startDelay)
- 타이핑 시작 전, 최소 3프레임 경과 **AND** unscaled 누적 시간이 `_startDelay` 이상이 될 때까지 대기한다.
- 목적: 씬 로드 직후 첫 몇 프레임의 큰 `unscaledDeltaTime`(GC, 에셋 로드 등)을 흘려보내 글자 폭주를 방지.

### 3.4 타이핑 단계 (컷별)
- 각 컷마다 `_label.text`를 `""`로 초기화 후 1글자씩 출력.
- 매 프레임 `charTimer += Mathf.Min(unscaledDeltaTime, _charInterval * 2f)` 누적.
- `charTimer >= _charInterval`일 때 글자 1개 추가, `charTimer -= _charInterval`.
- delta 상한(`_charInterval * 2f`)으로 프레임 spike 시 글자 폭주를 클램핑.

### 3.5 클릭 동작 분기
타이핑 중(`shown < full.Length`)과 완성 후 대기 상태는 같은 입력 키를 사용하지만 동작이 다르다:

| 상태 | 입력 | 동작 |
|---|---|---|
| 타이핑 진행 중 | 좌클릭 / 스페이스 / 엔터 | 해당 컷 텍스트를 즉시 전부 표시(완성). 타이핑 루프 탈출 후 `yield return null`로 클릭 프레임 소비 |
| 타이핑 완성 후 대기 | 좌클릭 / 스페이스 / 엔터 | 다음 컷으로 진행. 마지막 컷이면 `Release()` 호출 후 종료. 진행 클릭 프레임 소비 `yield return null` |
| 어느 상태에서든 | ESC | 즉시 `Release()` 코루틴 실행 후 전체 컷신 종료 |

- 클릭 프레임 소비(`yield return null`)는 **완성 처리 직후**와 **다음 컷 진행 직후** 각 1프레임씩 삽입되어 같은 클릭이 연속 두 단계에 걸쳐 이중 동작하는 것을 방지한다.

### 3.6 페이드아웃 및 종료
- `Release()` 코루틴: unscaled 시간으로 `_fadeOut`초 동안 CanvasGroup.alpha를 1→0으로 Lerp.
- 페이드 완료 후: alpha=0, blocksRaycasts=false, gameObject.SetActive(false), Time.timeScale=1f.
- `_fadeOut=0`이면 즉시 alpha=0(Lerp 분모 0 방어 처리 포함).

### 3.7 컷 목록 (기본값)
| 컷 번호 | 텍스트 | 역할 |
|---|---|---|
| Cut 1 | `검(劍)이 녹슬어가던 시대,` | 세계 배경 |
| Cut 2 | `이름 없는 검사가 일어섰다.` | 플레이어 등장 |
| Cut 3 | `—— 검나쎄짐 ——` | 게임 타이틀 선언 |

---

## 4. Formulas

**변수 정의**

| 변수 | 의미 | 기본값 / 범위 |
|---|---|---|
| `I` | `_charInterval` — 글자 1개 출력 간격(초, unscaled) | 0.04 s / 권장 0.02 ~ 0.12 |
| `D` | `_startDelay` — 워밍업 대기 시간(초, unscaled) | 0.35 s / 권장 0.1 ~ 1.0 |
| `F` | `_fadeOut` — 페이드아웃 지속 시간(초, unscaled) | 0.4 s / 권장 0.0 ~ 1.0 |
| `n` | 현재 컷의 글자 수(`full.Length`) | 정수 ≥ 0 |
| `dt` | `Time.unscaledDeltaTime` — 프레임 소요 시간 | > 0 |
| `charTimer` | 글자 출력까지의 누적 시간 버퍼 | 0.0 ~ `I`(루프 내) |
| `warm` | 워밍업 누적 시간 | 0.0 ~ `D` |
| `warmFrames` | 워밍업 프레임 카운터 | 0 ~ 3+ |

**F1 — 워밍업 종료 조건**
```
종료 = (warmFrames ≥ 3) AND (warm ≥ D)
warm += unscaledDeltaTime  (매 프레임)
warmFrames += 1            (매 프레임)
```
- 예: 첫 2프레임이 dt=0.15s씩 흘러도 `warmFrames<3`이므로 대기 지속. 3프레임 후 `warm=0.30s < 0.35s`이면 추가 프레임 대기. 4프레임째 `warm=0.35s` 조건 충족 → 타이핑 시작.

**F2 — 글자 출력 타이머 (프레임별)**
```
charTimer += Mathf.Min(dt, I × 2)
if charTimer ≥ I:
    charTimer -= I
    shown += 1
```
- delta 상한: `I × 2 = 0.08s`. 프레임 spike(예: dt=0.5s)가 와도 한 프레임에 글자가 1개만 추가됨.
- 예(정상): I=0.04s, dt=0.016s(60fps) → charTimer는 0.016씩 누적, 약 2.5프레임마다 1글자 출력.
- 예(spike): I=0.04s, dt=0.5s → clamp → charTimer+=0.08s. 1개 출력 후 charTimer=0.04s → 다음 프레임에 연속 1개. 최대 연속 2개.

**F3 — 컷 1개 타이핑 소요 시간(정상 60fps, 스킵 없음)**
```
T_cut ≈ n × I
```
- 예: Cut 1 글자수 n=14, I=0.04s → T_cut ≈ 0.56s.
- 예: Cut 3 n=10(`—— 검나쎄짐 ——`의 실제 문자 수), I=0.04s → T_cut ≈ 0.40s.

**F4 — 전체 컷신 최대 소요 시간(스킵 없음, 각 컷 완성 후 클릭 없이 대기 0)**
```
T_total ≥ D + Σ(T_cut_i) + F
```
- 워밍업 0.35s + 3컷 합산(실측 ~1.0s) + 페이드 0.4s ≈ 1.75s(하한). 플레이어 클릭 없을 경우 다음 컷 대기 무한으로 열려 있음.

**F5 — 페이드아웃 alpha (매 프레임)**
```
alpha = Lerp(1, 0, e / F)   (F > 0)
alpha = 0                    (F = 0, 즉시)
```
- 예: F=0.4s, e=0.2s → alpha=0.5.

---

## 5. Edge Cases

| 상황 | 처리 |
|---|---|
| **맵0 외 스테이지 진입** | `Start()`에서 즉시 `ReleaseImmediate()`. CanvasGroup alpha=0, blocksRaycasts=false, gameObject 비활성화. `Time.timeScale` 변경 없음(1.0 유지). |
| **타이핑 중 클릭** | 해당 컷 즉시 완성(`shown=full.Length`, while 루프 break). 다음 프레임(`yield return null`) 소비 후 다음 컷 대기 상태로 진입. 이 프레임 소비가 없으면 같은 클릭으로 "완성 + 다음 컷 진행"이 연속 실행됨. |
| **완성 직후 같은 프레임 클릭** | 완성 처리 후 `yield return null`로 1프레임 소비. 해당 클릭 입력(`wasPressedThisFrame`)은 다음 프레임에서 false → 이중 동작 방지. |
| **다음 컷 진행 직후 클릭** | 진행 처리 후 `yield return null`로 1프레임 소비. 연속 클릭이 다음 컷의 타이핑 루프에 즉시 진입하지 않음. |
| **ESC — 전체 스킵** | 타이핑 중·완성 대기 중 어느 상태에서든 즉시 `Release()` 코루틴 시작 후 `yield break`. 페이드아웃 후 `Time.timeScale=1f`. |
| **씬 로드 직후 첫 프레임 큰 delta** | 워밍업 루프(F1)가 최소 3프레임 + 0.35s를 강제 대기. 이 구간의 큰 `unscaledDeltaTime`은 `warm`에 누적될 뿐 글자 출력에 영향 없음. |
| **`_group`이 null인 상태** | 페이드/레이캐스트 처리 전체를 건너뜀. `_label`만 동작하며 타임스케일 제어는 유지됨. |
| **`_label`이 null인 상태** | 텍스트 설정 줄마다 null 체크로 건너뜀. 타이밍 루프는 정상 진행. 화면에 텍스트가 표시되지 않을 뿐 로직은 완전 실행. |
| **`_lines`가 빈 배열** | `foreach` 루프 즉시 종료 → `Release()` 바로 호출. 컷신 없이 게임 즉시 시작. |
| **폰트 미지원 글리프(tofu)** | TextMesh Pro(Pretendard)는 박스드로잉 문자 U+2500(`─`) 글리프 미포함. Cut 3 구분선을 U+2500으로 설정 시 흰 사각형(두부) 출력. **수정 적용**: em dash U+2014(`—`) 2개 조합 `——`으로 교체. Pretendard 폰트 atlas에 포함되어 정상 렌더링 확인. |
| **`_fadeOut = 0`** | `Release()`에서 Lerp 분모 0 방어: `_fadeOut > 0f ? e / _fadeOut : 1f`. alpha 즉시 0으로 설정됨. |
| **코루틴 실행 중 씬 파괴** | MonoBehaviour 소멸 시 코루틴 자동 중단. `Time.timeScale`이 0인 채로 남을 위험이 있으나, 씬 전환 시 `GameManager`가 timeScale을 관리하는 흐름에서는 씬 리로드로 초기화. |

---

## 6. Dependencies

**이 시스템이 의존하는 것 (upstream)**

| 시스템 | 용도 |
|---|---|
| `GameSession.SelectedMapIndex` (static) | 컷신 재생 여부 판단. 맵0 이외이면 즉시 비활성화 |
| TextMesh Pro (`TMP_Text`) | 타자기 텍스트 출력. `_label.text` Substring 업데이트 |
| Unity Input System (`Mouse.current`, `Keyboard.current`) | 좌클릭/스페이스/엔터(진행), ESC(스킵) 감지 |
| `CanvasGroup` | 검은 화면 페이드 및 레이캐스트 차단 |
| `Time.timeScale` / `Time.unscaledDeltaTime` | 게임 정지 + 연출 타이밍 제어 |

**이 시스템에 의존하는 것 (downstream)**

| 시스템 | 관계 |
|---|---|
| 스테이지 진행(`DebugStageSelector`, 맵0 로드 흐름) | 컷신이 `Time.timeScale=0`으로 게임 정지 → 컷신 종료 후 timeScale=1로 복귀 시 게임 로직 실행 시작. 컷신 비활성화 시에는 즉시 실행. |
| 게임 시작 연출 전반 | 맵0 첫 인상을 담당. 스폰·웨이브·카메라 등은 timeScale=0 동안 정지 상태 |

**양방향 문서 갱신 필요 (design-docs 규칙)**
- `stage-progression.md` → "맵0 진입 시 인트로 컷신(`IntroCutscene.cs`)이 timeScale=0으로 게임 정지 후 재생" 언급 추가 권장
- `game-state-manager.md`(작성 시) → timeScale 관리 경계에서 IntroCutscene과의 관계 명시 권장

---

## 7. Tuning Knobs

| 노브 | 필드명 | 기본값 | 안전 범위 | 영향 |
|---|---|---|---|---|
| 글자 출력 속도 | `_charInterval` | 0.04 s | 0.02 ~ 0.12 s | 타자기 체감 속도. ↓ = 빠른 타다닥 / ↑ = 느린 또각또각. 0.02 미만은 60fps에서 1-2프레임마다 다수 글자 출력 → 타이핑 감 소실 |
| 워밍업 대기 | `_startDelay` | 0.35 s | 0.1 ~ 1.0 s | 씬 로드 직후 첫 프레임 spike 흡수. ↓ = 폭주 위험(낮은 사양 기기). 0.35s는 GC+로드 완충을 위해 측정된 값. 0.1 미만은 저사양에서 글자 폭주 가능 |
| 페이드아웃 시간 | `_fadeOut` | 0.4 s | 0.0 ~ 1.0 s | 컷신 종료의 부드러움. ↑ = 더 부드럽고 긴 전환 / 0.0 = 즉시 컷. 1.0 초과 시 게임 시작이 느리다는 인상을 줄 수 있음 |
| 컷 텍스트 목록 | `_lines[]` | 3개 | 1 ~ 10개 권장 | 서사량과 연출 길이. 컷 수 ↑ = 총 소요 시간 증가. 인스펙터 TextArea에서 자유 편집 가능 |
| 재생 맵 인덱스 | `_onlyOnMapIndex` | 0 | 0 ~ 스테이지 수-1 | 컷신이 재생될 스테이지. 기본 0(맵0=첫 스테이지). 다른 스테이지 인트로 추가 시 인스턴스 분리 권장 |
| delta 클램프 배수 | 코드 내 상수 `_charInterval * 2f` | I×2 | I×1.5 ~ I×3 | 프레임 spike 시 한 프레임 최대 출력 글자 수 제한. 1.0(=I)이면 spike 완전 억제 / 3.0이면 spike 흡수 3글자. 현재 2f(2글자)는 60fps 정상 흐름과 spike 흡수의 균형 |

비-노브(고정 설계): 진행 입력(좌클릭/스페이스/엔터), ESC 스킵 키, 프레임 소비 방식(`wasPressedThisFrame` + 1프레임 yield), timeScale=0 연출 방식, unscaled 시간 사용.

---

## 8. Acceptance Criteria

각 항목은 QA가 합/불 판정 가능해야 한다.

**재생 조건**
- [ ] 진행도 초기화 상태에서 맵0을 선택해 진입하면 검은 화면과 함께 타자기 텍스트가 출력되기 시작한다.
- [ ] 맵1(Stage2) 또는 맵2(Stage3)를 선택해 진입 시 컷신 없이 즉시 게임이 시작된다(검은 화면 미노출, timeScale 정상 1.0).

**타이핑 및 클릭 분기**
- [ ] 타이핑 진행 중 좌클릭 시 해당 컷의 전체 텍스트가 즉시 표시되고, 그 클릭으로 다음 컷으로 진행되지 않는다(1프레임 지연 후 다음 클릭 대기).
- [ ] 타이핑 완성 후 좌클릭 시 다음 컷 타이핑이 시작된다(Cut 3이면 페이드아웃 및 게임 시작).
- [ ] 스페이스/엔터 키가 좌클릭과 동일하게 동작한다.
- [ ] ESC 키 입력 시 타이핑·완성 대기 어느 상태에서든 즉시 페이드아웃 후 게임이 시작된다.

**워밍업 및 폭주 방지**
- [ ] 씬 로드 직후 첫 번째 텍스트 출력 시 글자가 한꺼번에 나타나지 않고 타자기 효과로 순차 출력된다(워밍업 동작 확인).
- [ ] 프레임 드롭(일시 스파이크) 발생 시 한 프레임에 3글자 이상 동시 출력되지 않는다.

**컷 순서 및 내용**
- [ ] Cut 1 `검(劍)이 녹슬어가던 시대,` → Cut 2 `이름 없는 검사가 일어섰다.` → Cut 3 `—— 검나쎄짐 ——` 순서로 출력된다.
- [ ] Cut 3 구분선이 흰 사각형(tofu)이 아닌 `——` 대시로 정상 렌더링된다.

**타임스케일 및 종료**
- [ ] 컷신 재생 중 게임 내 적·플레이어·타이머 등이 정지 상태다(`Time.timeScale=0` 확인).
- [ ] 컷신 종료 후(페이드아웃 완료 후) 게임 오브젝트가 정상 동작한다(`Time.timeScale=1.0`).
- [ ] 컷신 종료 후 CanvasGroup이 비활성화되어 HUD 등 이후 UI에 레이캐스트 간섭이 없다.

---

> **문서 완료** (8/8 섹션). 코드 전수 기반 검증. 후속: `stage-progression.md`에 맵0 인트로 컷신 언급 추가 권장(양방향 참조).
