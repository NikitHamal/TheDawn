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
