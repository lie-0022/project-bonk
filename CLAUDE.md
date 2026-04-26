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

## Naming Conventions

@.claude/docs/naming-conventions.md

## Coding Standards

@.claude/docs/coding-standards.md

## Context Management

@.claude/docs/context-management.md
