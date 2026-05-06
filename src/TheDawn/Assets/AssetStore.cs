using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheDawn.Data;

namespace TheDawn.Assets;

public readonly record struct TerrainVisual(string Texture, Rectangle Source, Color BaseColor, Color Tint);
public readonly record struct SpriteVisual(string Texture, Rectangle Source, Vector2 Size, Vector2 Offset, SpriteEffects Effects);

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
        Load("building_roofs", "Environment/Structures/Buildings/Roofs.png");
        Load("building_shadows", "Environment/Structures/Buildings/Shadows.png");
        Load("vegetation", "Environment/Props/Static/Vegetation.png");
        Load("resources", "Environment/Props/Static/Resources.png");
        Load("rocks", "Environment/Props/Static/Rocks.png");
        Load("farm", "Environment/Props/Static/Farm.png");
        Load("tools", "Environment/Props/Static/Tools.png");
        Load("shadows", "Environment/Props/Static/Shadows.png");
        Load("dungeon_props", "Environment/Props/Static/Dungeon_Props.png");
        Load("esoteric", "Environment/Props/Static/Esoteric.png");
        Load("furniture", "Environment/Props/Static/Furniture.png");
        Load("meat", "Environment/Props/Static/Meat.png");
        Load("pan_static", "Environment/Props/Static/Pan.png");
        Load("pan_anim_01", "Environment/Props/Animated/Pan_01-Sheet.png");
        Load("pan_anim_02", "Environment/Props/Animated/Pan_02-Sheet.png");
        Load("pan_anim_03", "Environment/Props/Animated/Pan_03-Sheet.png");
        Load("pan_anim_04", "Environment/Props/Animated/Pan_04-Sheet.png");
        Load("pan_anim_05", "Environment/Props/Animated/Pan_05-Sheet.png");
        Load("tree_a", "Environment/Props/Static/Trees/Model_01/Size_02.png");
        Load("tree_b", "Environment/Props/Static/Trees/Model_02/Size_02.png");
        Load("tree_c", "Environment/Props/Static/Trees/Model_03/Size_02.png");
        Load("tree_m1_s2", "Environment/Props/Static/Trees/Model_01/Size_02.png");
        Load("tree_m1_s3", "Environment/Props/Static/Trees/Model_01/Size_03.png");
        Load("tree_m2_s2", "Environment/Props/Static/Trees/Model_02/Size_02.png");
        Load("tree_m2_s3", "Environment/Props/Static/Trees/Model_02/Size_03.png");
        Load("tree_m3_s2", "Environment/Props/Static/Trees/Model_03/Size_02.png");
        Load("tree_m3_s3", "Environment/Props/Static/Trees/Model_03/Size_03.png");
        Load("campfire", "Environment/Structures/Stations/Bonfire/Bonfire.png");
        Load("fire", "Environment/Structures/Stations/Bonfire/Fire_01-Sheet.png");
        Load("smoke", "Environment/Structures/Stations/Bonfire/Smoke-Sheet.png");
        Load("bonfire_anim", "Environment/Structures/Stations/Bonfire/Bonfire_09-Sheet.png");
        Load("workbench", "Environment/Structures/Stations/Workbench/Workbench.png");
        Load("sawmill", "Environment/Structures/Stations/Sawmill/Base.png");
        Load("sawmill_level1", "Environment/Structures/Stations/Sawmill/Level_1.png");
        Load("sawmill_level2", "Environment/Structures/Stations/Sawmill/Level_2-Sheet.png");
        Load("sawmill_level3", "Environment/Structures/Stations/Sawmill/Level_3-Sheet.png");
        Load("furnace", "Environment/Structures/Stations/Furnace/Furnace.png");
        Load("anvil", "Environment/Structures/Stations/Anvil/Anvil.png");
        Load("alchemy", "Environment/Structures/Stations/Alchemy/Alchemy_Table_03-Sheet.png");
        Load("alchemy_anim_01", "Environment/Structures/Stations/Alchemy/Alchemy_Table_01-Sheet.png");
        Load("alchemy_anim_02", "Environment/Structures/Stations/Alchemy/Alchemy_Table_02-Sheet.png");
        Load("cooking_station", "Environment/Structures/Stations/Cooking Station/Cooking Station.png");
        Load("cooker_01", "Environment/Structures/Stations/Cooking Station/Cooker/Cooker_01.png");
        Load("cooker_02", "Environment/Structures/Stations/Cooking Station/Cooker/Cooker_02.png");
        Load("grill_01", "Environment/Structures/Stations/Cooking Station/Grill/Grill_01-Sheet.png");
        Load("player_idle_down", "Entities/Characters/Body_A/Animations/Idle_Base/Idle_Down-Sheet.png");
        Load("player_idle_up", "Entities/Characters/Body_A/Animations/Idle_Base/Idle_Up-Sheet.png");
        Load("player_idle_side", "Entities/Characters/Body_A/Animations/Idle_Base/Idle_Side-Sheet.png");
        Load("player_run_down", "Entities/Characters/Body_A/Animations/Run_Base/Run_Down-Sheet.png");
        Load("player_run_up", "Entities/Characters/Body_A/Animations/Run_Base/Run_Up-Sheet.png");
        Load("player_run_side", "Entities/Characters/Body_A/Animations/Run_Base/Run_Side-Sheet.png");
        Load("player_slice_down", "Entities/Characters/Body_A/Animations/Slice_Base/Slice_Down-Sheet.png");
        Load("player_slice_side", "Entities/Characters/Body_A/Animations/Slice_Base/Slice_Side-Sheet.png");
        Load("player_slice_up", "Entities/Characters/Body_A/Animations/Slice_Base/Slice_Up-Sheet.png");
        Load("player_crush_down", "Entities/Characters/Body_A/Animations/Crush_Base/Crush_Down-Sheet.png");
        Load("player_crush_side", "Entities/Characters/Body_A/Animations/Crush_Base/Crush_Side-Sheet.png");
        Load("player_crush_up", "Entities/Characters/Body_A/Animations/Crush_Base/Crush_Up-Sheet.png");
        Load("player_collect_down", "Entities/Characters/Body_A/Animations/Collect_Base/Collect_Down-Sheet.png");
        Load("player_collect_side", "Entities/Characters/Body_A/Animations/Collect_Base/Collect_Side-Sheet.png");
        Load("player_collect_up", "Entities/Characters/Body_A/Animations/Collect_Base/Collect_Up-Sheet.png");
        Load("player_fishing_down", "Entities/Characters/Body_A/Animations/Fishing_Base/Fishing_Down-Sheet.png");
        Load("player_fishing_side", "Entities/Characters/Body_A/Animations/Fishing_Base/Fishing_Side-Sheet.png");
        Load("player_fishing_up", "Entities/Characters/Body_A/Animations/Fishing_Base/Fishing_Up-Sheet.png");
        Load("player_hit_down", "Entities/Characters/Body_A/Animations/Hit_Base/Hit_Down-Sheet.png");
        Load("player_hit_side", "Entities/Characters/Body_A/Animations/Hit_Base/Hit_Side-Sheet.png");
        Load("player_hit_up", "Entities/Characters/Body_A/Animations/Hit_Base/Hit_Up-Sheet.png");
        Load("player_watering_down", "Entities/Characters/Body_A/Animations/Watering_Base/Watering_Down-Sheet.png");
        Load("player_watering_side", "Entities/Characters/Body_A/Animations/Watering_Base/Watering_Side-Sheet.png");
        Load("player_watering_up", "Entities/Characters/Body_A/Animations/Watering_Base/Watering_Up-Sheet.png");
        Load("skeleton_rogue_idle", "Entities/Mobs/Skeleton Crew/Skeleton - Rogue/Idle/Idle-Sheet.png");
        Load("skeleton_rogue_run", "Entities/Mobs/Skeleton Crew/Skeleton - Rogue/Run/Run-Sheet.png");
        Load("skeleton_warrior_idle", "Entities/Mobs/Skeleton Crew/Skeleton - Warrior/Idle/Idle-Sheet.png");
        Load("skeleton_warrior_run", "Entities/Mobs/Skeleton Crew/Skeleton - Warrior/Run/Run-Sheet.png");
        Load("skeleton_mage_idle", "Entities/Mobs/Skeleton Crew/Skeleton - Mage/Idle/Idle-Sheet.png");
        Load("skeleton_mage_run", "Entities/Mobs/Skeleton Crew/Skeleton - Mage/Run/Run-Sheet.png");
        Load("orc_rogue_idle", "Entities/Mobs/Orc Crew/Orc - Rogue/Idle/Idle-Sheet.png");
        Load("orc_rogue_run", "Entities/Mobs/Orc Crew/Orc - Rogue/Run/Run-Sheet.png");
        Load("orc_warrior_idle", "Entities/Mobs/Orc Crew/Orc - Warrior/Idle/Idle-Sheet.png");
        Load("orc_warrior_run", "Entities/Mobs/Orc Crew/Orc - Warrior/Run/Run-Sheet.png");
        Load("orc_shaman_idle", "Entities/Mobs/Orc Crew/Orc - Shaman/Idle/Idle-Sheet.png");
        Load("orc_shaman_run", "Entities/Mobs/Orc Crew/Orc - Shaman/Run/Run-Sheet.png");
        Load("knight_idle", "Entities/Npc's/Knight/Idle/Idle-Sheet.png");
        Load("knight_run", "Entities/Npc's/Knight/Run/Run-Sheet.png");
        Load("rogue_idle", "Entities/Npc's/Rogue/Idle/Idle-Sheet.png");
        Load("rogue_run", "Entities/Npc's/Rogue/Run/Run-Sheet.png");
        Load("wizard_idle", "Entities/Npc's/Wizzard/Idle/Idle-Sheet.png");
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

    public static TerrainVisual TerrainVisual(TileType tile, int x, int y)
    {
        var h = StableHash(x, y, 37);
        return tile switch
        {
            TileType.Water => new TerrainVisual("water", Cell80(h & 1, 1 + ((h >> 3) & 1)), new Color(43, 137, 201), Color.White),
            TileType.Dirt => new TerrainVisual("floors", Cell80(1 + ((h >> 2) & 1), (h >> 5) & 1), new Color(126, 92, 59), Color.White),
            TileType.Stone => new TerrainVisual("walls", Cell80(1 + ((h >> 1) & 1), 3), new Color(96, 98, 94), Color.White),
            TileType.CaveFloor => new TerrainVisual("wall_variations", Cell80((h >> 1) % 3, 1), new Color(58, 54, 50), Color.White),
            TileType.DungeonFloor => new TerrainVisual("dungeon", Cell80(0, (h >> 3) & 1), new Color(30, 40, 46), Color.White),
            TileType.Snow => new TerrainVisual("floors", Cell80((h >> 2) & 1, 2 + ((h >> 4) & 1)), new Color(226, 238, 246), Color.White),
            TileType.RuinFloor => new TerrainVisual("building_floors", new Rectangle(0, 0, 64, 64), new Color(104, 75, 48), Color.White),
            _ => new TerrainVisual("floors", Cell80(0, h & 1), new Color(42, 119, 35), Color.White)
        };
    }

    public static Rectangle Cell80(int column, int row) => new(column * 80, row * 80, 80, 80);
    public static Rectangle Cell64(int column, int row) => new(column * 64, row * 64, 64, 64);
    public static Rectangle Cell32(int column, int row) => new(column * 32, row * 32, 32, 32);
    public static Rectangle Cell16(int column, int row) => new(column * 16, row * 16, 16, 16);

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
