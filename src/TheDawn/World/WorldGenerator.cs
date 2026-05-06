using Microsoft.Xna.Framework;
using TheDawn.Data;

namespace TheDawn.World;

public sealed class WorldGenerator
{
    private readonly int _seed;
    private readonly bool _primaryRiverRunsNorthSouth;
    private readonly double _primaryRiverOffset;
    public TilePoint DungeonEntrance { get; }

    public WorldGenerator(int seed)
    {
        _seed = seed;
        _primaryRiverRunsNorthSouth = (ValueNoise.Hash(seed, 7, 17) & 1) == 0;
        _primaryRiverOffset = -42.0 + ValueNoise.HashUnit(seed, 24, 91) * 84.0;
        var angle = ValueNoise.HashUnit(seed, 91, 11) * Math.PI * 2.0;
        var radius = 172 + (int)(ValueNoise.HashUnit(seed, 34, 77) * 110);
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
        GenerateDecorations(chunk);
        GenerateNodes(chunk);
        return chunk;
    }

    private TileType GenerateTile(int x, int y)
    {
        var dist = Math.Sqrt((double)x * x + (double)y * y);
        var dungeonDist = Math.Abs(x - DungeonEntrance.X) + Math.Abs(y - DungeonEntrance.Y);
        if (dungeonDist <= 11) return TileType.DungeonFloor;

        var warpX = (ValueNoise.Fractal(_seed + 1701, x, y, 0.011, 3, 0.52) - 0.5) * 50.0;
        var warpY = (ValueNoise.Fractal(_seed + 1702, x, y, 0.011, 3, 0.52) - 0.5) * 50.0;
        var wx = x + warpX;
        var wy = y + warpY;

        var elevation = ValueNoise.Fractal(_seed + 101, wx, wy, 0.0044, 5, 0.54);
        var moisture = ValueNoise.Fractal(_seed + 202, wx, wy, 0.0064, 4, 0.56);
        var temperature = ValueNoise.Fractal(_seed + 303, wx, wy, 0.0022, 4, 0.55);

        // Every seed receives one named strategic river near spawn, then secondary rivers and lakes.
        // The field is deterministic and continuous, which lets rivers function as reliable natural walls.
        var primaryRiverDistance = PrimaryRiverDistance(x, y);
        var primaryRiverWidth = 2.3 + ValueNoise.Fractal(_seed + 430, x, y, 0.018, 2, 0.45) * 1.8;
        if (dist > GameConfig.SpawnSafeRadiusTiles + 4 && primaryRiverDistance < primaryRiverWidth) return TileType.Water;

        var riverA = Math.Abs(ValueNoise.Fractal(_seed + 404, wx, wy, 0.0060, 4, 0.50) - 0.5);
        var riverB = Math.Abs(ValueNoise.Fractal(_seed + 405, wx + 1900, wy - 1700, 0.0053, 4, 0.50) - 0.5);
        var secondaryRiverWidth = 0.0080 + moisture * 0.0070;
        var isRiver = dist > GameConfig.SpawnSafeRadiusTiles + 5 && elevation > 0.34 && Math.Min(riverA, riverB) < secondaryRiverWidth;
        var isLake = dist > 95 && elevation < 0.247 && moisture > 0.61;
        if (isRiver || isLake) return TileType.Water;

        if (dist > 690 && temperature < 0.38 && elevation > 0.43) return TileType.Snow;

        var caveDist = DistanceToCaveSpine(x, y);
        var caveBand = ValueNoise.Fractal(_seed + 606, wx, wy, 0.011, 4, 0.53);
        if ((dist > 82 && dist < 560 && caveDist < 4.0) || (dist > 115 && elevation > 0.58 && caveBand > 0.705)) return TileType.CaveFloor;

        if (IsRuinFloor(x, y, dist)) return TileType.RuinFloor;

        var stoneBand = ValueNoise.Fractal(_seed + 707, wx, wy, 0.017, 3, 0.48);
        if (dist > 76 && elevation > 0.70 && stoneBand > 0.61) return TileType.Stone;

        var dirtField = ValueNoise.Fractal(_seed + 808, wx, wy, 0.030, 3, 0.55);
        var nearDungeonTrail = dungeonDist < 30 + (int)(ValueNoise.Noise2D(_seed + 809, x, y, 0.04) * 12.0);
        var nearCaveTrail = caveDist < 7.2 && dist > 72;
        if (dirtField > 0.742 || nearDungeonTrail || nearCaveTrail) return TileType.Dirt;

        return TileType.Grass;
    }

