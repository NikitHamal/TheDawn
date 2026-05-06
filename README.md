# The Dawn

**The Dawn** is a 2D pixel-art open-world survival game built with C# and MonoGame. Each run creates a new deterministic infinite jungle world. The player gathers, fishes, mines, farms, builds, hires defenders, survives escalating night raids, and loses the live save permanently on death.

## Current playable phase

This repository is a full first playable vertical slice with production-oriented modular code. It includes:

- MonoGame DesktopGL Windows target and MonoGame Android target.
- Main menu, create world, load world, options/controls, and loading screen.
- Infinite chunked procedural world using deterministic value-noise generation.
- Jungle, rivers, caves, dungeon entrance pressure direction, ruins, and distant snow biome rules.
- River collision that forces raid pathing around water and rewards base placement.
- Gathering: chop trees, mine rocks/ore/crystals/gold, gather berries/crops, fish rivers.
- Inventory, hunger, health, repair, crafting, building, farming, and save/load.
- Building tiers: wood, stone, iron, crystal walls plus campfire, stations, towers, barracks, alchemy, traps, farm plots.
- Hireable units: swordsman, archer, mage, miner, farmer; surviving units level permanently.
- Day/dusk/night/dawn loop with the requested 10-minute dusk preparation warning.
- Raid scaling according to day bands: skeleton probing, mixed squads, organized sieges, multi-wave raids, day-60+ pressure.
- Dijkstra/BFS grid pathing around rivers and structures, no magic navigation mesh dependency.
- Android committed default keystore for private-repo CI signing.
- GitHub Actions that run on push to every branch and produce commit-hash-renamed artifacts.

## Controls

| Action | Desktop |
| --- | --- |
| Move | WASD / Arrow keys |
| Use / attack / gather / fish / place build | E or left mouse |
| Eat food | Right mouse |
| Cycle build blueprint | B |
| Hire selected unit / cycle hire selection | H |
| Craft help | C |
| Quick craft | 1 sword, 2 bow, 3 cook fish, 4 ingot, 5 potion |
| Save | F5 |
| Pause | Esc |

Android includes touch movement/action zones and gamepad-friendly input paths.

## Build locally

Install .NET 8 SDK and the appropriate MonoGame/Android workloads.

### Windows DesktopGL

```bash
dotnet restore src/TheDawn/TheDawn.csproj -p:TargetFramework=net8.0 -p:TargetFrameworks=net8.0
dotnet publish src/TheDawn/TheDawn.csproj -f net8.0 -c Release -r win-x64 --self-contained true --no-restore -p:TargetFramework=net8.0 -p:TargetFrameworks=net8.0 -p:PublishSingleFile=true -p:PublishReadyToRun=false -o artifacts/windows/TheDawn-win-x64
```

### Android

```bash
dotnet workload install android
dotnet restore src/TheDawn/TheDawn.csproj -p:TargetFramework=net8.0-android -p:TargetFrameworks=net8.0-android

# APK
dotnet publish src/TheDawn/TheDawn.csproj -f net8.0-android -c Release --no-restore \
  -p:TargetFramework=net8.0-android \
  -p:TargetFrameworks=net8.0-android \
  -p:ApplicationId=com.the.dawn \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=android-signing/the-dawn-default.keystore \
  -p:AndroidSigningKeyAlias=thedawn \
  -p:AndroidSigningStorePass=dawn-default-changeit \
  -p:AndroidSigningKeyPass=dawn-default-changeit \
  -p:AndroidPackageFormat=apk

# AAB
dotnet publish src/TheDawn/TheDawn.csproj -f net8.0-android -c Release --no-restore \
  -p:TargetFramework=net8.0-android \
  -p:TargetFrameworks=net8.0-android \
  -p:ApplicationId=com.the.dawn \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=android-signing/the-dawn-default.keystore \
  -p:AndroidSigningKeyAlias=thedawn \
  -p:AndroidSigningStorePass=dawn-default-changeit \
  -p:AndroidSigningKeyPass=dawn-default-changeit \
  -p:AndroidPackageFormat=aab
```

## CI outputs

