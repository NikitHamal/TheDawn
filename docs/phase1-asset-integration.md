# Phase 1 Asset Integration Pass

This pass replaces the earlier raw-sheet stamping renderer with a normalized atlas pipeline.

## What changed

- `Content/Assets/Generated/TerrainAtlas.png` contains 32x32 opaque, tile-safe terrain cells for grass, dirt, water, stone, cave, dungeon, snow, and ruins.
- `Content/Assets/Generated/ObjectsAtlas.png` contains cleaned 64x64 object cells cropped from the Pixel Crawler pack: trees, bushes, crops, rocks, ore, crystals, campfire, workbench, sawmill, furnace, anvil, alchemy table, tower, barracks, traps, farm plot, walls, and gates.
- `AssetStore.TerrainSource(...)` and `AssetStore.ObjectCell(...)` are now the only places that map tile/object visuals to generated atlas rectangles.
- `PlayScreen` renders terrain from the generated atlas instead of drawing huge transparent 80x80 source cells over colored rectangles.
- `WorldGenerator` was replaced with deterministic layered world fields: domain-warped elevation, moisture, temperature, narrow rivers, lakes, caves, ruin centers, dungeon entrance zone, forests, veins, and clustered resources.

## Audit images

- `docs/phase1-generated-atlas-audit.png` shows the generated terrain and object atlas cells.
- `docs/phase1-worldgen-audit.png` shows a representative spawn-area world composition using the new atlas and generator.

## Rationale

The Pixel Crawler terrain sheets are not production-ready tile atlases by themselves. Several cells are decorative blob masks, palette labels, transparent cutouts, or sheet-layout regions. Drawing those cells directly creates black holes, giant squares, repeated labels, and obvious sheet artifacts. The generated atlas preserves the pack's visual language while converting it into clean runtime cells.
