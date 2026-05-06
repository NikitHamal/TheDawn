using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheDawn.Data;

namespace TheDawn.Assets;

public sealed class AssetStore
{
    private const string Root = "Content/Assets/PixelCrawlerFreePack";
    private readonly GraphicsDevice _graphics;
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.OrdinalIgnoreCase);

    public AssetStore(GraphicsDevice graphics) => _graphics = graphics;

    public Texture2D this[string id] => _textures[id];

    public Texture2D Texture(string id) => _textures[id];

    public bool Has(string id) => _textures.ContainsKey(id);

    public void LoadAll()
    {
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

    public static string TileTextureId(TileType tile) => tile switch
    {
        TileType.Water => "water",
        TileType.DungeonFloor or TileType.RuinFloor => "dungeon",
        _ => "floors"
    };

    public static Color TileBaseColor(TileType tile, int x, int y)
    {
        var delta = (StableHash(x, y, 19) % 15) - 7;
        var baseColor = tile switch
        {
            TileType.Grass => new Color(40, 112, 24),
            TileType.Dirt => new Color(108, 77, 44),
            TileType.Stone => new Color(80, 84, 80),
            TileType.CaveFloor => new Color(45, 49, 48),
            TileType.DungeonFloor => new Color(31, 38, 43),
            TileType.Snow => new Color(172, 190, 214),
            TileType.RuinFloor => new Color(58, 55, 50),
            TileType.Water => new Color(52, 135, 204),
            _ => new Color(40, 112, 24)
        };
        return Shade(baseColor, delta);
    }

    public static Rectangle? TileOverlaySource(TileType tile, int x, int y)
    {
        var variant = StableHash(x, y, 37) & 7;
        return tile switch
        {
            TileType.Grass => variant switch
            {
                0 => new Rectangle(0, 0, 80, 80),
                1 => new Rectangle(0, 80, 80, 80),
                _ => null
            },
            TileType.Dirt => variant < 5 ? new Rectangle(160, variant % 2 == 0 ? 0 : 80, 80, 80) : null,
            TileType.Stone => new Rectangle(240, 0, 80, 80),
            TileType.CaveFloor => variant < 6 ? new Rectangle(320, 0, 80, 80) : null,
            TileType.DungeonFloor => new Rectangle((variant % 2) * 80, 0, 80, 80),
            TileType.RuinFloor => new Rectangle(240, 0, 80, 80),
            TileType.Snow => variant < 6 ? new Rectangle(0, 240 + (variant % 2) * 80, 80, 80) : null,
            TileType.Water => new Rectangle((variant & 1) * 80, 80 + ((variant >> 1) & 1) * 80, 80, 80),
            _ => null
        };
    }

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

    private static Color Shade(Color color, int delta)
    {
        static byte Clamp(int value) => (byte)Math.Clamp(value, 0, 255);
        return new Color(Clamp(color.R + delta), Clamp(color.G + delta), Clamp(color.B + delta), color.A);
    }
}
