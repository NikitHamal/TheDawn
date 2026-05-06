using Microsoft.Xna.Framework;
using TheDawn.Data;

namespace TheDawn.World;

public sealed class GameWorld
{
    private readonly Dictionary<(int X, int Y), Chunk> _chunks = new();
    private readonly Queue<(int X, int Y)> _loadOrder = new();
    private readonly WorldGenerator _generator;

    public int Seed { get; }
    public TilePoint DungeonEntrance => _generator.DungeonEntrance;
    public HashSet<long> RemovedNodeIds { get; } = new();
    public List<Structure> Structures { get; } = new();

    public GameWorld(int seed)
    {
        Seed = seed;
        _generator = new WorldGenerator(seed);
    }

    public void Warm(Vector2 center, int radiusChunks)
    {
        var cx = FloorDiv((int)MathF.Floor(center.X / GameConfig.TileSize), GameConfig.ChunkSize);
        var cy = FloorDiv((int)MathF.Floor(center.Y / GameConfig.TileSize), GameConfig.ChunkSize);
        for (var y = cy - radiusChunks; y <= cy + radiusChunks; y++)
        for (var x = cx - radiusChunks; x <= cx + radiusChunks; x++) GetChunk(x, y);
    }

    public void TrimAround(Vector2 center)
    {
        if (_chunks.Count <= GameConfig.MaxLoadedChunks) return;
        var centerTile = WorldToTile(center);
        var centerChunk = (X: FloorDiv(centerTile.X, GameConfig.ChunkSize), Y: FloorDiv(centerTile.Y, GameConfig.ChunkSize));
        var remove = _chunks.Keys
            .Where(k => Math.Abs(k.X - centerChunk.X) > GameConfig.ActiveChunkRadius + 1 || Math.Abs(k.Y - centerChunk.Y) > GameConfig.ActiveChunkRadius + 1)
            .Take(Math.Max(0, _chunks.Count - GameConfig.MaxLoadedChunks))
            .ToList();
        foreach (var key in remove) _chunks.Remove(key);
    }

    public TileType GetTile(int tileX, int tileY)
    {
        var chunk = GetChunk(FloorDiv(tileX, GameConfig.ChunkSize), FloorDiv(tileY, GameConfig.ChunkSize));
        var lx = Mod(tileX, GameConfig.ChunkSize);
        var ly = Mod(tileY, GameConfig.ChunkSize);
        return chunk.GetLocal(lx, ly);
    }

    public bool IsWater(int tileX, int tileY) => GetTile(tileX, tileY) == TileType.Water;

    public bool IsPassable(int tileX, int tileY, bool unitsCanPassGates = false)
    {
        var tile = GetTile(tileX, tileY);
        if (tile == TileType.Water) return false;
        foreach (var structure in Structures)
        {
            if (structure.IsDestroyed || structure.Tile.X != tileX || structure.Tile.Y != tileY) continue;
            var def = GameBalance.Structures[structure.Type];
            if (!def.BlocksMovement) continue;
            if (unitsCanPassGates && structure.Type == StructureType.Gate) continue;
            return false;
        }
        return true;
    }

    public IEnumerable<ResourceNode> NodesIn(Rectangle worldBounds)
    {
        var min = WorldToTile(new Vector2(worldBounds.Left, worldBounds.Top));
        var max = WorldToTile(new Vector2(worldBounds.Right, worldBounds.Bottom));
        var minCx = FloorDiv(min.X, GameConfig.ChunkSize);
        var maxCx = FloorDiv(max.X, GameConfig.ChunkSize);
        var minCy = FloorDiv(min.Y, GameConfig.ChunkSize);
        var maxCy = FloorDiv(max.Y, GameConfig.ChunkSize);
        for (var cy = minCy; cy <= maxCy; cy++)
        for (var cx = minCx; cx <= maxCx; cx++)
        {
            var chunk = GetChunk(cx, cy);
            foreach (var node in chunk.Nodes)
            {
                if (!RemovedNodeIds.Contains(node.Id) && !node.IsDepleted) yield return node;
            }
        }
    }

    public ResourceNode? FindNodeNear(Vector2 position, float radius)
    {
        var rect = new Rectangle((int)(position.X - radius), (int)(position.Y - radius), (int)(radius * 2), (int)(radius * 2));
        ResourceNode? best = null;
        var bestDist = radius * radius;
        foreach (var node in NodesIn(rect))
        {
            var d = Vector2.DistanceSquared(position, node.Tile.CenterWorld);
            if (d < bestDist)
            {
                best = node;
                bestDist = d;
            }
        }
        return best;
    }

    public Structure? StructureAt(int tileX, int tileY)
        => Structures.FirstOrDefault(s => !s.IsDestroyed && s.Tile.X == tileX && s.Tile.Y == tileY);

    public Structure? FindStructureNear(Vector2 position, float radius)
    {
        Structure? best = null;
        var bestDist = radius * radius;
        foreach (var structure in Structures)
        {
            if (structure.IsDestroyed) continue;
            var d = Vector2.DistanceSquared(position, structure.Tile.CenterWorld);
            if (d < bestDist)
            {
                best = structure;
                bestDist = d;
            }
        }
        return best;
    }

    public bool CanPlaceStructure(StructureType type, TilePoint tile)
    {
        if (IsWater(tile.X, tile.Y)) return false;
        if (StructureAt(tile.X, tile.Y) != null) return false;
        var existingResource = FindNodeAt(tile.X, tile.Y);
        if (existingResource != null) return false;
        return true;
    }

    public ResourceNode? FindNodeAt(int tileX, int tileY)
    {
        var chunk = GetChunk(FloorDiv(tileX, GameConfig.ChunkSize), FloorDiv(tileY, GameConfig.ChunkSize));
        return chunk.Nodes.FirstOrDefault(n => n.Tile.X == tileX && n.Tile.Y == tileY && !RemovedNodeIds.Contains(n.Id) && !n.IsDepleted);
    }

    public void RemoveNode(ResourceNode node) => RemovedNodeIds.Add(node.Id);

    public static TilePoint WorldToTile(Vector2 world) => new((int)MathF.Floor(world.X / GameConfig.TileSize), (int)MathF.Floor(world.Y / GameConfig.TileSize));

    public static Vector2 TileToWorldCenter(int tileX, int tileY) => new((tileX + 0.5f) * GameConfig.TileSize, (tileY + 0.5f) * GameConfig.TileSize);

    private Chunk GetChunk(int cx, int cy)
    {
        var key = (cx, cy);
        if (_chunks.TryGetValue(key, out var chunk)) return chunk;
        chunk = _generator.Generate(cx, cy);
        _chunks[key] = chunk;
        _loadOrder.Enqueue(key);
        return chunk;
    }

    public static int FloorDiv(int value, int divisor)
    {
        var result = value / divisor;
        var rem = value % divisor;
        if ((rem != 0) && ((rem < 0) != (divisor < 0))) result--;
        return result;
    }

    public static int Mod(int value, int divisor)
    {
        var result = value % divisor;
        return result < 0 ? result + divisor : result;
    }
}
