# The Dawn

**The Dawn** is a 2D pixel-art open-world survival game built with C# and MonoGame. Each run creates a new deterministic infinite jungle world. The player gathers, fishes, mines, farms, builds, hires defenders, survives escalating night raids, and loses the live save permanently on death.

## Current playable phase

This repository is a full first playable vertical slice with production-oriented modular code rather than a throwaway prototype. It includes:

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
dotnet restore src/TheDawn/TheDawn.csproj -p:TargetFramework=net8.0
dotnet publish src/TheDawn/TheDawn.csproj -f net8.0 -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/windows/TheDawn-win-x64
```

### Android

```bash
dotnet workload install android
dotnet restore src/TheDawn/TheDawn.csproj -p:TargetFramework=net8.0-android
dotnet publish src/TheDawn/TheDawn.csproj -f net8.0-android -c Release \
  -p:ApplicationId=com.the.dawn \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=android-signing/the-dawn-default.keystore \
  -p:AndroidSigningKeyAlias=thedawn \
  -p:AndroidSigningStorePass=dawn-default-changeit \
  -p:AndroidSigningKeyPass=dawn-default-changeit \
  -p:AndroidPackageFormats=apk,aab
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
