# MCP 서버 출처 추적 보고서 + GUI 비활성화 안내

- **작성일**: 2026-04-26
- **세션**: Claude Code Game Studios 환경 Unity 6.3 정리 (Step 4)
- **목적**: BladeSurge 프로젝트와 무관한 MCP 서버 5개를 비활성화하기 위해 출처를 추적하고, 파일 편집 불가능한 항목은 GUI 처리 경로 안내

---

## 추적 절차 (5경로 전수)

| 경로 | 명령/파일 | 결과 |
|------|----------|------|
| A | `claude mcp list` | "No MCP servers configured" — CLI에는 등록 0건 |
| B | `~/.claude/` 하위 모든 .json (`find`) | `.credentials.json`, `plugins/blocklist.json`, `plugins/known_marketplaces.json`, `sessions/*.json`, `settings.json` — MCP 정의 없음 |
| C | `env \| grep -i claude` | CLAUDE_* 환경변수만 있음 (MCP 정의 없음) |
| D | `~/.claude/plugins/` | `marketplaces/claude-plugins-official/` 존재. MCP 정의 없음 (마켓플레이스 메타데이터만) |
| E | `~/AppData/Roaming/Claude/claude_desktop_config.json` | **여기서 `smat-project` 1건만 발견** |

### 5개 비활성화 대상 grep 결과 (전체 파일 전수)

```text
~/.claude.json:                     0건
~/.claude/settings.json:            0건
~/.claude/plugins/*.json:           0건
~/AppData/Roaming/Claude/*.json:    1건 — claude_desktop_config.json:line 3 ("smat-project")
```

**결론**: 5개 중 4개 (Claude_in_Chrome, mcp-registry, scheduled-tasks, 462a7aa6...) 는 **Claude Desktop 앱 빌트인 connector**로 추정. 텍스트 설정 파일에 정의되지 않고 Desktop 앱 자체 코드에서 등록.

---

## 발견된 출처 상세

### 1. `claude_desktop_config.json` (Windows: `%APPDATA%\Claude\`)

```json
{
  "mcpServers": {
    "smat-project": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-filesystem",
        "C:\\SMAT-Project"
      ]
    }
  },
  "preferences": {
    "allowAllBrowserActions": true,
    "coworkScheduledTasksEnabled": true,
    "ccdScheduledTasksEnabled": true,
    "sidebarMode": "epitaxy",
    "bypassPermissionsModeEnabled": true,
    "coworkWebSearchEnabled": true
  }
}
```

- `smat-project`: 사용자가 정의한 파일시스템 MCP (`@modelcontextprotocol/server-filesystem` → `C:\SMAT-Project`)
- `coworkScheduledTasksEnabled`: scheduled-tasks 기능 토글로 추정
- `ccdScheduledTasksEnabled`: ccd-prefix 기능 (ccd_directory, ccd_session) 관련 추정
- `coworkWebSearchEnabled`: Web Search 기능
- `allowAllBrowserActions`: Claude_in_Chrome 관련 추정

---

## 5개 비활성화 대상 — GUI 처리 안내

### ① smat-project (12 tools, 파일시스템 도구)
- **출처**: `claude_desktop_config.json` 직접 편집 가능
- **GUI 경로**: Claude Desktop 앱 → Settings → Developer → "Edit Config" 버튼 → `mcpServers.smat-project` 객체 제거
- **또는 텍스트 편집기로** `C:\Users\jayju\AppData\Roaming\Claude\claude_desktop_config.json` 직접 수정
- **재시작 필요**: Claude Desktop 종료 후 재시작
- **파급**: 본 프로젝트와 무관하므로 비활성화 권장

### ② Claude_in_Chrome (25 tools, 브라우저 자동화)
- **출처**: Claude Desktop 빌트인 + Chrome 확장 연동 (`ChromeNativeHost/com.anthropic.claude_browser_extension.json`)
- **GUI 경로**: Claude Desktop 앱 → Settings → Connectors → "Claude in Chrome" → Disable
- **또는**: Chrome 브라우저에서 Claude 확장 비활성화/제거
- **파급**: UI 자동화 작업이 필요한 다른 프로젝트에는 영향. 본 게임 프로젝트엔 무관

### ③ mcp-registry (3 tools)
- **출처**: Claude Desktop 빌트인 connector
- **GUI 경로**: Claude Desktop 앱 → Settings → Connectors → "MCP Registry" → Disable (정확한 명칭은 UI에서 확인)
- **파급**: 다른 MCP 검색/추천 기능 비활성화

### ④ scheduled-tasks (3 tools)
- **출처**: Claude Desktop 빌트인 (`coworkScheduledTasksEnabled` preference 관련)
- **GUI 경로 1**: Claude Desktop 앱 → Settings → Cowork/Scheduled Tasks → Disable
- **GUI 경로 2**: `claude_desktop_config.json`의 `preferences.coworkScheduledTasksEnabled: false`로 변경
- **파급**: 예약 작업 기능 비활성화

### ⑤ 462a7aa6-5170-4857-8817-b33293df4043 (8 tools, Google Drive)
- **출처**: Claude Desktop 빌트인 connector (UUID 형식)
- **GUI 경로**: Claude Desktop 앱 → Settings → Connectors → "Google Drive" 또는 해당 통합 → Disconnect/Disable
- **파급**: GDrive 통합 비활성화. OAuth 토큰도 함께 정리됨

---

## 유지 대상 3개 (참고)

| MCP | 도구 수 | 유지 사유 |
|-----|--------|---------|
| Claude_Preview | 13 | UI 스크린샷·미리보기 — 게임 UI 검증 시 유용 |
| ccd_directory | 1 | 디렉토리 접근 (저비용) |
| ccd_session | 2 | 세션 관리 (저비용) |

---

## 권장 처리 순서

1. **smat-project**부터 (가장 명확한 출처, 파일 편집)
   - 백업: `cp ~/AppData/Roaming/Claude/claude_desktop_config.json ~/AppData/Roaming/Claude/claude_desktop_config.json.backup_2026-04-26`
   - `mcpServers.smat-project` 객체 제거
   - JSON 유효성 검증: `python -m json.tool <파일>`
   - Claude Desktop 재시작
2. **나머지 4개**: Claude Desktop GUI에서 Settings → Connectors 페이지 확인 후 토글로 비활성화
3. 비활성화 후 새 Claude Code 세션 시작 → `/status`로 활성 MCP 도구 수 비교

---

## 예상 효과

5개 모두 비활성화 시 약 **51 tools 감소** (25 + 12 + 3 + 3 + 8 = 51) → 세션 베이스라인 토큰 절감 추정 ~3K~5K.

---

## 본 세션에서 변경한 것

**없음.** 추적 보고서만 작성. 실제 비활성화는 사용자가 GUI에서 직접 수행.

## 후속 작업 추천

- Claude Desktop 앱의 Connectors 페이지를 캡처해서 정확한 토글 명칭 확인
- 비활성화 후 효과 측정: `/cost` 또는 `/status` 비교
- 본 보고서 업데이트 (어떤 connector를 어떻게 비활성화했는지 기록)
