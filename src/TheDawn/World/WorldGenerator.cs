using Microsoft.Xna.Framework;
using TheDawn.Data;

namespace TheDawn.World;

public sealed class WorldGenerator
{
    private readonly int _seed;
    public TilePoint DungeonEntrance { get; }

    public WorldGenerator(int seed)
    {
        _seed = seed;
        var angle = ValueNoise.HashUnit(seed, 91, 11) * Math.PI * 2.0;
        var radius = 155 + (int)(ValueNoise.HashUnit(seed, 34, 77) * 80);
        DungeonEntrance = new TilePoint((int)Math.Round(Math.Cos(angle) * radius), (int)Math.Round(Math.Sin(angle) * radius));
    }

    public Chunk Generate(int chunkX, int chunkY)
    {
        var chunk = new Chunk(chunkX, chunkY);
        for (var ly = 0; ly < GameConfig.ChunkSize; ly++)
        {
            for (var lx = 0; lx < GameConfig.ChunkSize; lx++)
            {
                var gx = chunkX * GameConfig.ChunkSize + lx;
                var gy = chunkY * GameConfig.ChunkSize + ly;
                var tile = GenerateTile(gx, gy);
                chunk.SetLocal(lx, ly, tile);
            }
        }
        GenerateNodes(chunk);
        return chunk;
    }

    private TileType GenerateTile(int x, int y)
    {
        var dist = Math.Sqrt((double)x * x + (double)y * y);
        var dungeonDist = Math.Abs(x - DungeonEntrance.X) + Math.Abs(y - DungeonEntrance.Y);
        if (dungeonDist < 8) return TileType.DungeonFloor;

        var river = Math.Abs(ValueNoise.Fractal(_seed + 303, x, y, 0.006, 3, 0.55) - 0.5);
        var riverWidth = 0.027 + ValueNoise.Noise2D(_seed + 99, x, y, 0.015) * 0.018;
        if (river < riverWidth && dist > 10) return TileType.Water;

        var cave = ValueNoise.Fractal(_seed + 616, x, y, 0.012, 4, 0.5);
        var cold = ValueNoise.Fractal(_seed + 1515, x, y, 0.004, 3, 0.5);
        var ruins = ValueNoise.Fractal(_seed + 922, x, y, 0.018, 2, 0.45);
        if (dist > 620 && cold > 0.66) return TileType.Snow;
        if (dist > 90 && cave > 0.69) return TileType.CaveFloor;
        if (dist > 330 && ruins > 0.735) return TileType.RuinFloor;
        var dirt = ValueNoise.Fractal(_seed + 44, x, y, 0.035, 2, 0.55);
        return dirt > 0.64 ? TileType.Dirt : TileType.Grass;
    }

    private void GenerateNodes(Chunk chunk)
    {
        for (var ly = 0; ly < GameConfig.ChunkSize; ly++)
        {
            for (var lx = 0; lx < GameConfig.ChunkSize; lx++)
            {
                var gx = chunk.ChunkX * GameConfig.ChunkSize + lx;
                var gy = chunk.ChunkY * GameConfig.ChunkSize + ly;
                if (Math.Abs(gx) < GameConfig.SpawnSafeRadiusTiles && Math.Abs(gy) < GameConfig.SpawnSafeRadiusTiles) continue;
                var tile = chunk.GetLocal(lx, ly);
                if (tile == TileType.Water || tile == TileType.DungeonFloor) continue;
                var roll = ValueNoise.HashUnit(_seed, gx, gy, 401);
                ResourceNode? node = tile switch
                {
                    TileType.Grass when roll > 0.78 => Tree(gx, gy, roll),
                    TileType.Grass when roll is > 0.72 and <= 0.76 => Rock(gx, gy),
                    TileType.Grass when roll is > 0.68 and <= 0.72 => Bush(gx, gy),
                    TileType.Dirt when roll > 0.91 => WildCrop(gx, gy),
                    TileType.Dirt when roll is > 0.84 and <= 0.88 => Rock(gx, gy),
                    TileType.CaveFloor when roll > 0.83 => Ore(gx, gy, roll),
                    TileType.Stone when roll > 0.79 => Rock(gx, gy),
                    TileType.Snow when roll > 0.88 => Crystal(gx, gy),
                    TileType.RuinFloor when roll > 0.90 => Gold(gx, gy),
                    _ => null
                };
                if (node != null) chunk.Nodes.Add(node);
            }
        }
    }

    private static long NodeId(int x, int y, ResourceType type) => ((long)x & 0x1FFFFFL) << 42 | (((long)y & 0x1FFFFFL) << 21) | (long)type;

    private ResourceNode Tree(int x, int y, double roll)
    {
        var sprite = roll > 0.94 ? "tree_c" : roll > 0.87 ? "tree_b" : "tree_a";
        return new ResourceNode { Id = NodeId(x, y, ResourceType.Tree), Type = ResourceType.Tree, Tile = new TilePoint(x, y), Health = 34, MaxHealth = 34, SpriteId = sprite, Source = Rectangle.Empty, YieldItem = ItemId.Wood, YieldAmount = 5 };
    }

    private static ResourceNode Bush(int x, int y) => new() { Id = NodeId(x, y, ResourceType.BerryBush), Type = ResourceType.BerryBush, Tile = new TilePoint(x, y), Health = 10, MaxHealth = 10, SpriteId = "vegetation", Source = new Rectangle(0, 0, 80, 80), YieldItem = ItemId.Food, YieldAmount = 3 };
    private static ResourceNode WildCrop(int x, int y) => new() { Id = NodeId(x, y, ResourceType.WildCrop), Type = ResourceType.WildCrop, Tile = new TilePoint(x, y), Health = 8, MaxHealth = 8, SpriteId = "farm", Source = new Rectangle(0, 0, 80, 80), YieldItem = ItemId.Seed, YieldAmount = 2 };
    private static ResourceNode Rock(int x, int y) => new() { Id = NodeId(x, y, ResourceType.Rock), Type = ResourceType.Rock, Tile = new TilePoint(x, y), Health = 30, MaxHealth = 30, SpriteId = "rocks", Source = new Rectangle(0, 0, 64, 64), YieldItem = ItemId.Stone, YieldAmount = 4 };
    private ResourceNode Ore(int x, int y, double roll) => roll > 0.94 ? Crystal(x, y) : new ResourceNode { Id = NodeId(x, y, ResourceType.IronOre), Type = ResourceType.IronOre, Tile = new TilePoint(x, y), Health = 42, MaxHealth = 42, SpriteId = "resources", Source = new Rectangle(80, 0, 80, 80), YieldItem = ItemId.IronOre, YieldAmount = 3 };
    private static ResourceNode Crystal(int x, int y) => new() { Id = NodeId(x, y, ResourceType.CrystalDeposit), Type = ResourceType.CrystalDeposit, Tile = new TilePoint(x, y), Health = 55, MaxHealth = 55, SpriteId = "resources", Source = new Rectangle(160, 0, 80, 80), YieldItem = ItemId.Crystal, YieldAmount = 2 };
    private static ResourceNode Gold(int x, int y) => new() { Id = NodeId(x, y, ResourceType.GoldVein), Type = ResourceType.GoldVein, Tile = new TilePoint(x, y), Health = 50, MaxHealth = 50, SpriteId = "resources", Source = new Rectangle(240, 0, 80, 80), YieldItem = ItemId.Gold, YieldAmount = 2 };
}
