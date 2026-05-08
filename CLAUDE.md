# Claude Code Game Studios -- Game Studio Agent Architecture

Indie game development managed through 48 coordinated Claude Code subagents.
Each agent owns a specific domain, enforcing separation of concerns and quality.

## Technology Stack

- **Engine**: Unity 6.3 LTS (6000.3.x)
- **Language**: C#
- **Version Control**: Git with trunk-based development
- **Build System**: Unity Build Pipeline (Build Profiles)
- **Asset Pipeline**: Unity Asset Import Pipeline + Addressables

> **Note**: This project uses **Unity 6.3 LTS** exclusively.
> Unity-specific agents (`unity-csharp`, `unity-architect`, `unity-debugger`) are active.
> Godot agents are disabled in `.claude/agents/_disabled/`. Unreal agents remain
> available but should not be invoked unless the project pivots.

## Project Path

- **Unity Project Root**: `src/BladeSurge/`
- **Unity Version**: 6000.3.10f1 (Unity 6.3)
- **Render Pipeline**: Universal Render Pipeline (URP)

## Project Structure

@.claude/docs/directory-structure.md

## Engine Version Reference

@docs/engine-reference/unity/VERSION.md

## Technical Preferences

@.claude/docs/technical-preferences.md

## Coordination Rules

@.claude/docs/coordination-rules.md

## Collaboration Protocol

**User-driven collaboration, not autonomous execution.**
Every task follows: **Question -> Options -> Decision -> Draft -> Approval**

- Agents MUST ask "May I write this to [filepath]?" before using Write/Edit tools
- Agents MUST show drafts or summaries before requesting approval
- Multi-file changes require explicit approval for the full changeset
- No commits without user instruction

See `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md` for full protocol and examples.

> **First session?** If the project has no engine configured and no game concept,
> run `/start` to begin the guided onboarding flow.

## Unity MCP (Editor 직접 조작)

CoplayDev `unity-mcp` 설치됨 (2026-05-08). `mcp__UnityMCP__*` 도구로 Editor 직접 조작 가능.

**규칙**:
- Scene/GameObject/Component/Prefab/ScriptableObject 작업은 **MCP 도구 우선 사용**
- 핸드오프 문서(`.claude/docs/editor-handoff.md`)는 MCP가 못 하는 것에만:
  - `.fbx` / `.png` / `.wav` 등 외부 에셋 임포트
  - Play 모드 체감 판정 ("이 점프 느낌 어때?")
  - Material/Shader 비주얼 튜닝
  - 빌드 (Build Profile)
- `manage_components` Add 시 **FQN 필수** (네임스페이스 미사용 프로젝트면 클래스명 그대로)
- `execute_code`에서 `using` 지시문 금지 — 메서드 바디로 실행됨, 항상 FQN 사용
- MCP 서버 미연결 상태(`/mcp` 확인)면 Editor에서 `Window > MCP For Unity` → `Start Server` 후 재시도

## Workflow

**B타입 워크플로우 (2026-04-26~)**: 1인 개발자(PM/결정자) + AI(풀스택 개발자) 협업 모델.
Sprint 시스템 폐지, continuous flow.

@.claude/docs/workflow-b-type.md

@.claude/docs/editor-handoff.md

@.claude/docs/review-workflow.md

## Deprecated Concepts (참고 금지)

초기 기획에서 폐기된 컨셉이 코드/문서에 잔재할 수 있다.
새 작업 전 반드시 확인:

@.claude/docs/deprecated-concepts.md

## Naming Conventions

@.claude/docs/naming-conventions.md

## Coding Standards

@.claude/docs/coding-standards.md

## Context Management

@.claude/docs/context-management.md
