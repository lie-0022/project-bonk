# Technical Preferences

<!-- Populated by /setup-engine. Updated as the user makes decisions throughout development. -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: Unity 6.3 LTS (6000.3.10f1)
- **Language**: C# (.NET Standard 2.1)
- **Rendering**: Universal Render Pipeline (URP) — Render Graph API (Compatibility Mode 사용 금지)
- **Physics**: Unity PhysX 3D
- **Project Path**: `src/BladeSurge/`

## Input & Platform

- **Target Platforms**: PC (Windows 11) — MVP 기준
- **Input Methods**: Mixed (Keyboard/Mouse + Gamepad)
- **Primary Input**: Keyboard/Mouse
- **Gamepad Support**: Partial (점진 도입)
- **Touch Support**: None
- **Platform Notes**: Windows 11 우선. 추후 macOS/Steam Deck 검토

## Naming Conventions

- **Classes**: PascalCase (e.g., `PlayerController`)
- **Public Fields/Properties**: PascalCase (e.g., `MoveSpeed`)
- **Private Fields**: _camelCase (e.g., `_moveSpeed`)
- **Methods**: PascalCase (e.g., `TakeDamage()`)
- **Signals/Events**: On + PascalCase (e.g., `OnPlayerDied`)
- **Files**: PascalCase matching class (e.g., `PlayerController.cs`)
- **Scenes/Prefabs**: PascalCase (e.g., `PlayerCharacter.prefab`)
- **Constants**: PascalCase or UPPER_SNAKE_CASE (e.g., `MaxHealth` / `MAX_HEALTH`)

## Performance Budgets

- **Target Framerate**: 60fps (PC 기준)
- **Frame Budget**: 16.6ms @ 60fps
- **Draw Calls**: MVP 단계 미세 튜닝 보류 (실측 후 결정)
- **Memory Ceiling**: MVP 단계 미세 튜닝 보류 (실측 후 결정)

## Testing

- **Framework**: Unity Test Framework (NUnit 기반)
- **Minimum Coverage**: MVP 단계 — 핵심 로직(무기 공식, 데미지 계산 등) 필수 / 전체 비율은 추후 결정
- **Required Tests**: Balance formulas, gameplay systems

## Forbidden Patterns

- `Object.FindObjectsOfType<T>()` — 대신 `Object.FindObjectsByType<T>(FindObjectsSortMode.None)` 사용
- `Object.FindObjectOfType<T>()` — 대신 `Object.FindFirstObjectByType<T>()` 또는 `FindAnyObjectByType<T>()` 사용
- URP Compatibility Mode (`ScriptableRenderPass` 구식 방식) — Render Graph API 사용
- `Entities.ForEach` (DOTS) — `IJobEntity` 또는 `SystemAPI.Query` 사용 (6.5에서 제거 예정)
- 런타임에서 `FindObjectsOfType` 반복 호출 — 캐싱 필수

## Allowed Libraries / Addons

- Unity Input System (`com.unity.inputsystem`) — 기본 Input 대신 사용 권장 (BladeSurge 4파일 사용 중)
- Unity UI (uGUI)
- TextMesh Pro
- Universal RP (`com.unity.render-pipelines.universal`)

## Engine Specialists

- **Primary**: `unity-csharp` (C# 스크립팅, MonoBehaviour, New Input System)
- **Architect**: `unity-architect` (폴더 구조, Asmdef, ScriptableObject 아키텍처)
- **Debugger**: `unity-debugger` (컴파일 에러, NullReferenceException, 성능)
- **Disabled**: `godot-*` (5개) — `.claude/agents/_disabled/` 에 격리

### File Extension Routing

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| `.cs` (게임 로직) | `unity-csharp` |
| `.cs` (Editor 폴더) | `unity-csharp` (Editor 컨텍스트 명시) |
| `.shader`, `.shadergraph` | (추후 unity-shader 추가 검토) |
| `.uxml`, `.uss` | (추후 unity-ui 추가 검토) |
| `.unity` (Scene), `.prefab` | `unity-architect` |
| `.asset` (ScriptableObject) | `unity-architect` |
| `.asmdef` | `unity-architect` |
| 컴파일/런타임 에러 | `unity-debugger` |
| 일반 아키텍처 검토 | `unity-architect` (Primary) |

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- ADR-001 (가칭): Engine = Unity 6.3 LTS — 본 파일 Step 2 결정 (2026-04-26). 정식 ADR은 `/architecture-decision`으로 작성 권장.
