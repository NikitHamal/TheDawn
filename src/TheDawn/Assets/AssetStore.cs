using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheDawn.Data;

namespace TheDawn.Assets;

public sealed class AssetStore
{
    private const string Root = "Content/Assets/PixelCrawlerFreePack";
    private const string GeneratedRoot = "Content/Assets/Generated";
    private readonly GraphicsDevice _graphics;
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.OrdinalIgnoreCase);

    public AssetStore(GraphicsDevice graphics) => _graphics = graphics;

    public Texture2D this[string id] => _textures[id];

    public Texture2D Texture(string id) => _textures[id];

    public bool Has(string id) => _textures.ContainsKey(id);

    public void LoadAll()
    {
        LoadGenerated("terrain_atlas", "TerrainAtlas.png");
        LoadGenerated("objects_atlas", "ObjectsAtlas.png");

        Load("floors", "Environment/Tilesets/Floors_Tiles.png");
        Load("water", "Environment/Tilesets/Water_tiles.png");
        Load("walls", "Environment/Tilesets/Wall_Tiles.png");
        Load("wall_variations", "Environment/Tilesets/Wall_Variations.png");
        Load("dungeon", "Environment/Tilesets/Dungeon_Tiles.png");
        Load("building_walls", "Environment/Structures/Buildings/Walls.png");
        Load("building_floors", "Environment/Structures/Buildings/Floors.png");
        Load("building_props", "Environment/Structures/Buildings/Props.png");
        Load("vegetation", "Environment/Props/Static/Vegetation.png");
        Load("resources", "Environment/Props/Static/Resources.png");
        Load("rocks", "Environment/Props/Static/Rocks.png");
        Load("farm", "Environment/Props/Static/Farm.png");
        Load("tools", "Environment/Props/Static/Tools.png");
        Load("dungeon_props", "Environment/Props/Static/Dungeon_Props.png");
        Load("tree_a", "Environment/Props/Static/Trees/Model_01/Size_02.png");
        Load("tree_b", "Environment/Props/Static/Trees/Model_02/Size_02.png");
        Load("tree_c", "Environment/Props/Static/Trees/Model_03/Size_02.png");
        Load("campfire", "Environment/Structures/Stations/Bonfire/Bonfire.png");
        Load("fire", "Environment/Structures/Stations/Bonfire/Fire_01-Sheet.png");
        Load("workbench", "Environment/Structures/Stations/Workbench/Workbench.png");
        Load("sawmill", "Environment/Structures/Stations/Sawmill/Base.png");
        Load("furnace", "Environment/Structures/Stations/Furnace/Furnace.png");
        Load("anvil", "Environment/Structures/Stations/Anvil/Anvil.png");
        Load("alchemy", "Environment/Structures/Stations/Alchemy/Alchemy_Table_03-Sheet.png");
        Load("player_idle_down", "Entities/Characters/Body_A/Animations/Idle_Base/Idle_Down-Sheet.png");
        Load("player_idle_up", "Entities/Characters/Body_A/Animations/Idle_Base/Idle_Up-Sheet.png");
        Load("player_idle_side", "Entities/Characters/Body_A/Animations/Idle_Base/Idle_Side-Sheet.png");
        Load("player_run_down", "Entities/Characters/Body_A/Animations/Run_Base/Run_Down-Sheet.png");
        Load("player_run_up", "Entities/Characters/Body_A/Animations/Run_Base/Run_Up-Sheet.png");
        Load("player_run_side", "Entities/Characters/Body_A/Animations/Run_Base/Run_Side-Sheet.png");
        Load("player_slice_down", "Entities/Characters/Body_A/Animations/Slice_Base/Slice_Down-Sheet.png");
        Load("player_slice_side", "Entities/Characters/Body_A/Animations/Slice_Base/Slice_Side-Sheet.png");
        Load("skeleton_rogue_run", "Entities/Mobs/Skeleton Crew/Skeleton - Rogue/Run/Run-Sheet.png");
        Load("skeleton_warrior_run", "Entities/Mobs/Skeleton Crew/Skeleton - Warrior/Run/Run-Sheet.png");
        Load("skeleton_mage_run", "Entities/Mobs/Skeleton Crew/Skeleton - Mage/Run/Run-Sheet.png");
        Load("orc_rogue_run", "Entities/Mobs/Orc Crew/Orc - Rogue/Run/Run-Sheet.png");
        Load("orc_warrior_run", "Entities/Mobs/Orc Crew/Orc - Warrior/Run/Run-Sheet.png");
        Load("orc_shaman_run", "Entities/Mobs/Orc Crew/Orc - Shaman/Run/Run-Sheet.png");
        Load("knight_run", "Entities/Npc's/Knight/Run/Run-Sheet.png");
        Load("rogue_run", "Entities/Npc's/Rogue/Run/Run-Sheet.png");
        Load("wizard_run", "Entities/Npc's/Wizzard/Run/Run-Sheet.png");
        Load("wood_weapons", "Weapons/Wood/Wood.png");
        Load("bone_weapons", "Weapons/Bone/Bone.png");
    }

    private void Load(string id, string relativePath)
    {
        using var stream = OpenStream($"{Root}/{relativePath}");
        var texture = Texture2D.FromStream(_graphics, stream);
        texture.Name = id;
        _textures[id] = texture;
    }

    private void LoadGenerated(string id, string relativePath)
    {
        using var stream = OpenStream($"{GeneratedRoot}/{relativePath}");
        var texture = Texture2D.FromStream(_graphics, stream);
        texture.Name = id;
        _textures[id] = texture;
    }

    private static Stream OpenStream(string path)
    {
        try
        {
            return TitleContainer.OpenStream(path);
        }
        catch
        {
            var basePath = AppContext.BaseDirectory;
            var normalized = path.Replace('/', Path.DirectorySeparatorChar);
            var candidates = new[]
            {
                Path.Combine(basePath, normalized),
                Path.Combine(Directory.GetCurrentDirectory(), normalized),
                Path.Combine(basePath, "..", "..", "..", normalized)
            };
            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate)) return File.OpenRead(candidate);
            }
            throw new FileNotFoundException($"Asset not found: {path}");
        }
    }

    public static Rectangle TerrainSource(TileType tile, int x, int y)
    {
        var row = tile switch
        {
            TileType.Grass => 0,
            TileType.Dirt => 1,
            TileType.Water => 2,
            TileType.Stone => 3,
            TileType.CaveFloor => 4,
            TileType.DungeonFloor => 5,
            TileType.Snow => 6,
            TileType.RuinFloor => 7,
            _ => 0
        };
        var variant = StableHash(x, y, 37) & 3;
        return AtlasCell(variant, row, 32);
    }

    public static Rectangle ObjectCell(int index, int cellSize = 64) => AtlasCell(index % 8, index / 8, cellSize);

    private static Rectangle AtlasCell(int column, int row, int cellSize) => new(column * cellSize, row * cellSize, cellSize, cellSize);

    public static int StableHash(int x, int y, int salt)
    {
        unchecked
        {
            var h = 2166136261u;
            h = (h ^ (uint)x) * 16777619u;
            h = (h ^ (uint)(x >> 16)) * 16777619u;
            h = (h ^ (uint)y) * 16777619u;
            h = (h ^ (uint)(y >> 16)) * 16777619u;
            h = (h ^ (uint)salt) * 16777619u;
            return (int)(h & 0x7FFFFFFF);
        }
    }
}