- `.github/workflows/windows.yml` publishes and Authenticode-signs the Windows executable with a private CI self-signed certificate, zips it, and uploads `TheDawn-<12-char-commit>-win-x64.zip`.
- `.github/workflows/android.yml` builds signed APK/AAB artifacts using `android-signing/the-dawn-default.keystore` and uploads `TheDawn-<12-char-commit>-signed.apk` plus AAB when produced.

The checked-in Android keystore is intentionally present because this repository is private, as requested. Replace it before any public release.

## Asset pack

Pixel art is integrated from `Pixel Crawler - Free Pack 2.0.4` by Anokolisa. Terms are included in `src/TheDawn/Content/Assets/PixelCrawlerFreePack/Terms.txt`. An asset audit, original contact sheet, and post-fix visual audit image are in `docs/`.

The atlas cells in this pack are mostly transparent overlays rather than complete opaque terrain tiles. The renderer therefore draws an opaque terrain base first and then layers cropped asset-pack overlays/sprites on top; do not revert terrain drawing to raw atlas-cell stamping.

## Project structure

```text
src/TheDawn
  Assets/       Raw PNG loading, atlas source definitions
  Data/         Item/building/unit/enemy identifiers and balance tables
  Entities/     Player, enemies, hired units, projectiles
  Game/         MonoGame bootstrap/config
  Input/        Keyboard, mouse, gamepad, touch abstraction
  Persistence/  JSON save, graveyard, permadeath live-save deletion
  Rendering/    Camera, pixel text, animation helpers
  Screens/      Menu, create world, loading, play screen
  Systems/      Session, time loop, raids, crafting
  World/        Chunked infinite generation and Dijkstra pathing
```

## CI repair notes

The Windows workflow restores the desktop target with `-r win-x64` before publishing with `--no-restore`; this is required so `project.assets.json` contains the `net8.0/win-x64` target used by the self-contained Windows publish. Windows CI now publishes with `-p:PublishReadyToRun=false` because the hosted runner restore did not provide a valid ReadyToRun runtime package for this MonoGame DesktopGL publish, causing `NETSDK1094`.

The Android workflow intentionally publishes twice, once with `-p:AndroidPackageFormat=apk` and once with `-p:AndroidPackageFormat=aab`. Do not combine these into `apk;aab` on the command line; GitHub Actions/MSBuild treated `aab` as a stray switch in CI.

## Phase 1 visual/world-generation repair

The runtime no longer stamps raw Pixel Crawler sheet cells into the world. The source pack includes palette labels, transparent gutters, and decorative blob cells that are not safe to draw as terrain tiles directly. Phase 1 now ships generated runtime atlases:

- The game now renders directly from original Pixel Crawler source sheets. No generated runtime visual atlases are shipped.
- `docs/phase1-asset-integration.md` documents the direct source-rectangle mapping approach.
- `docs/phase1-worldgen-audit.png` - representative world-generation audit.

Android packaging is selected with `TheDawnAndroidPackageFormat=apk` or `TheDawnAndroidPackageFormat=aab`, which is mapped inside the csproj. Do not pass semicolon-separated Android package format values in CI.

## Phase 1 deep asset/world pass

This package includes an additional repair pass over the earlier Phase 1 visuals:

- A deterministic non-blocking decoration layer was added to chunks, using vegetation, rocks, resources, dungeon props, building props, snow/water detail, and ruin/cave dressing.
- Spawn generation now creates a small safe clearing instead of a large empty green field, with starter-ring jungle resources near the player.
- Every seed now has a deterministic strategic river near spawn, plus secondary rivers/lakes, cave spines, dungeon trail pressure, ruins, snow, stone, and cave bands.
- Tree resources use direct Pixel Crawler tree sheet crops from multiple models and sizes, with deterministic visual offsets to reduce grid stamping while keeping tile-stable interaction.
- Player actions now display Pixel Crawler action animations for chopping/slicing, mining/crushing, gathering, fishing, watering, and hit reactions.
- NPCs and mobs now use idle/run animation selection instead of always running.
- Campfire/fire/smoke/furnace/alchemy now have animated overlays.
- The generated terrain atlas now provides eight variants per terrain type.

Audit images are available in `docs/`:

- `phase1-deep-asset-contact.png`
- direct source-sheet visual audit docs; generated runtime visual atlases removed.
- `phase1-deep-worldgen-audit.png`
