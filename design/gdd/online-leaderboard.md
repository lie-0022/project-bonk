# 온라인 리더보드 시스템 (Online Leaderboard)

> **System: 온라인 리더보드 (신규)** | Category: Infra/Meta | Status: Implemented
> 관련 코드: `OnlineLeaderboard.cs`, `LeaderboardPanelUI.cs`
> 작성: 2026-06-09 (코드 전수 기반)

---

## 1. Overview

온라인 리더보드 시스템은 플레이어가 런(run) 종료 후 **닉네임과 최종 점수를 전 세계 공개 목록에 기록**하고, 상위 플레이어 목록을 조회할 수 있게 하는 Infra/Meta 시스템이다. 점수 저장소로 **Firebase Realtime Database REST API**를 사용하며, 추가 패키지를 전혀 설치하지 않고 Unity 기본 내장 `UnityWebRequest`만으로 구현한다.

**UGS(Unity Gaming Services) 미채택 배경 — ADR:** 초기 구현에서 `com.unity.services.authentication` 및 `com.unity.services.leaderboards` 패키지를 도입하려 했으나, UnityMCP의 `execute_code` 방식으로 패키지를 설치할 때 컴파일러 명령줄이 과도하게 길어지며 실행이 깨지는 문제가 발생했다. 이를 해결하는 대신 **외부 패키지 의존 없이 REST 한 줄로 해결 가능한 Firebase RTDB**를 채택했다. Firebase는 별도 SDK 없이 HTTP `POST`/`GET` 요청만으로 데이터를 읽고 쓸 수 있어 런타임 바이너리 의존이 0이다.

치팅 가능성(클라이언트 직접 전송, 공개 쓰기 규칙)은 캐주얼 인디 타이틀 기준으로 감수한다. 보안 강화가 필요하면 Firebase Security Rules 또는 서버사이드 검증으로 점진 도입 가능하다.

핵심 구성요소 요약:

| 구성요소 | 역할 |
|---|---|
| `OnlineLeaderboard` (MonoBehaviour, 싱글턴) | REST 통신 전담. 점수 등록(`SubmitScore`) / 상위 조회(`FetchTop`) / 파싱(`ParseEntries`) / 정제(`Sanitize`) |
| `LeaderboardPanelUI` | 닉네임 입력 → 제출 버튼 → 상태 표시 → 순위 목록 렌더링. GameClear 화면에 배치 |
| Firebase RTDB (외부) | 영속 저장소. `/{node}.json` 엔드포인트를 통해 각 엔트리를 push-key UUID로 저장 |

## 2. Player Fantasy

**"이 판은 진짜 잘 됐는데 — 내 이름을 남기고 싶다."**

게임 클리어 직후 플레이어는 자신의 점수가 얼마나 대단한지 확인하고 싶어한다. 세 글자 닉네임을 입력하고 "등록" 버튼을 누르는 행위는 **아케이드 하이스코어 문화**의 감각을 재현한다. 목록이 로드되어 자신의 이름이 상위권에 보이는 순간은 "내가 잘 했다"는 사실을 외부에 각인시키는 성취감을 준다.

세 글자 제한은 제약이 아니라 **레트로 감성**이다. 짧은 닉네임을 고르는 과정 자체가 작은 의식이며, 다음 도전에서 "더 위에 올라가자"는 동기를 남긴다. 순위 목록은 타인의 기록을 구경하는 관전 재미도 겸한다.

오프라인이거나 DB 미연결일 때는 패널이 조용히 "(DB 미연결)" 상태를 표시하고, 핵심 게임 흐름을 방해하지 않는다. 리더보드는 **덤**이지 게임 진행의 전제조건이 아니다.

## 3. Detailed Rules

### 3.1 점수 등록 흐름

