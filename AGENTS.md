# AGENTS.md - The Dawn AI handoff file

## Project intent

The Dawn is an open-world, endless, permadeath 2D pixel-art survival RPG built with C# and MonoGame. The game must remain modular, deterministic where world generation and raid scaling are concerned, and production-oriented. Avoid placeholder mechanics, hard-to-change monoliths, and hidden build steps.

## Non-negotiable constraints

- Framework: C# + MonoGame.
- Windows package target: `net8.0` DesktopGL.
- Android package target: `net8.0-android`.
- Android package name: `com.the.dawn`.
- CI must trigger on any push to any branch.
- Windows and Android workflows must stay separate.
- Android builds must use the committed default keystore at `android-signing/the-dawn-default.keystore` unless explicitly replaced.
- Produced app artifacts must be renamed with the commit hash.
- Preserve the asset terms file and do not sell the asset pack itself.
- Permadeath means the live save is deleted/archived on player death.

## Current actions completed

- Created complete repository layout under `TheDawn/`.
- Integrated Pixel Crawler Free Pack PNG assets into `src/TheDawn/Content/Assets/PixelCrawlerFreePack/`.
- Preserved `Terms.txt` from the asset pack.
- Created `docs/asset-audit.json` with PNG paths and dimensions.
- Created `docs/asset-contact-sheet.png` after visual inspection of major tile/entity/station sheets.
- Generated a committed default Android keystore:
  - path: `android-signing/the-dawn-default.keystore`
  - alias: `thedawn`
  - store/key password: `dawn-default-changeit`
- Added separate GitHub Actions workflows:
  - `.github/workflows/windows.yml`
  - `.github/workflows/android.yml`
- Implemented menu, create world, load world, options/controls, loading screen, and play screen.
- Implemented raw PNG asset loading instead of MGCB to simplify desktop/Android CI asset inclusion.
- Implemented deterministic infinite chunk generation, rivers, caves, dungeon entrance pressure direction, ruins, snow, resources, and lazy chunk caching.
- Implemented player movement, hunger, health, inventory, gathering, fishing, repair, crafting, building, unit hiring, day cycle, raid director, enemy AI, projectiles, save/load, and permadeath archive.
- Implemented Dijkstra/BFS pathing around water and solid structures.

## Current state

The codebase is a first full playable vertical slice. It is structured for extension rather than one-off demo scripting. The container used to create this repo does not have the .NET SDK installed, so local compilation in this environment was not possible. GitHub Actions and developer machines with .NET 8 + Android workload are the intended build verification path.

## Next actions for future agents

1. Run the Windows workflow or local `dotnet publish -f net8.0` and fix any compiler errors caused by platform-specific MonoGame API drift.
2. Run the Android workflow and confirm the signed APK/AAB filenames resolve as expected for the installed .NET Android toolchain.
3. Add automated smoke tests for deterministic world generation and save/load DTO round-tripping.
4. Add a proper content/version manifest and a title-screen asset-logo pass.
5. Expand biomes with authored structure templates and dungeon-room generation.
6. Add audio, settings persistence, key rebinding, and accessibility toggles.
7. Replace the private default Android keystore before public distribution.
8. Add release signing certificates/secrets if Windows packages must be trusted by SmartScreen rather than self-signed.

## Open questions

- Should the real-time day duration remain long-form survival pacing, or should a selectable shorter pacing mode be added for testing/streaming?
- Should dungeon raids physically march from the visible dungeon entrance over long distances, or should the current active-area vanguard spawning remain for playability?
- Which additional units should be prioritized after swordsman/archer/mage/miner/farmer?
- Should the final public release use MGCB-processed content for compression and platform-specific texture processing, or keep raw PNG loading for transparent modding?

## Coding guidance

- Keep gameplay constants in `Data/GameBalance.cs` or `Game/GameConfig.cs`.
- Do not bury balance values in rendering or input classes.
- Keep save DTOs in `Persistence/SaveGame.cs`; do not serialize MonoGame runtime objects directly.
- Keep procedural generation deterministic from `World/ValueNoise.cs` and `World/WorldGenerator.cs`.
- Use explicit data tables for new enemies, structures, units, and recipes.
- Keep new screens behind the `IGameScreen` interface.
- Never remove `Terms.txt` from the included asset pack.

