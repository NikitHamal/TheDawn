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

## 2026-05-06 Phase 1 Asset/World Repair Pass

Recent actions:
- Removed generated runtime visual atlases from `src/TheDawn/Content/Assets/Generated/`.
- Replaced generated-atlas rendering with direct source-sheet rendering. `PlayScreen` now draws source rectangles from the original Pixel Crawler files, with runtime base fills only where an original terrain stamp is transparent.
- Rebuilt `WorldGenerator` around deterministic world fields: elevation, moisture, temperature, narrow river masks, lakes, cave bands, ruin centers, dungeon entrance zone, forest density, and ore/resource veins.
- Added visual audit files:
  - `docs/phase1-worldgen-audit.png`
  - `docs/phase1-asset-integration.md`
- Changed Android package selection to use the repo property `TheDawnAndroidPackageFormat=apk|aab`, which feeds both Android package properties from the csproj and avoids semicolon-splitting in GitHub Actions.

Current state:
- Phase 1 is a playable survival build with deterministic world generation, cleaned asset integration, day/dusk/night/dawn cycle, gathering, building, hiring, raids, save/load, permadeath archive, and CI workflows.
- The container used for this handoff still does not include `dotnet`; perform final `dotnet restore`, `dotnet publish -f net8.0 -r win-x64`, and `dotnet publish -f net8.0-android` validation on a machine/runner with the SDK and Android workload.

Next actions:
1. Run the GitHub Actions workflows after extracting this package.
2. If any Android publishing target changes in future .NET Android releases, update only `TheDawnAndroidPackageFormat` mapping inside `TheDawn.csproj`; do not pass semicolon-separated package formats on the command line.
3. Continue the direct source-rectangle catalog as new Pixel Crawler sheets are introduced; do not reintroduce generated runtime atlas PNGs unless explicitly approved.
4. Add biome-specific decoration layers for cave props, snow crystals, and ruins once the first-pass runtime art is accepted.

Open questions:
- Whether Phase 2 should prioritize combat depth, inventory UI, NPC job assignment UI, or dungeon exploration rooms first.

## 2026-05-06 Phase 1 Deep Asset Integration / World Visual Repair

Reason for pass:
- User screenshots still showed a sparse, overly flat world, visible grid feel, too little use of the Pixel Crawler sheets, and no player/station action animation beyond basic idle/run.

Recent actions completed:
- Rebuilt `WorldGenerator` to guarantee a deterministic strategic river near every spawn while still supporting secondary rivers/lakes, cave spines, dungeon entrance terrain, ruins, snow, stone, dirt trails, and cave bands.
- Reduced the spawn exclusion from a huge empty field to a small playable clearing; starter-ring resources now appear around the player instead of far outside the camera.
- Added `WorldDecoration` and `DecorationType`, with a non-blocking deterministic decoration layer on each chunk: grass tufts, ferns, flowers, mushrooms, branches, pebbles, cave debris, dungeon props, ruin debris, snow clumps, and water foam.
- Increased runtime asset coverage in `AssetStore.LoadAll`: vegetation, resources, rocks, dungeon props, esoteric/furniture/meat/pan sheets, animated pan sheets, building roofs/shadows, sawmill levels, alchemy animation sheets, cooking/grill assets, additional tree model/size sheets, NPC idle sheets, mob idle sheets, and player action sheets.
- Replaced tree rendering for resources with direct, explicit tree-sheet crops from multiple models/sizes instead of only the normalized object atlas cells.
- Locked jungle tree resource variants to living/fall trees; dead/snow tree variants should be reserved for ruin/snow-biome-specific resources in a later pass.
- Added deterministic sprite jitter for resource node visuals so the world no longer reads as rigidly grid-stamped while keeping gameplay interaction tile-stable.
- Added player visual action state and animation selection for slice/chop, crush/mine, collect/gather, fishing, watering, and hit animations.
- Added enemy and hired-unit idle/run animation selection using the appropriate Pixel Crawler sheets.
- Added animated overlays for campfires, smoke, furnace fire, and alchemy table bubbles.
- Expanded `TerrainAtlas.png` to 8 deterministic variants per terrain row using Pixel Crawler base patterns plus opaque normalized cells.
- Updated `docs/asset-audit.json` and added:
  - `docs/phase1-deep-asset-contact.png`
  - `docs/phase1-terrain-atlas-deep-audit.png`
  - `docs/phase1-deep-worldgen-audit.png`

Current state:
- The current repo is a stronger Phase 1 pass, not a content-sheet stamp prototype. The world now has layered deterministic terrain, spawn resources, river strategy, non-blocking environmental dressing, direct trees, and action/station animations.
- The container still does not include a local .NET SDK, so syntax was carefully edited and file/YAML/XML/asset existence were verified, but final compile validation must be done in GitHub Actions or on a machine with .NET 8 and the Android workload.

Next actions:
1. Run Windows and Android GitHub Actions immediately after extracting.
2. If compile errors appear, fix those first before adding more content.
3. Add biome-specific resource visual variants: dead trees only in ruins/caves, snow trees only in snow, crystal shards around crystal deposits.
4. Add a proper object draw queue that interleaves structures, resources, units, and enemies by world Y for perfect depth sorting.
5. Add animated death/corpse persistence instead of removing enemies immediately.
6. Add authored dungeon-room templates and ruin templates using the loaded furniture/esoteric/dungeon prop sheets.
7. Add a real inventory/crafting UI so the expanded art assets are visible through icons, stations, and recipes.

Open questions:
- Should the spawn river always be visible in the first camera, or remain nearby but require a short walk for discovery?
- Should tree collision become multi-tile for large tree variants, or should Phase 1 keep tile-stable interactions for responsiveness?
- Should Phase 2 prioritize visual depth sorting, dungeon rooms, or the inventory/crafting UI?