1. GameClear 화면에서 `LeaderboardPanelUI`가 활성화(`OnEnable`)될 때 자동으로 `Refresh()` 호출 → 현재 상위 목록 표시.
2. 플레이어가 닉네임 입력 필드(`_nicknameInput`)에 최대 3글자를 입력한다. 이전 런에서 사용한 닉네임이 `PlayerPrefs("BladeSurge.Nickname")`에서 자동 복원된다.
3. "등록" 버튼(`_submitButton`)을 누르면 `OnSubmit()` 실행:
   - `_submitted == true`이면 "이미 등록됨" 표시 후 중단(런당 1회 제한).
   - `OnlineLeaderboard.IsConfigured == false`이면 "온라인 랭킹 미설정" 표시 후 중단.
   - 닉네임을 `PlayerPrefs`에 저장(`PlayerPrefs.Save()`).
   - `_submitButton.interactable = false` (중복 제출 방지).
   - `OnlineLeaderboard.SubmitScore(nick, RunTotals.Score, callback)` 호출.
4. 콜백:
   - 성공(`ok=true`): `_submitted=true`, 상태 텍스트 "등록 완료!", `Refresh()` 호출.
   - 실패(`ok=false`): 상태 텍스트 "등록 실패 (네트워크 확인)", 버튼 다시 활성화.

### 3.2 REST 통신 규칙

**점수 등록 (POST push)**

- 엔드포인트: `{_databaseUrl}/{_node}.json`
  - 실 URL: `https://ssazim-default-rtdb.firebaseio.com/leaderboard.json`
- HTTP 메서드: `POST`
- 본문: `{"nickname":"닉","score":12345}` (JSON UTF-8)
- 헤더: `Content-Type: application/json`
- 타임아웃: `_timeout` 초 (기본 10)
- 성공 응답: HTTP 200, 본문에 Firebase push-key JSON `{"name":"-Nxxx..."}`. 이 키는 UI가 사용하지 않음(무시).
- 실패: HTTP 4xx/5xx 또는 네트워크 오류 → 콜백 `false`.

**상위 조회 (GET 전체 + 클라이언트 정렬)**

- 엔드포인트: `{_databaseUrl}/{_node}.json`
- HTTP 메서드: `GET`
- 타임아웃: `_timeout` 초
- 응답 형식: `{"pushKey1":{"nickname":"A","score":100},"pushKey2":{"nickname":"B","score":200},...}` (Firebase RTDB 맵 형식)
- 클라이언트 처리:
  1. `ParseEntries(json)` — 정규식 `\{[^{}]*\}` 로 내부 객체(flat entry) 추출 → `JsonUtility.FromJson<Entry>` 역직렬화.
  2. `List.Sort((a,b) => b.score.CompareTo(a.score))` — 점수 내림차순 정렬.
  3. `count > 0` 이면 `GetRange(0, count)` 잘라 상위 N개만 반환.
- 빈 DB 응답: `"null"` 문자열 → `ParseEntries`가 빈 리스트 반환 → UI "아직 기록 없음" 표시.

### 3.3 닉네임 정제 규칙 (Sanitize)

입력값은 항상 `Sanitize()`를 거쳐 등록된다.

- 공백만 있거나 null/empty → `"익명"` 반환.
- `"`, `\`, `\n`, `\r` 문자 제거 (JSON 인젝션 방지).
- `Trim()` 적용(양쪽 공백 제거).
- 길이 3 초과 시 앞 3글자로 잘라냄 (`Substring(0, 3)`).
- 정제 후 빈 문자열 → `"익명"` 반환.

UI(`LeaderboardPanelUI`)는 정제 전 닉네임을 `PlayerPrefs`에 저장(입력 원문 유지). 실제 DB에는 정제된 닉네임이 기록된다.

### 3.4 표시 포맷

- 상위 `_topCount`개(기본 10) 표시.
- 각 행 포맷: `{순위}.   {닉네임}      {점수:N0}` (예: `1.   김갑   12,345`)
- 순위는 1부터 시작하는 1-based 표시.

### 3.5 싱글턴 생명주기

- `OnlineLeaderboard`는 `DontDestroyOnLoad`로 씬 전환에도 유지.
- 씬에 중복 인스턴스 존재 시 나중 인스턴스가 `Destroy(gameObject)`로 자기 파괴.
- `LeaderboardPanelUI`는 `Awake`/`OnEnable`에서 `OnlineLeaderboard.Instance`를 폴링해 늦은 의존성 해결.

## 4. Formulas

**변수 정의**

| 변수 | 의미 | 범위 |
|---|---|---|
| `S` | 런 최종 점수 (`RunTotals.Score`) | 정수 ≥ 0 |
| `N_all` | DB에서 조회된 전체 엔트리 수 | ≥ 0 |
| `N_top` | 표시할 상위 개수 (`_topCount`) | 1 ~ N_all (기본 10) |
| `L` | 닉네임 원문 길이 (정제 전) | ≥ 0 |
| `T` | 타임아웃 초 (`_timeout`) | 권장 5 ~ 30 (기본 10) |

**F1 — 닉네임 길이 제한**

```
nick_stored = Sanitize(nick_raw)

