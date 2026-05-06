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
        var radius = 168 + (int)(ValueNoise.HashUnit(seed, 34, 77) * 96);
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
                chunk.SetLocal(lx, ly, GenerateTile(gx, gy));
            }
        }
        GenerateNodes(chunk);
        return chunk;
    }

    private TileType GenerateTile(int x, int y)
    {
        var dist = Math.Sqrt((double)x * x + (double)y * y);
        var dungeonDist = Math.Abs(x - DungeonEntrance.X) + Math.Abs(y - DungeonEntrance.Y);
        if (dungeonDist <= 10) return TileType.DungeonFloor;

        var warpX = (ValueNoise.Fractal(_seed + 1701, x, y, 0.012, 3, 0.52) - 0.5) * 42.0;
        var warpY = (ValueNoise.Fractal(_seed + 1702, x, y, 0.012, 3, 0.52) - 0.5) * 42.0;
        var wx = x + warpX;
        var wy = y + warpY;

        var elevation = ValueNoise.Fractal(_seed + 101, wx, wy, 0.0046, 5, 0.54);
        var moisture = ValueNoise.Fractal(_seed + 202, wx, wy, 0.0065, 4, 0.56);
        var temperature = ValueNoise.Fractal(_seed + 303, wx, wy, 0.0025, 4, 0.55);

        var riverA = Math.Abs(ValueNoise.Fractal(_seed + 404, wx, wy, 0.0060, 4, 0.50) - 0.5);
        var riverB = Math.Abs(ValueNoise.Fractal(_seed + 405, wx + 1900, wy - 1700, 0.0053, 4, 0.50) - 0.5);
        var riverWidth = 0.0095 + moisture * 0.0065;
        var isRiver = dist > GameConfig.SpawnSafeRadiusTiles + 4 && elevation > 0.33 && Math.Min(riverA, riverB) < riverWidth;
        var isLake = dist > 90 && elevation < 0.245 && moisture > 0.62;
        if (isRiver || isLake) return TileType.Water;

        if (dist > 760 && temperature < 0.37 && elevation > 0.45) return TileType.Snow;

        var caveBand = ValueNoise.Fractal(_seed + 606, wx, wy, 0.011, 4, 0.53);
        if (dist > 110 && elevation > 0.58 && caveBand > 0.705) return TileType.CaveFloor;

        if (IsRuinFloor(x, y, dist)) return TileType.RuinFloor;

        var stoneBand = ValueNoise.Fractal(_seed + 707, wx, wy, 0.018, 3, 0.48);
        if (dist > 80 && elevation > 0.70 && stoneBand > 0.64) return TileType.Stone;

        var dirtField = ValueNoise.Fractal(_seed + 808, wx, wy, 0.030, 3, 0.55);
        var nearDungeonTrail = dungeonDist < 34 + (int)(ValueNoise.Noise2D(_seed + 809, x, y, 0.04) * 8.0);
        if (dirtField > 0.735 || nearDungeonTrail) return TileType.Dirt;

        return TileType.Grass;
    }

    private bool IsRuinFloor(int x, int y, double dist)
    {
        if (dist < 320) return false;
        const int cell = 96;
        var cx = FloorDiv(x, cell);
        var cy = FloorDiv(y, cell);
        for (var oy = -1; oy <= 1; oy++)
        {
            for (var ox = -1; ox <= 1; ox++)
            {
                var candidateX = cx + ox;
                var candidateY = cy + oy;
                if (ValueNoise.HashUnit(_seed + 909, candidateX, candidateY) < 0.86) continue;
                var centerX = candidateX * cell + 20 + (int)(ValueNoise.HashUnit(_seed + 910, candidateX, candidateY) * (cell - 40));
                var centerY = candidateY * cell + 20 + (int)(ValueNoise.HashUnit(_seed + 911, candidateX, candidateY) * (cell - 40));
                var dx = x - centerX;
                var dy = y - centerY;
                var radius = 10 + (int)(ValueNoise.HashUnit(_seed + 912, candidateX, candidateY) * 10);
                if (dx * dx + dy * dy < radius * radius) return true;
            }
        }
        return false;
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

                var roll = ValueNoise.HashUnit(_seed + 1200, gx, gy);
                var forest = ValueNoise.Fractal(_seed + 1210, gx, gy, 0.032, 3, 0.55);
                var resourceVein = ValueNoise.Fractal(_seed + 1220, gx, gy, 0.052, 2, 0.52);
                ResourceNode? node = tile switch
                {
                    TileType.Grass when forest > 0.50 && roll > 0.805 => Tree(gx, gy, roll),
                    TileType.Grass when forest <= 0.50 && roll > 0.930 => Tree(gx, gy, roll),
                    TileType.Grass when roll is > 0.735 and <= 0.785 => Bush(gx, gy),
                    TileType.Grass when roll is > 0.690 and <= 0.715 => WildCrop(gx, gy),
                    TileType.Dirt when roll > 0.910 => WildCrop(gx, gy),
                    TileType.Dirt when roll is > 0.835 and <= 0.885 => Rock(gx, gy),
                    TileType.Stone when roll > 0.790 => Rock(gx, gy),
                    TileType.CaveFloor when roll > 0.805 && resourceVein > 0.52 => Ore(gx, gy, roll),
                    TileType.Snow when roll > 0.875 => Crystal(gx, gy),
                    TileType.RuinFloor when roll > 0.865 => RuinLoot(gx, gy, roll),
                    _ => null
                };
                if (node != null && !AdjacentNodeExists(chunk, lx, ly, node.Type)) chunk.Nodes.Add(node);
            }
        }
    }

    private static bool AdjacentNodeExists(Chunk chunk, int lx, int ly, ResourceType type)
    {
        for (var y = Math.Max(0, ly - 1); y <= Math.Min(GameConfig.ChunkSize - 1, ly + 1); y++)
        for (var x = Math.Max(0, lx - 1); x <= Math.Min(GameConfig.ChunkSize - 1, lx + 1); x++)
        {
            if (chunk.Nodes.Any(n => n.Type == type && n.Tile.X == chunk.ChunkX * GameConfig.ChunkSize + x && n.Tile.Y == chunk.ChunkY * GameConfig.ChunkSize + y)) return true;
        }
        return false;
    }

    private static int FloorDiv(int value, int divisor)
    {
        var result = value / divisor;
        var rem = value % divisor;
        if ((rem != 0) && ((rem < 0) != (divisor < 0))) result--;
        return result;
    }

    private static long NodeId(int x, int y, ResourceType type) => ((long)x & 0x1FFFFFL) << 42 | (((long)y & 0x1FFFFFL) << 21) | (long)type;

    private ResourceNode Tree(int x, int y, double roll)
    {
        var variant = (int)(ValueNoise.Hash(_seed + 1300, x, y) % 6);
        return new ResourceNode
        {
            Id = NodeId(x, y, ResourceType.Tree) | ((long)variant << 8),
            Type = ResourceType.Tree,
            Tile = new TilePoint(x, y),
            Health = 34,
            MaxHealth = 34,
            SpriteId = "objects_atlas",
            Source = Rectangle.Empty,
            YieldItem = ItemId.Wood,
            YieldAmount = 5 + (roll > 0.93 ? 2 : 0)
        };
    }

    private static ResourceNode Bush(int x, int y) => Node(x, y, ResourceType.BerryBush, 10, ItemId.Food, 3);
    private static ResourceNode WildCrop(int x, int y) => Node(x, y, ResourceType.WildCrop, 8, ItemId.Seed, 2);
    private static ResourceNode Rock(int x, int y) => Node(x, y, ResourceType.Rock, 30, ItemId.Stone, 4);
    private ResourceNode Ore(int x, int y, double roll) => roll > 0.945 ? Crystal(x, y) : Node(x, y, ResourceType.IronOre, 42, ItemId.IronOre, 3);
    private static ResourceNode Crystal(int x, int y) => Node(x, y, ResourceType.CrystalDeposit, 55, ItemId.Crystal, 2);
    private static ResourceNode RuinLoot(int x, int y, double roll) => roll > 0.945 ? Node(x, y, ResourceType.GoldVein, 50, ItemId.Gold, 2) : Rock(x, y);

    private static ResourceNode Node(int x, int y, ResourceType type, int health, ItemId yield, int amount) => new()
    {
        Id = NodeId(x, y, type),
        Type = type,
        Tile = new TilePoint(x, y),
        Health = health,
        MaxHealth = health,
        SpriteId = "objects_atlas",
        Source = Rectangle.Empty,
        YieldItem = yield,
        YieldAmount = amount
    };
}