    private double PrimaryRiverDistance(int x, int y)
    {
        if (_primaryRiverRunsNorthSouth)
        {
            var pathX = _primaryRiverOffset + Math.Sin(y * 0.035 + _seed * 0.013) * 18.0 + (ValueNoise.Fractal(_seed + 411, 0, y, 0.012, 3, 0.55) - 0.5) * 34.0;
            return Math.Abs(x - pathX);
        }
        var pathY = _primaryRiverOffset + Math.Cos(x * 0.035 + _seed * 0.013) * 18.0 + (ValueNoise.Fractal(_seed + 412, x, 0, 0.012, 3, 0.55) - 0.5) * 34.0;
        return Math.Abs(y - pathY);
    }

    private double DistanceToCaveSpine(int x, int y)
    {
        var angle = ValueNoise.HashUnit(_seed, 61, 22) * Math.PI * 2.0;
        var nx = Math.Cos(angle);
        var ny = Math.Sin(angle);
        var along = x * nx + y * ny;
        var perp = Math.Abs(-x * ny + y * nx);
        var waviness = (ValueNoise.Fractal(_seed + 613, along, 0, 0.010, 3, 0.55) - 0.5) * 20.0;
        return Math.Abs(perp - 86.0 - waviness);
    }

    private bool IsRuinFloor(int x, int y, double dist)
    {
        if (dist < 300) return false;
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
                var centerX = candidateX * cell + 18 + (int)(ValueNoise.HashUnit(_seed + 910, candidateX, candidateY) * (cell - 36));
                var centerY = candidateY * cell + 18 + (int)(ValueNoise.HashUnit(_seed + 911, candidateX, candidateY) * (cell - 36));
                var dx = x - centerX;
                var dy = y - centerY;
                var radius = 10 + (int)(ValueNoise.HashUnit(_seed + 912, candidateX, candidateY) * 11);
                if (dx * dx + dy * dy < radius * radius) return true;
            }
        }
        return false;
    }

    private void GenerateDecorations(Chunk chunk)
    {
        for (var ly = 0; ly < GameConfig.ChunkSize; ly++)
        {
            for (var lx = 0; lx < GameConfig.ChunkSize; lx++)
            {
                var gx = chunk.ChunkX * GameConfig.ChunkSize + lx;
                var gy = chunk.ChunkY * GameConfig.ChunkSize + ly;
                var tile = chunk.GetLocal(lx, ly);
                var hash = ValueNoise.HashUnit(_seed + 1600, gx, gy);
                var detail = ValueNoise.Fractal(_seed + 1601, gx, gy, 0.065, 2, 0.50);
                var offset = new Vector2((float)((ValueNoise.HashUnit(_seed + 1610, gx, gy) - 0.5) * 18.0), (float)((ValueNoise.HashUnit(_seed + 1611, gx, gy) - 0.5) * 18.0));
                var flip = ValueNoise.HashUnit(_seed + 1612, gx, gy) > 0.5;
                var variant = (int)(ValueNoise.Hash(_seed + 1613, gx, gy) & 15);
                var scale = 0.85f + (float)ValueNoise.HashUnit(_seed + 1614, gx, gy) * 0.30f;
                DecorationType? type = tile switch
                {
                    TileType.Grass when detail > 0.60 && hash > 0.22 => DecorationType.GrassTuft,
                    TileType.Grass when detail > 0.50 && hash is > 0.12 and <= 0.22 => DecorationType.Fern,
                    TileType.Grass when detail > 0.48 && hash is > 0.06 and <= 0.12 => DecorationType.Flower,
                    TileType.Grass when detail < 0.28 && hash < 0.045 => DecorationType.Mushroom,
                    TileType.Dirt when hash > 0.72 => DecorationType.Branch,
                    TileType.Dirt when hash < 0.10 => DecorationType.Pebble,
                    TileType.Stone when hash > 0.58 => DecorationType.Pebble,
                    TileType.CaveFloor when hash > 0.56 => DecorationType.CaveDebris,
                    TileType.DungeonFloor when hash > 0.55 => DecorationType.DungeonRelic,
                    TileType.RuinFloor when hash > 0.36 => DecorationType.RuinDebris,
                    TileType.Snow when hash > 0.50 => DecorationType.SnowClump,
                    TileType.Water when IsEdgeWater(chunk, lx, ly) && hash > 0.50 => DecorationType.WaterFoam,
                    _ => null
                };
                if (type == null) continue;
                chunk.Decorations.Add(new WorldDecoration
                {
                    Type = type.Value,
                    Tile = new TilePoint(gx, gy),
                    Offset = offset,
                    Variant = variant,
                    Flip = flip,
                    Scale = scale
                });
            }
        }
    }

    private static bool IsEdgeWater(Chunk chunk, int lx, int ly)
    {
        if (chunk.GetLocal(lx, ly) != TileType.Water) return false;
        for (var y = -1; y <= 1; y++)
        for (var x = -1; x <= 1; x++)
        {
            if (x == 0 && y == 0) continue;
            var nx = lx + x;
            var ny = ly + y;
            if (nx < 0 || ny < 0 || nx >= GameConfig.ChunkSize || ny >= GameConfig.ChunkSize) continue;
            if (chunk.GetLocal(nx, ny) != TileType.Water) return true;
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
                var spawnDistance = Math.Sqrt((double)gx * gx + (double)gy * gy);
                if (spawnDistance < 4.5) continue;

                var tile = chunk.GetLocal(lx, ly);
                if (tile == TileType.Water || tile == TileType.DungeonFloor) continue;

                var roll = ValueNoise.HashUnit(_seed + 1200, gx, gy);
                var forest = ValueNoise.Fractal(_seed + 1210, gx, gy, 0.031, 3, 0.55);
                var resourceVein = ValueNoise.Fractal(_seed + 1220, gx, gy, 0.052, 2, 0.52);
                var starterRing = spawnDistance >= 5.0 && spawnDistance <= 24.0;
                ResourceNode? node = tile switch
                {
                    TileType.Grass when starterRing && forest > 0.41 && roll > 0.705 => Tree(gx, gy, roll),
                    TileType.Grass when forest > 0.55 && roll > 0.770 => Tree(gx, gy, roll),
                    TileType.Grass when forest <= 0.55 && roll > 0.920 => Tree(gx, gy, roll),
                    TileType.Grass when roll is > 0.735 and <= 0.785 => Bush(gx, gy),
                    TileType.Grass when starterRing && roll is > 0.630 and <= 0.675 => Bush(gx, gy),
                    TileType.Grass when roll is > 0.690 and <= 0.722 => WildCrop(gx, gy),
                    TileType.Dirt when roll > 0.902 => WildCrop(gx, gy),
                    TileType.Dirt when roll is > 0.828 and <= 0.890 => Rock(gx, gy),
                    TileType.Stone when roll > 0.770 => Rock(gx, gy),
                    TileType.CaveFloor when roll > 0.780 && resourceVein > 0.50 => Ore(gx, gy, roll),
                    TileType.Snow when roll > 0.855 => Crystal(gx, gy),
                    TileType.RuinFloor when roll > 0.835 => RuinLoot(gx, gy, roll),
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
        var variant = (int)(ValueNoise.Hash(_seed + 1300, x, y) % 10);
        return new ResourceNode
        {
            Id = NodeId(x, y, ResourceType.Tree) | ((long)variant << 8),
            Type = ResourceType.Tree,
            Tile = new TilePoint(x, y),
            Health = 34 + (variant is 2 or 7 ? 8 : 0),
            MaxHealth = 34 + (variant is 2 or 7 ? 8 : 0),
            SpriteId = "source-direct",
            Source = Rectangle.Empty,
            YieldItem = ItemId.Wood,
            YieldAmount = 5 + (roll > 0.93 ? 2 : 0) + (variant is 2 or 7 ? 1 : 0)
        };
    }

    private static ResourceNode Bush(int x, int y) => Node(x, y, ResourceType.BerryBush, 10, ItemId.Food, 3);
    private static ResourceNode WildCrop(int x, int y) => Node(x, y, ResourceType.WildCrop, 8, ItemId.Seed, 2);
    private static ResourceNode Rock(int x, int y) => Node(x, y, ResourceType.Rock, 30, ItemId.Stone, 4);
    private ResourceNode Ore(int x, int y, double roll) => roll > 0.942 ? Crystal(x, y) : Node(x, y, ResourceType.IronOre, 42, ItemId.IronOre, 3);
    private static ResourceNode Crystal(int x, int y) => Node(x, y, ResourceType.CrystalDeposit, 55, ItemId.Crystal, 2);
    private static ResourceNode RuinLoot(int x, int y, double roll) => roll > 0.936 ? Node(x, y, ResourceType.GoldVein, 50, ItemId.Gold, 2) : Rock(x, y);

    private static ResourceNode Node(int x, int y, ResourceType type, int health, ItemId yield, int amount) => new()
    {
        Id = NodeId(x, y, type),
        Type = type,
        Tile = new TilePoint(x, y),
        Health = health,
        MaxHealth = health,
        SpriteId = "source-direct",
        Source = Rectangle.Empty,
        YieldItem = yield,
        YieldAmount = amount
    };
}