if isNullOrWhitespace(nick_raw):
    nick_stored = "익명"
else:
    nick_clean = remove(nick_raw, '"', '\', '\n', '\r').Trim()
    if len(nick_clean) == 0:
        nick_stored = "익명"
    else:
        nick_stored = nick_clean[0 .. min(3, len(nick_clean))]
```

예시:
- 입력 `"홍길동전"` → 정제 후 `"홍길동"` (4자 → 앞 3자)
- 입력 `"A\"B"` → 따옴표 제거 → `"AB"` (2자, 그대로)
- 입력 `"   "` → Trim() → `""` → `"익명"`

**F2 — 상위 N 슬라이스**

```
sorted = sort_desc(entries, by=score)     // b.score - a.score 기준
result = sorted[0 .. min(N_top, N_all)]  // 0-based exclusive 상한
```

예시: N_all=25, N_top=10 → result는 index 0~9 (10개). N_all=3, N_top=10 → result는 index 0~2 (3개).

**F3 — ParseEntries 정규식 매칭**

```
pattern = \{[^{}]*\}
matches = Regex.Matches(json, pattern)
for each m in matches:
    entry = JsonUtility.FromJson<Entry>(m.Value)
    if entry.nickname != null && entry.nickname != "":
        list.add(entry)
```

이 패턴은 Firebase RTDB가 반환하는 `{"pushKey": {"nickname":"X","score":N}, ...}` 맵에서 바깥 객체(중괄호 안에 중괄호)를 제외하고 leaf-level flat 객체만 추출한다. 중첩 JSON(score 필드가 객체인 경우 등)이 있으면 `[^{}]*`가 매칭하지 않아 자동 스킵.

**F4 — 요청 타임아웃 경계**

```
request.timeout = T   // UnityWebRequest.timeout (초, 정수)
실패 조건: T초 경과 OR HTTP 4xx/5xx OR 연결 불가
```

예시: T=10, 서버 응답 없음 → 10초 후 `req.result != Success` → 콜백 `null`/`false`.

## 5. Edge Cases

| 상황 | 처리 |
|---|---|
| **`_databaseUrl` 미설정 또는 "http"로 시작하지 않음** | `IsConfigured == false`. `SubmitScore`/`FetchTop` 모두 즉시 콜백(`false`/`null`) 후 종료. UI는 "온라인 랭킹 미설정"/"(DB 미연결)" 표시. 게임 흐름 차단 없음. |
| **오프라인(네트워크 없음)** | `UnityWebRequest.result == ConnectionError`. `SubmitScore`→ 콜백 `false`. `FetchTop`→ 콜백 `null`. UI "등록 실패 (네트워크 확인)" / "불러오기 실패". 버튼 재활성화되어 재시도 가능. |
| **타임아웃 초과** | T초 후 `req.result != Success`. SubmitScore와 FetchTop 모두 실패 경로와 동일 처리. |
| **Firebase 응답 본문 `"null"` (DB 비어 있음)** | `ParseEntries`에서 조기 빈 리스트 반환. UI "아직 기록 없음 — 첫 주자가 되어보세요!" 표시. |
| **Firebase 응답 HTTP 401/403 (보안 규칙 차단)** | `req.result != Success`. 실패 콜백. 콘솔에 `[OnlineLeaderboard] 조회 실패:` 경고. DB Security Rules 확인 필요. |
| **HTTP 200이지만 본문이 기형 JSON** | `ParseEntries` 정규식이 매칭 실패 또는 `JsonUtility.FromJson` 예외 → catch로 무시 → 빈 리스트 반환. UI "아직 기록 없음" 표시(오류 없음). |
| **런당 중복 제출** | `_submitted == true` 검사로 차단. "이미 등록됨" 표시. 버튼 비활성 유지는 없으므로 재등록 시도 버튼은 눌리지만 응답 없음. |
| **닉네임 입력 없이 제출** | `nick = ""` → `Sanitize("")` → `"익명"` 으로 등록. |
| **중복 닉네임 (다른 플레이어가 동일 닉네임 사용)** | Firebase는 push-key(UUID)로 구분하므로 서버 측 중복 없음. 표시 목록에는 동일 닉네임이 여러 행으로 나타날 수 있음 — 캐주얼 허용. |
| **닉네임에 JSON 특수문자 포함 (`"`, `\`)** | `Sanitize`에서 제거. 예: 입력 `"A\"B"` → 저장 `"AB"`. JSON 인젝션 차단. |
| **`OnlineLeaderboard.Instance == null` (LeaderboardPanelUI 시작 시)** | `OnEnable`에서 재폴링 후 null이면 "온라인 랭킹 미설정" 표시. 이후 동작 생략. |
| **씬 전환 중 HTTP 응답 도착** | `OnlineLeaderboard`는 `DontDestroyOnLoad`이므로 코루틴 유지. `LeaderboardPanelUI`가 이미 파괴된 경우 `SetStatus`/`SetList`의 null 체크로 NRE 방지. |
| **치팅 (점수 조작 POST)** | Firebase Security Rules가 공개 쓰기(`".write": true`)이므로 임의 점수 등록 가능. 캐주얼 타이틀 기준 허용. 방어 불필요. |
| **점수 0 등록** | 정상 처리. `RunTotals.Score == 0` 이어도 등록 차단 없음. |

## 6. Dependencies

**이 시스템이 의존하는 것 (upstream)**

| 시스템 | 용도 |
|---|---|
| `RunTotals` (정적 클래스) | `RunTotals.Score` — 등록할 런 최종 점수 소스. 점수 산출 규칙은 `ranking-scoring.md` 소관 |
| `UnityEngine.Networking.UnityWebRequest` | HTTP POST/GET 전송. Unity 내장, 추가 패키지 불필요 |
| Firebase Realtime Database (외부 서비스) | 영속 저장소. URL: `https://ssazim-default-rtdb.firebaseio.com` |
| `PlayerPrefs` | 닉네임 로컬 기억 (`"BladeSurge.Nickname"` 키) |
| TextMeshPro (`TMP_InputField`, `TMP_Text`) | UI 텍스트 렌더링 |
| `UnityEngine.UI.Button` | 제출 버튼 |

**이 시스템에 의존하는 것 (downstream)**

| 시스템 | 관계 |
|---|---|
| GameClear UI (`GameClearUI` 또는 해당 씬 패널) | `LeaderboardPanelUI`를 자식으로 보유. 클리어 화면 진입 시 자동 표시 |
| 게임 상태 관리 (`GameManager`) | Win 상태 전환이 GameClear 화면을 활성화해 `LeaderboardPanelUI.OnEnable` 트리거 |

**양방향 문서 갱신 필요 (design-docs 규칙)**

- `ranking-scoring.md` → "OnlineLeaderboard가 `RunTotals.Score`를 읽어 리더보드에 등록" 언급 추가 필요
- `game-state-manager.md` → "Win 전환 시 GameClear 화면에서 LeaderboardPanelUI 활성화" 언급 추가 권장
- `stage-progression.md` → 의존 없음(단방향). 단, StageGate → Win 전환 → LeaderboardPanelUI 간접 연계는 이미 stage-progression.md §6에서 GameManager 언급으로 커버됨

## 7. Tuning Knobs

| 노브 | 위치 | 안전 범위 | 영향 |
|---|---|---|---|
| `_databaseUrl` | `OnlineLeaderboard` Inspector | 유효한 Firebase RTDB URL | DB 교체(스테이징 ↔ 프로덕션). 잘못된 값 → `IsConfigured=false`로 즉시 안전 실패 |
| `_node` | `OnlineLeaderboard` Inspector | 영숫자 + 하이픈 문자열 (기본 `"leaderboard"`) | DB 내 노드 경로. 변경 시 기존 데이터와 분리됨 |
| `_timeout` | `OnlineLeaderboard` Inspector | 5 ~ 30 (기본 10, 단위: 초) | ↓ = 저사양/느린 네트워크에서 조기 실패 증가. ↑ = UI 응답성 저하 |
| `_topCount` | `LeaderboardPanelUI` Inspector | 3 ~ 50 (기본 10) | 표시 행 수. ↑ = GET 응답 파싱 부하 소폭 증가(전체 조회 후 잘라냄) |
| 닉네임 최대 길이 | `OnlineLeaderboard.Sanitize()` 코드 내 (`> 3`, `Substring(0,3)`) | 현재 3 고정 (코드 변경 필요) | ↑ = 표시 공간 필요. DB 엔트리 크기 소폭 증가. 레트로 감성 감소 |
| Firebase Security Rules | Firebase Console (외부) | 쓰기 공개/제한, 읽기 공개/제한 | 공개 쓰기 → 치팅 가능. 제한 쓰기 → 서버사이드 토큰 검증 필요(현재 미구현) |
| DB 엔트리 보존 정책 | Firebase Console (TTL 규칙 또는 Cloud Functions) | 제한 없음 (현재 미설정) | 시간 경과에 따라 수천 엔트리 누적 → GET 응답 크기 증가, 파싱 시간 미세 증가 |

비-노브(고정 설계): push-key UUID 저장 방식(순서 없음), 클라이언트 정렬(서버 정렬 쿼리 미사용), 런당 1회 제출 제한(`_submitted` 플래그).

## 8. Acceptance Criteria

각 항목은 QA가 합/불 판정 가능해야 한다.

**기본 등록/조회**

- [ ] GameClear 화면 진입 시 `LeaderboardPanelUI`가 활성화되어 "불러오는 중..." 후 상위 N개 목록 또는 "아직 기록 없음"이 표시된다.
- [ ] 닉네임 3글자 입력 후 "등록" 버튼을 누르면 상태 텍스트가 "등록 중..." → "등록 완료!"로 바뀌고 목록이 갱신된다.
- [ ] 등록 완료 후 동일 화면에서 "등록" 버튼을 다시 누르면 "이미 등록됨"이 표시되고 재요청이 발생하지 않는다.
- [ ] Firebase Console에서 `leaderboard` 노드를 확인했을 때 등록한 닉네임과 점수가 저장되어 있다.
- [ ] 표시 목록은 점수 내림차순으로 정렬되어 있다(최고 점수가 1행).

**닉네임 규칙**

- [ ] 4글자 이상 입력 시 실제 DB에는 앞 3글자만 저장된다(UI 입력 필드 원문은 유지).
- [ ] 닉네임에 `"` 또는 `\` 입력 시 해당 문자가 제거된 채로 등록된다.
- [ ] 닉네임 입력 없이(또는 공백만) 등록 시 DB에 `"익명"`으로 저장된다.
- [ ] 이전 런에서 사용한 닉네임이 다음 GameClear 화면에서 입력 필드에 자동 복원된다.

**오프라인/미설정**

- [ ] `_databaseUrl`가 비어 있는 상태에서 GameClear 화면 진입 시 "온라인 랭킹 미설정"과 "(DB 미연결)"이 표시되며 게임 흐름이 중단되지 않는다.
- [ ] 네트워크를 끊은 상태에서 "등록" 버튼을 누르면 `_timeout`초 이내에 "등록 실패 (네트워크 확인)"이 표시되고 버튼이 재활성화된다.
- [ ] 오프라인 중 GameClear 화면이 정상 렌더링되며 "등록" 버튼 외 다른 UI 요소는 정상 동작한다.

**씬 전환 / 싱글턴**

- [ ] 메인 메뉴 → GamePlay → GameClear 씬 순서를 거쳐도 `OnlineLeaderboard.Instance`가 null이 아니다.
- [ ] 같은 씬에 `OnlineLeaderboard`가 두 개 배치되어도 중복 인스턴스가 즉시 제거되어 하나만 동작한다.

---

> **문서 완료** (8/8 섹션). 코드 전수 기반 검증. 후속: ranking-scoring.md에 `RunTotals.Score → OnlineLeaderboard` 언급 추가, game-state-manager.md에 Win → LeaderboardPanelUI 연계 언급 추가.
