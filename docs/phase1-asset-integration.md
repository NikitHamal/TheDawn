# Phase 1 Asset Integration

Phase 1 no longer ships generated visual atlases.

The game renders from the original Pixel Crawler Free Pack files under:

`src/TheDawn/Content/Assets/PixelCrawlerFreePack/`

Runtime rendering uses source rectangles into the original sheets only:

- terrain: `Floors_Tiles.png`, `Water_tiles.png`, `Wall_Tiles.png`, `Wall_Variations.png`, `Dungeon_Tiles.png`, and `Buildings/Floors.png`
- world props: `Vegetation.png`, `Rocks.png`, `Resources.png`, `Farm.png`, `Dungeon_Props.png`, `Buildings/Props.png`
- trees: `Trees/Model_01`, `Model_02`, and `Model_03`, multiple sizes
- structures: original station and building sheets such as Bonfire, Workbench, Sawmill, Furnace, Anvil, Alchemy, and Buildings/Walls
- animation: original character, mob, NPC, fire, smoke, alchemy, and station sheets

Important distinction:

- Runtime atlas PNGs are not used.
- Generated audit/contact-sheet PNGs in `docs/` are visual QA artifacts only. They are not referenced by the game and are not loaded at runtime.
- Base terrain colors are drawn at runtime behind transparent Pixel Crawler terrain stamps because some original tiles are masks/edge pieces rather than fully opaque ground tiles.

Primary code touchpoints:

- `Assets/AssetStore.cs` loads original source PNG files and maps terrain tile types directly to original source rectangles.
- `Screens/PlayScreen.cs` draws terrain, resources, structures, decorations, entities, and animated props directly from original source textures.
