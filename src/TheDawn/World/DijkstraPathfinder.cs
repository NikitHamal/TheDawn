using Microsoft.Xna.Framework;

namespace TheDawn.World;

public sealed class DijkstraPathfinder
{
    private static readonly TilePoint[] Neighbors =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
    };

    public TilePoint? NextStep(GameWorld world, TilePoint start, TilePoint target, int maxNodes, bool unitsCanPassGates)
    {
        if (start.Equals(target)) return null;
        var cameFrom = new Dictionary<TilePoint, TilePoint>();
        var visited = new HashSet<TilePoint> { start };
        var queue = new Queue<TilePoint>();
        queue.Enqueue(start);
        var expanded = 0;
        while (queue.Count > 0 && expanded < maxNodes)
        {
            expanded++;
            var current = queue.Dequeue();
            foreach (var n in Neighbors)
            {
                var next = new TilePoint(current.X + n.X, current.Y + n.Y);
                if (visited.Contains(next)) continue;
                if (!next.Equals(target) && !world.IsPassable(next.X, next.Y, unitsCanPassGates)) continue;
                visited.Add(next);
                cameFrom[next] = current;
                if (next.Equals(target)) return ReconstructFirstStep(start, target, cameFrom);
                queue.Enqueue(next);
            }
        }
        return null;
    }

    private static TilePoint? ReconstructFirstStep(TilePoint start, TilePoint target, Dictionary<TilePoint, TilePoint> cameFrom)
    {
        var current = target;
        while (cameFrom.TryGetValue(current, out var parent))
        {
            if (parent.Equals(start)) return current;
            current = parent;
        }
        return null;
    }
}
