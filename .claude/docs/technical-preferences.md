# Technical Preferences

<!-- Populated by /setup-engine. Updated as the user makes decisions throughout development. -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: Unity 6.3 LTS (6000.3.x)
- **Language**: C#
- **Rendering**: Universal Render Pipeline (URP) — Render Graph API (Compatibility Mode 사용 금지)
- **Physics**: Unity PhysX 3D

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

- **Target Framerate**: [TO BE CONFIGURED — 60fps 권장]
- **Frame Budget**: [TO BE CONFIGURED — 16.6ms @ 60fps]
- **Draw Calls**: [TO BE CONFIGURED]
- **Memory Ceiling**: [TO BE CONFIGURED]

## Testing

- **Framework**: Unity Test Framework (NUnit 기반)
- **Minimum Coverage**: [TO BE CONFIGURED]
- **Required Tests**: Balance formulas, gameplay systems

## Forbidden Patterns

- `Object.FindObjectsOfType<T>()` — 대신 `Object.FindObjectsByType<T>(FindObjectsSortMode.None)` 사용
- `Object.FindObjectOfType<T>()` — 대신 `Object.FindFirstObjectByType<T>()` 또는 `FindAnyObjectByType<T>()` 사용
- URP Compatibility Mode (`ScriptableRenderPass` 구식 방식) — Render Graph API 사용
- `Entities.ForEach` (DOTS) — `IJobEntity` 또는 `SystemAPI.Query` 사용 (6.5에서 제거 예정)
- 런타임에서 `FindObjectsOfType` 반복 호출 — 캐싱 필수

## Allowed Libraries / Addons

- Unity Input System (`com.unity.inputsystem`) — 기본 Input 대신 사용 권장
- [None configured yet — add as dependencies are approved]

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- [No ADRs yet — use /architecture-decision to create one]
