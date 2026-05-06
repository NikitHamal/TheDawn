using Microsoft.Xna.Framework;
using TheDawn.Data;
using TheDawn.Entities;
using TheDawn.World;

namespace TheDawn.Systems;

public sealed class RaidDirector
{
    private readonly int _worldSeed;
    private int _activeNightDay;
    private readonly Queue<RaidSpawn> _schedule = new();
    private double _nightClock;
    private bool _nightActive;

    public RaidDirector(int worldSeed) => _worldSeed = worldSeed;

    public bool IsActive => _nightActive;

    public void BeginNight(int day)
    {
        _nightActive = true;
        _nightClock = 0;
        _activeNightDay = day;
        _schedule.Clear();
        foreach (var spawn in BuildSchedule(day)) _schedule.Enqueue(spawn);
    }

    public void EndNight() => _nightActive = false;

    public void Update(double seconds, GameWorld world, Vector2 baseCenter, List<Enemy> enemies)
    {
        if (!_nightActive) return;
        _nightClock += seconds;
        while (_schedule.Count > 0 && _schedule.Peek().AtSecond <= _nightClock)
        {
            var spawn = _schedule.Dequeue();
            for (var i = 0; i < spawn.Count; i++)
            {
                var pos = ChooseSpawnPosition(world, baseCenter, spawn.CountIndexSalt + i);
                enemies.Add(new Enemy(spawn.Type, pos));
            }
        }
    }

    private IEnumerable<RaidSpawn> BuildSchedule(int day)
    {
        var rng = new Random(unchecked(_worldSeed * 397 ^ day * 193));
        if (day <= 5)
        {
            yield return new RaidSpawn(0, EnemyType.SkeletonRogue, rng.Next(3, 6), 1000);
            yield break;
        }
        if (day <= 15)
        {
            var total = rng.Next(8, 16);
            yield return new RaidSpawn(0, EnemyType.SkeletonRogue, total / 2, 2000);
            yield return new RaidSpawn(35, EnemyType.OrcRogue, total - total / 2 - 1, 3000);
            if (day >= 8) yield return new RaidSpawn(65, EnemyType.SkeletonMage, 1, 4000);
            yield break;
        }
        if (day <= 30)
        {
            yield return new RaidSpawn(0, EnemyType.OrcWarrior, 5 + day / 6, 5000);
            yield return new RaidSpawn(20, EnemyType.SkeletonArcher, 4 + day / 8, 6000);
            yield return new RaidSpawn(55, EnemyType.OrcShaman, 1 + day / 24, 7000);
            yield break;
        }
        if (day <= 60)
        {
            var waves = day < 45 ? 2 : 3;
            for (var wave = 0; wave < waves; wave++)
            {
                var offset = wave * 80;
                yield return new RaidSpawn(offset, EnemyType.OrcWarrior, 6 + day / 8, 8000 + wave * 100);
                yield return new RaidSpawn(offset + 30, EnemyType.SkeletonMage, 2 + day / 20, 8100 + wave * 100);
                yield return new RaidSpawn(offset + 50, EnemyType.RaidLeader, 1, 8200 + wave * 100);
            }
            if (day % 10 == 0) yield return new RaidSpawn(170, EnemyType.DungeonBoss, 1, 9000);
            yield break;
        }
        for (var wave = 0; wave < 4; wave++)
        {
            var offset = wave * 70;
            yield return new RaidSpawn(offset, EnemyType.OrcWarrior, 10 + day / 12, 10000 + wave * 100);
            yield return new RaidSpawn(offset + 16, EnemyType.SkeletonMage, 4 + day / 30, 10100 + wave * 100);
            yield return new RaidSpawn(offset + 36, EnemyType.OrcShaman, 3 + day / 40, 10200 + wave * 100);
            yield return new RaidSpawn(offset + 54, EnemyType.RaidLeader, 1 + day / 90, 10300 + wave * 100);
        }
        if (day % 5 == 0) yield return new RaidSpawn(230, EnemyType.DungeonBoss, 1, 11000);
    }

    private Vector2 ChooseSpawnPosition(GameWorld world, Vector2 baseCenter, int salt)
    {
        var dungeon = world.DungeonEntrance.CenterWorld;
        var direction = baseCenter - dungeon;
        if (direction.LengthSquared() < 1) direction = new Vector2(1, 0);
        direction.Normalize();
        var perp = new Vector2(-direction.Y, direction.X);
        var offset = ((int)ValueNoise.Hash(_worldSeed, _activeNightDay, salt) % 13 - 6) * GameConfig.TileSize;
        var candidate = baseCenter - direction * (26 * GameConfig.TileSize) + perp * offset;
        var tile = GameWorld.WorldToTile(candidate);
        for (var r = 0; r < 8; r++)
        {
            for (var y = -r; y <= r; y++)
            for (var x = -r; x <= r; x++)
            {
                if (Math.Abs(x) != r && Math.Abs(y) != r) continue;
                var tx = tile.X + x;
                var ty = tile.Y + y;
                if (world.IsPassable(tx, ty, false)) return GameWorld.TileToWorldCenter(tx, ty);
            }
        }
        return GameWorld.TileToWorldCenter(tile.X, tile.Y);
    }

    private readonly record struct RaidSpawn(double AtSecond, EnemyType Type, int Count, int CountIndexSalt);
}
