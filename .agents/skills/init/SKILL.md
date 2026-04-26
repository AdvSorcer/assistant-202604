---
name: init
description: Project initialization context loader for the explicit /init command or requests to understand the current project by reading infra.md. Use when the user invokes /init, asks Codex to initialize project context, or wants Codex to understand what this repository does from infra.md before taking further action.
---

# Init

Use this skill to establish project context from `infra.md`.

## Workflow

1. Locate `infra.md` in the current workspace root first.
2. If it is not in the root, search the workspace for a case-insensitive match such as `infra.md`, `INFRA.md`, or `Infra.md`.
3. Read the file before inspecting unrelated project files.
4. Summarize the project in practical terms:
   - what the project does
   - main architecture or infrastructure pieces
   - important commands, services, or workflows mentioned
   - constraints or conventions Codex should respect
5. If the user only invoked `/init`, stop after the summary and ask what they want to work on next.
6. If the user paired `/init` with another task, use the `infra.md` understanding as context and continue with that task.

## Missing File

If `infra.md` cannot be found, say that clearly and list the paths searched. Then inspect only the minimal project files needed to infer where infrastructure documentation might live, such as the repository root listing or documentation directory names.

## Output Style

Keep the response concise. Prefer a short project-context summary over a file-by-file explanation.
