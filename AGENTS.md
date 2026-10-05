# Repository Guidelines

## Project Structure & Module Organization

This is a Unity 6000.3.6f1 2D narrative game using URP, TextMesh Pro, and AI-generated scene directives.

- `Assets/Scripts/`: AI requests, scene orchestration, saving, and event channels.
- `Assets/Scripts/Data/`: save models and API-response parsing.
- `Assets/Scripts/Modules/`: dialogue, background, character, and choice presentation.
- `Assets/Scripts/Mock/`: offline scene playback for development.
- `Assets/Scenes/SampleScene.unity`: the current playable scene.
- `Assets/Resources/`: runtime sprites; backgrounds use IDs such as `tavern_night`, and characters use `speaker_emotion`, such as `stranger_angry`.
- `Assets/Prefabs/`, `Assets/Fonts/`: reusable UI and Korean text assets.
- `Packages/`, `ProjectSettings/`: dependencies and Unity configuration; `devlog/`: design decisions and development notes.

## Build, Test, and Development Commands

Open the project with Unity Hub using the recorded editor version. Open `SampleScene` and enter Play Mode for local development. Use Unity Build Profiles for player builds; no custom build script exists.

PowerShell editor import/compilation check:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath "$PWD" -logFile "$PWD\Logs\compile.log"
```

Inspect the log for compilation failures; process completion alone is insufficient.

## Coding Style & Naming Conventions

Use four-space indentation and braces on separate lines. Use PascalCase for types and methods, camelCase for fields and locals, and `[SerializeField] private` for Inspector references. Match script filenames to their MonoBehaviour types when adding scripts. No repository formatter or linter is configured.

Publish `SceneDirective` through the event channel; keep presentation logic in modules. Subscribe in `OnEnable` and unsubscribe in `OnDisable`.

## Testing Guidelines

Unity Test Framework 1.6.0 is installed, but no automated suites or coverage threshold exist. Add Edit Mode tests for parsers/save data and Play Mode tests for UI/state interactions under `Assets/Tests/`, with appropriate test assemblies. Name tests descriptively, such as `Parse_InvalidJson_ReturnsFailure`. Run through Window > General > Test Runner. Manually verify choices, missing sprites, and save/restore using mocks before paid API calls.

## Commit & Pull Request Guidelines

No Git history is available to establish conventions. Use focused imperative commits, such as `Fix save restoration`. Describe behavior changes, validation, and related issues in PRs; include screenshots for UI changes. Commit Unity `.meta` files with their assets. Exclude generated caches and logs.

## Security & Configuration

Set `ANTHROPIC_API_KEY` locally and restart Unity Hub/editor. Never commit or log credentials. Direct API calls are editor-only; distributed builds require a server proxy. Saves reside in `Application.persistentDataPath`; avoid destructive save resets during ordinary testing.
