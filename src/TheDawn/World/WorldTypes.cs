using Microsoft.Xna.Framework;
using TheDawn.Data;

namespace TheDawn.World;

public readonly record struct TilePoint(int X, int Y)
{
    public Vector2 CenterWorld => new((X + 0.5f) * GameConfig.TileSize, (Y + 0.5f) * GameConfig.TileSize);
}

public sealed class ResourceNode
{
    public long Id { get; init; }
    public ResourceType Type { get; init; }
    public TilePoint Tile { get; init; }
    public int Health { get; set; }
    public int MaxHealth { get; init; }
    public string SpriteId { get; init; } = "tree_a";
    public Rectangle Source { get; init; }
    public int YieldAmount { get; init; }
    public ItemId YieldItem { get; init; }
    public bool IsDepleted => Health <= 0;
}

public sealed class WorldDecoration
{
    public DecorationType Type { get; init; }
    public TilePoint Tile { get; init; }
    public Vector2 Offset { get; init; }
    public int Variant { get; init; }
    public bool Flip { get; init; }
    public float Scale { get; init; } = 1f;
}

public sealed class Structure
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public StructureType Type { get; init; }
    public TilePoint Tile { get; init; }
    public int Health { get; set; }
    public int MaxHealth { get; init; }
    public int Growth { get; set; }
    public bool IsDestroyed => Health <= 0;
}

public sealed class Chunk
{
    public int ChunkX { get; }
    public int ChunkY { get; }
    public TileType[] Tiles { get; }
    public List<ResourceNode> Nodes { get; } = new();
    public List<WorldDecoration> Decorations { get; } = new();

    public Chunk(int chunkX, int chunkY)
    {
        ChunkX = chunkX;
        ChunkY = chunkY;
        Tiles = new TileType[GameConfig.ChunkSize * GameConfig.ChunkSize];
    }

    public TileType GetLocal(int x, int y) => Tiles[y * GameConfig.ChunkSize + x];
    public void SetLocal(int x, int y, TileType type) => Tiles[y * GameConfig.ChunkSize + x] = type;
}