## Visual repair pass after first run screenshot

- Fixed terrain rendering that drew transparent atlas cells directly over the black clear color.
- Terrain now renders an opaque tile base color first, then draws asset-pack overlays on top with deterministic coordinate variants.
- Corrected resource and tree rendering so full sprite sheets and `PALETTE:` labels are not drawn in-world.
- Cropped trees, berry bushes, rocks, crystals, ore, and gold to explicit sprite rectangles from the Pixel Crawler sheets.
- Corrected several building/station source rectangles so workbench, sawmill, furnace, anvil, walls, traps, and farm plots do not display unrelated atlas regions.
- Hid the mobile touch overlay on DesktopGL builds; it now only appears in Android builds.
- Future agents should treat the first screenshot issue as an asset-atlas interpretation bug, not a gameplay-system bug.

## Recent CI repair — restore framework flags

GitHub Actions restore commands were corrected after CI reported `MSB1008: Only one project can be specified`. The cause was using `dotnet restore ... -f net8.0`; for `dotnet restore`, `-f` is the short form for force, not target framework, so `net8.0` became a second positional project argument. Workflows and README now use `-p:TargetFramework=net8.0` and `-p:TargetFramework=net8.0-android` for target-specific restores. A root `global.json` was also added to pin CI to the .NET 8 SDK feature band instead of accidentally using newer preinstalled SDKs.

## 2026-05-06 CI Repair Pass 2

CI logs showed two workflow issues:

1. The Windows workflow still evaluated the Android target during `dotnet publish`, which failed on `windows-latest` because the Android workload is not installed there. The fix is to constrain both restore and publish with `-p:TargetFramework=net8.0 -p:TargetFrameworks=net8.0`, and publish with `--no-restore` so the publish step does not perform a fresh multi-target restore.
2. The Android workflow passed `AndroidPackageFormats=apk;aab` as a semicolon-delimited command-line property. MSBuild parsed `aab` as a separate switch in CI. The fix is to build APK and AAB in two explicit `dotnet publish` commands using singular `-p:AndroidPackageFormat=apk` and `-p:AndroidPackageFormat=aab`.

The project file now declares default target frameworks only when `$(TargetFrameworks)` is empty, which lets CI intentionally constrain each workflow to one platform target.

## Recent CI repair - runtime restore and Node 24 actions

- Fixed Windows publish by restoring `net8.0` with `-r win-x64` before `--no-restore` publish. This resolves missing `net8.0/win-x64` assets in `project.assets.json`.
- Added conditional desktop `RuntimeIdentifiers` entry for `win-x64` in `TheDawn.csproj`.
- Replaced Node 20-era action versions with current Node 24-compatible action versions: `actions/checkout@v6`, `actions/setup-dotnet@v5`, `actions/setup-java@v5`, and `actions/upload-artifact@v7`.
- Pinned CI runners to `windows-2022` and `ubuntu-22.04` for stable .NET 8/Android workload behavior.
- Android package output is built with two separate publishes: `-p:AndroidPackageFormat=apk` and `-p:AndroidPackageFormat=aab`. Do not use a semicolon-delimited `apk;aab` command-line property; CI parsed `aab` as a separate MSBuild switch.
## 2026-05-06 CI Repair Pass 4 - Windows ReadyToRun and Android Semicolon

Latest CI logs showed:

1. Windows publish restored and compiled `net8.0/win-x64`, but failed with `NETSDK1094` while optimizing assemblies for ReadyToRun. The workflow now sets `-p:PublishReadyToRun=false` while keeping self-contained single-file output and Authenticode signing.
2. Android still failed with `MSB1006: Switch: aab`, meaning semicolon package formats were still being split before or inside MSBuild. The workflow now publishes APK and AAB in two independent `dotnet publish` calls using singular `AndroidPackageFormat` values.

Keep these CI choices unless a future agent can run the workflows end-to-end and prove a different configuration works.
