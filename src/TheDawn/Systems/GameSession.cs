using Microsoft.Xna.Framework;
using TheDawn.Data;
using TheDawn.Entities;
using TheDawn.Persistence;
using TheDawn.World;

namespace TheDawn.Systems;

public sealed class GameSession
{
    private readonly DijkstraPathfinder _pathfinder = new();
    private readonly CraftingSystem _crafting = new();
    private readonly RaidDirector _raidDirector;
    private float _messageTime;
    private float _autosaveTimer;

    public GameWorld World { get; }
    public Player Player { get; }
    public TimeSystem Time { get; } = new();
    public List<Enemy> Enemies { get; } = new();
    public List<HiredUnit> Units { get; } = new();
    public List<Projectile> Projectiles { get; } = new();
    public int SelectedBuildIndex { get; private set; }
    public int SelectedHireIndex { get; private set; }
    public bool BuildMode { get; private set; }
    public string Message { get; private set; } = "Find river bends. Build before dusk. Survive the dawn.";
    public bool IsPermadead { get; private set; }

    public StructureType SelectedStructure => GameBalance.BuildOrder[SelectedBuildIndex];
    public UnitType SelectedHire => GameBalance.HireOrder[SelectedHireIndex];

    public GameSession(int seed)
    {
        World = new GameWorld(seed);
        Player = new Player();
        _raidDirector = new RaidDirector(seed);
        World.Warm(Player.Position, GameConfig.InitialWorldWarmupRadiusChunks);
    }

    public static GameSession FromSave(SaveGame save)
    {
        var session = new GameSession(save.Seed);
        session.Player.Position = new Vector2(save.Player.X, save.Player.Y);
        session.Player.Health = save.Player.Health;
        session.Player.MaxHealth = save.Player.MaxHealth;
        session.Player.Hunger = save.Player.Hunger;
        session.Player.WeaponTier = save.Player.WeaponTier;
        session.Player.Inventory = Inventory.FromDictionary(save.Inventory);
        session.Time.DayNumber = save.DayNumber;
        session.Time.Phase = save.Phase;
        session.Time.PhaseElapsed = save.PhaseElapsed;
        session.World.RemovedNodeIds.Clear();
        foreach (var id in save.RemovedNodeIds) session.World.RemovedNodeIds.Add(id);
        session.World.Structures.Clear();
        foreach (var s in save.Structures)
        {
            session.World.Structures.Add(new Structure
            {
                Id = s.Id,
                Type = s.Type,
                Tile = new TilePoint(s.X, s.Y),
                Health = s.Health,
                MaxHealth = s.MaxHealth,
                Growth = s.Growth
            });
        }
        foreach (var u in save.Units)
        {
            var unit = new HiredUnit(u.Type, new Vector2(u.X, u.Y))
            {
                Health = u.Health,
                MaxHealth = u.MaxHealth,
                Level = u.Level,
                NightsSurvived = u.NightsSurvived,
                AssignedTile = new TilePoint(u.AssignedX, u.AssignedY)
            };
            session.Units.Add(unit);
        }
        session.SetMessage("Loaded world. Your run continues.", 4f);
        return session;
    }

    public SaveGame ToSave() => new()
    {
        Seed = World.Seed,
        DayNumber = Time.DayNumber,
        Phase = Time.Phase,
        PhaseElapsed = Time.PhaseElapsed,
        Inventory = Player.Inventory.ToDictionary(),
        RemovedNodeIds = World.RemovedNodeIds.ToList(),
        Player = new PlayerSave
        {
            X = Player.Position.X,
            Y = Player.Position.Y,
            Health = Player.Health,
            MaxHealth = Player.MaxHealth,
            Hunger = Player.Hunger,
            WeaponTier = Player.WeaponTier
        },
        Structures = World.Structures.Where(s => !s.IsDestroyed).Select(s => new StructureSave
        {
            Id = s.Id,
            Type = s.Type,
            X = s.Tile.X,
            Y = s.Tile.Y,
            Health = s.Health,
            MaxHealth = s.MaxHealth,
            Growth = s.Growth
        }).ToList(),
        Units = Units.Where(u => u.IsAlive).Select(u => new UnitSave
        {
            Type = u.Type,
            X = u.Position.X,
            Y = u.Position.Y,
            Health = u.Health,
            MaxHealth = u.MaxHealth,
            Level = u.Level,
            NightsSurvived = u.NightsSurvived,
            AssignedX = u.AssignedTile.X,
            AssignedY = u.AssignedTile.Y
        }).ToList()
    };

    public void Update(GameTime gameTime, TheDawn.Input.InputState input, Vector2 pointerWorld)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (IsPermadead) return;
        _messageTime -= dt;
        _autosaveTimer += dt;
        Time.Update(dt);
        if (Time.PhaseChangedThisFrame) OnPhaseEntered(Time.Phase);
        _raidDirector.Update(dt, World, Player.Position, Enemies);

        if (input.BuildPressed)
        {
            BuildMode = true;
            SelectedBuildIndex = (SelectedBuildIndex + 1) % GameBalance.BuildOrder.Length;
            var def = GameBalance.Structures[SelectedStructure];
            SetMessage($"Build: {def.Name} - {CostLabel(def.Cost)}", 3f);
        }
        if (input.HirePressed)
        {
            SelectedHireIndex = (SelectedHireIndex + 1) % GameBalance.HireOrder.Length;
            TryHire(SelectedHire);
        }
        if (input.NumberPressed >= 0)
        {
            if (BuildMode && input.NumberPressed > 0 && input.NumberPressed <= GameBalance.BuildOrder.Length)
                SelectedBuildIndex = input.NumberPressed - 1;
            else if (!BuildMode) _crafting.CraftBasic(Player.Inventory, input.NumberPressed);
            if (!string.IsNullOrWhiteSpace(_crafting.LastMessage)) SetMessage(_crafting.LastMessage, 3f);
        }
        if (input.CraftPressed)
        {
            SetMessage("Craft keys: 1 sword, 2 bow, 3 cook fish, 4 ingot, 5 potion.", 5f);
        }

        if (BuildMode && (input.PrimaryPressed || input.ConfirmPressed)) TryBuild(GameWorld.WorldToTile(pointerWorld));

        UpdatePlayer(dt, input);
        if (!BuildMode && input.UsePressed) PlayerAction();
        UpdateUnits(dt);
        UpdateEnemies(dt);
        UpdateProjectiles(dt);
        World.Warm(Player.Position, GameConfig.ActiveChunkRadius);
        World.TrimAround(Player.Position);

        if (_autosaveTimer > 30f)
        {
            _autosaveTimer = 0f;
            SaveSystem.Save(this);
        }
        if (Player.Health <= 0 && !IsPermadead) DiePermadeath();
    }

    private void UpdatePlayer(float dt, TheDawn.Input.InputState input)
    {
        Player.AttackCooldown = Math.Max(0, Player.AttackCooldown - dt);
        Player.ActionCooldown = Math.Max(0, Player.ActionCooldown - dt);
        Player.AnimationTime += dt;
        Player.HungerClock += dt;
        if (Player.HungerClock >= 45f)
        {
            Player.HungerClock -= 45f;
            Player.Hunger = Math.Max(0, Player.Hunger - 1);
            if (Player.Hunger == 0) Player.TakeDamage(1);
        }
        if (Player.Inventory.Has(ItemId.Food, 1) && Player.Hunger < 60 && input.SecondaryPressed)
        {
            Player.Inventory.Spend(ItemId.Food, 1);
            Player.Hunger = Math.Min(GameConfig.PlayerMaxHunger, Player.Hunger + 25);
            SetMessage("Ate food.", 2f);
        }
        var move = input.Move;
        if (move.LengthSquared() > 0)
        {
            Player.Facing = Math.Abs(move.X) > Math.Abs(move.Y) ? (move.X < 0 ? Facing.Left : Facing.Right) : (move.Y < 0 ? Facing.Up : Facing.Down);
        }
        Player.Velocity = move;
        var step = move * GameConfig.PlayerSpeed * dt;
        TryMove(Player, new Vector2(step.X, 0), true);
        TryMove(Player, new Vector2(0, step.Y), true);
    }

    private void TryMove(Entity entity, Vector2 delta, bool unitsCanPassGates)
    {
        if (delta.LengthSquared() <= 0) return;
        var next = entity.Position + delta;
        var tile = GameWorld.WorldToTile(next);
        if (World.IsPassable(tile.X, tile.Y, unitsCanPassGates)) entity.Position = next;
    }

    private void PlayerAction()
    {
        if (Player.AttackCooldown <= 0)
        {
            var enemy = Enemies.Where(e => e.IsAlive).OrderBy(e => Vector2.DistanceSquared(e.Position, Player.Position)).FirstOrDefault();
            if (enemy != null && Vector2.Distance(enemy.Position, Player.Position) <= Player.AttackRange)
            {
                Player.AttackCooldown = 0.45f;
                enemy.TakeDamage(Player.AttackDamage);
                if (!enemy.IsAlive) LootEnemy(enemy);
                return;
            }
        }

        if (Player.ActionCooldown > 0) return;
        var node = World.FindNodeNear(Player.Position, 48f);
        if (node != null)
        {
            Player.ActionCooldown = 0.42f;
            var damage = node.Type is ResourceType.Tree ? 12 : 10 + Player.WeaponTier * 2;
            node.Health -= damage;
            if (node.Health <= 0)
            {
                World.RemoveNode(node);
                Player.Inventory.Add(node.YieldItem, node.YieldAmount);
                if (node.Type == ResourceType.BerryBush) Player.Inventory.Add(ItemId.Seed, 1);
                if (node.Type == ResourceType.Tree) Player.Inventory.Add(ItemId.Fiber, 1);
                SetMessage($"Gathered {node.YieldAmount} {node.YieldItem}.", 2.5f);
            }
            return;
        }
        var tile = GameWorld.WorldToTile(Player.Position);
        if (IsAdjacentToWater(tile))
        {
            Player.ActionCooldown = 2.25f;
            Player.Inventory.Add(ItemId.Fish, 1);
            SetMessage("Caught fish.", 2.5f);
            return;
        }
        var structure = World.FindStructureNear(Player.Position, 42f);
        if (structure != null && structure.Health < structure.MaxHealth && Player.Inventory.Spend(ItemId.Wood, 1))
        {
            structure.Health = Math.Min(structure.MaxHealth, structure.Health + 20);
            Player.ActionCooldown = 0.4f;
            SetMessage("Repaired structure.", 2f);
        }
    }

    private bool IsAdjacentToWater(TilePoint tile)
    {
        for (var y = -1; y <= 1; y++)
        for (var x = -1; x <= 1; x++)
        {
            if (World.IsWater(tile.X + x, tile.Y + y)) return true;
        }
        return false;
    }

    private void TryBuild(TilePoint tile)
    {
        var def = GameBalance.Structures[SelectedStructure];
        if (Vector2.Distance(tile.CenterWorld, Player.Position) > 190f)
        {
            SetMessage("Too far to build.", 2f);
            return;
        }
        if (!World.CanPlaceStructure(SelectedStructure, tile))
        {
            SetMessage("Tile blocked.", 2f);
            return;
        }
        if (!Player.Inventory.Spend(def.Cost.AsDictionary))
        {
            SetMessage($"Need {CostLabel(def.Cost)}.", 3f);
            return;
        }
        World.Structures.Add(new Structure { Type = SelectedStructure, Tile = tile, Health = def.MaxHealth, MaxHealth = def.MaxHealth });
        SetMessage($"Built {def.Name}.", 2f);
        SaveSystem.Save(this);
    }

    private void TryHire(UnitType type)
    {
        var hasBarracks = World.Structures.Any(s => !s.IsDestroyed && s.Type == StructureType.Barracks && Vector2.Distance(s.Tile.CenterWorld, Player.Position) < 170f);
        var hasAlchemy = World.Structures.Any(s => !s.IsDestroyed && s.Type == StructureType.AlchemyTable && Vector2.Distance(s.Tile.CenterWorld, Player.Position) < 170f);
        if (!hasBarracks && type != UnitType.Swordsman)
        {
            SetMessage("Build a barracks nearby to hire advanced units.", 3f);
            return;
        }
        if (type == UnitType.Mage && !hasAlchemy)
        {
            SetMessage("Mages require a nearby alchemy table.", 3f);
            return;
        }
        var def = GameBalance.Units[type];
        if (!Player.Inventory.Spend(def.Cost.AsDictionary))
        {
            SetMessage($"Need {CostLabel(def.Cost)} for {def.Name}.", 3f);
            return;
        }
        var unit = new HiredUnit(type, Player.Position + new Vector2(32, 0));
        Units.Add(unit);
        SetMessage($"Hired {def.Name}. Place them with H cycles; they hold this area.", 3f);
        SaveSystem.Save(this);
    }

    private void UpdateUnits(float dt)
    {
        foreach (var unit in Units.Where(u => u.IsAlive))
        {
            unit.AnimationTime += dt;
            unit.AttackTimer = Math.Max(0, unit.AttackTimer - dt);
            unit.WorkTimer += dt;
            if (Time.Phase == GamePhase.Night || Time.Phase == GamePhase.Dusk)
            {
                var def = GameBalance.Units[unit.Type];
                var target = Enemies.Where(e => e.IsAlive && Vector2.DistanceSquared(e.Position, unit.Position) <= def.Range * def.Range)
                    .OrderBy(e => Vector2.DistanceSquared(e.Position, unit.Position)).FirstOrDefault();
                if (target != null && unit.AttackTimer <= 0)
                {
                    unit.AttackTimer = def.AttackSeconds;
                    if (def.Range > 64)
                    {
                        FireProjectile(unit.Id, unit.Position, target.Position, unit.Damage, true, unit.Type == UnitType.Mage);
                    }
                    else
                    {
                        target.TakeDamage(unit.Damage);
                        if (!target.IsAlive) LootEnemy(target);
                    }
                }
                else if (Vector2.DistanceSquared(unit.Position, unit.AssignedTile.CenterWorld) > 28 * 28)
                {
                    MoveEntityToward(unit, unit.AssignedTile.CenterWorld, def.Speed, dt, true);
                }
            }
            else
            {
                if (unit.Type == UnitType.Miner && unit.WorkTimer > 8f)
                {
                    unit.WorkTimer = 0;
                    var node = World.FindNodeNear(unit.Position, 210f);
                    if (node != null && node.Type is ResourceType.Rock or ResourceType.IronOre or ResourceType.CrystalDeposit or ResourceType.GoldVein)
                    {
                        node.Health -= 20;
                        if (node.Health <= 0)
                        {
                            World.RemoveNode(node);
                            Player.Inventory.Add(node.YieldItem, Math.Max(1, node.YieldAmount - 1));
                        }
                    }
                }
                if (unit.Type == UnitType.Farmer && unit.WorkTimer > 10f)
                {
                    unit.WorkTimer = 0;
                    foreach (var plot in World.Structures.Where(s => !s.IsDestroyed && s.Type == StructureType.FarmPlot && Vector2.DistanceSquared(s.Tile.CenterWorld, unit.Position) < 280 * 280))
                    {
                        plot.Growth++;
                        if (plot.Growth >= 4)
                        {
                            plot.Growth = 0;
                            Player.Inventory.Add(ItemId.Food, 4);
                            Player.Inventory.Add(ItemId.Seed, 1);
                            SetMessage("Farmer harvested crops.", 2f);
                        }
                        break;
                    }
                }
            }
        }
        Units.RemoveAll(u => !u.IsAlive);
    }

    private void UpdateEnemies(float dt)
    {
        foreach (var enemy in Enemies.Where(e => e.IsAlive))
        {
            enemy.AnimationTime += dt;
            enemy.AttackTimer = Math.Max(0, enemy.AttackTimer - dt);
            enemy.PathTimer -= dt;
            var def = GameBalance.Enemies[enemy.Type];
            var target = SelectTarget(enemy, def);
            if (target.Position.HasValue)
            {
                var distance = Vector2.Distance(enemy.Position, target.Position.Value);
                if (distance <= def.Range)
                {
                    AttackTarget(enemy, target, def);
                }
                else
                {
                    if (enemy.PathTimer <= 0)
                    {
                        enemy.PathTimer = 0.25f;
                        enemy.CachedStep = _pathfinder.NextStep(World, enemy.CurrentTile, GameWorld.WorldToTile(target.Position.Value), 360, false);
                    }
                    var destination = enemy.CachedStep?.CenterWorld ?? target.Position.Value;
                    MoveEntityToward(enemy, destination, def.Speed, dt, false);
                }
            }
        }
        Enemies.RemoveAll(e => !e.IsAlive && e.Health <= 0);
    }

    private TargetInfo SelectTarget(Enemy enemy, EnemyDefinition def)
    {
        if (Time.DayNumber >= 60)
        {
            var unit = Units.Where(u => u.IsAlive).OrderBy(u => Vector2.DistanceSquared(enemy.Position, u.Position)).FirstOrDefault();
            if (unit != null) return TargetInfo.ForUnit(unit);
        }
        var nearbyStructure = World.Structures.Where(s => !s.IsDestroyed && GameBalance.Structures[s.Type].BlocksMovement)
            .OrderBy(s => Vector2.DistanceSquared(enemy.Position, s.Tile.CenterWorld)).FirstOrDefault();
        if (nearbyStructure != null && Vector2.DistanceSquared(enemy.Position, nearbyStructure.Tile.CenterWorld) < 340 * 340)
            return TargetInfo.ForStructure(nearbyStructure);
        var nearestUnit = Units.Where(u => u.IsAlive).OrderBy(u => Vector2.DistanceSquared(enemy.Position, u.Position)).FirstOrDefault();
        if (nearestUnit != null && Vector2.DistanceSquared(enemy.Position, nearestUnit.Position) < 190 * 190)
            return TargetInfo.ForUnit(nearestUnit);
        return TargetInfo.ForPlayer(Player);
    }

    private void AttackTarget(Enemy enemy, TargetInfo target, EnemyDefinition def)
    {
        if (enemy.AttackTimer > 0) return;
        enemy.AttackTimer = def.AttackSeconds;
        if (def.Range > 80 && target.Position.HasValue)
        {
            FireProjectile(enemy.Id, enemy.Position, target.Position.Value, def.Damage, false, enemy.Type is EnemyType.SkeletonMage or EnemyType.OrcShaman);
            return;
        }
        if (target.Player != null) target.Player.TakeDamage(def.Damage);
        if (target.Unit != null) target.Unit.TakeDamage(def.Damage);
        if (target.Structure != null)
        {
            target.Structure.Health -= def.Damage;
            if (target.Structure.Health <= 0)
            {
                target.Structure.Health = 0;
                SetMessage($"{GameBalance.Structures[target.Structure.Type].Name} was destroyed!", 3f);
            }
        }
    }

    private void MoveEntityToward(Entity entity, Vector2 destination, float speed, float dt, bool unitsCanPassGates)
    {
        var delta = destination - entity.Position;
        if (delta.LengthSquared() < 4) return;
        delta.Normalize();
        entity.Facing = Math.Abs(delta.X) > Math.Abs(delta.Y) ? (delta.X < 0 ? Facing.Left : Facing.Right) : (delta.Y < 0 ? Facing.Up : Facing.Down);
        var step = delta * speed * dt;
        TryMove(entity, new Vector2(step.X, 0), unitsCanPassGates);
        TryMove(entity, new Vector2(0, step.Y), unitsCanPassGates);
    }

    private void FireProjectile(Guid source, Vector2 from, Vector2 to, int damage, bool playerSide, bool area)
    {
        var dir = to - from;
        if (dir.LengthSquared() < 1) dir = Vector2.UnitX;
        dir.Normalize();
        Projectiles.Add(new Projectile { SourceId = source, Position = from, Velocity = dir * 220f, Damage = damage, FromPlayerSide = playerSide, Life = 2.2f, Radius = area ? 22f : 8f, AreaDamage = area });
    }

    private void UpdateProjectiles(float dt)
    {
        foreach (var p in Projectiles)
        {
            p.Life -= dt;
            p.Position += p.Velocity * dt;
            if (p.FromPlayerSide)
            {
                foreach (var enemy in Enemies.Where(e => e.IsAlive && Vector2.DistanceSquared(e.Position, p.Position) < p.Radius * p.Radius).ToList())
                {
                    enemy.TakeDamage(p.Damage);
                    if (!enemy.IsAlive) LootEnemy(enemy);
                    p.Life = 0;
                    if (!p.AreaDamage) break;
                }
            }
            else
            {
                if (Vector2.DistanceSquared(Player.Position, p.Position) < p.Radius * p.Radius)
                {
                    Player.TakeDamage(p.Damage);
                    p.Life = 0;
                }
                foreach (var unit in Units.Where(u => u.IsAlive && Vector2.DistanceSquared(u.Position, p.Position) < p.Radius * p.Radius).ToList())
                {
                    unit.TakeDamage(p.Damage);
                    p.Life = 0;
                    if (!p.AreaDamage) break;
                }
            }
        }
        Projectiles.RemoveAll(p => p.Expired);
    }

    private void LootEnemy(Enemy enemy)
    {
        var value = GameBalance.Enemies[enemy.Type].LootValue;
        Player.Inventory.Add(ItemId.Bone, Math.Max(1, value / 2));
        if (value >= 5) Player.Inventory.Add(ItemId.Gold, 1);
        if (enemy.Type is EnemyType.SkeletonMage or EnemyType.OrcShaman) Player.Inventory.Add(ItemId.Crystal, 1);
    }

    private void OnPhaseEntered(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.Dusk:
                SetMessage("DUSK: ten minute warning. Seal gaps. Assign guards.", 6f);
                break;
            case GamePhase.Night:
                _raidDirector.BeginNight(Time.DayNumber);
                SetMessage("NIGHT SIEGE: raiders are coming from the dungeon.", 6f);
                break;
            case GamePhase.Dawn:
                _raidDirector.EndNight();
                Enemies.Clear();
                foreach (var unit in Units.Where(u => u.IsAlive))
                {
                    unit.NightsSurvived++;
                    if (unit.NightsSurvived % 3 == 0) unit.Level = Math.Min(5, unit.Level + 1);
                }
                foreach (var plot in World.Structures.Where(s => !s.IsDestroyed && s.Type == StructureType.FarmPlot))
                {
                    plot.Growth++;
                    if (plot.Growth >= 5)
                    {
                        plot.Growth = 0;
                        Player.Inventory.Add(ItemId.Food, 3);
                        Player.Inventory.Add(ItemId.Seed, 1);
                    }
                }
                Player.DaysSurvived = Time.DayNumber;
                Player.Health = Math.Min(Player.MaxHealth, Player.Health + 15);
                SetMessage("DAWN: count losses, repair walls, collect the story.", 6f);
                SaveSystem.Save(this);
                break;
            case GamePhase.Day:
                SetMessage($"DAY {Time.DayNumber}: new world pressure unlocked.", 5f);
                break;
        }
    }


    private void DiePermadeath()
    {
        IsPermadead = true;
        SaveSystem.ArchiveDeath(this);
        SaveSystem.DeleteLiveSave();
        SetMessage($"PERMADEATH: Day {Time.DayNumber}. The jungle keeps the ruins.", 999f);
    }

    public void SetMessage(string message, float seconds)
    {
        Message = message;
        _messageTime = seconds;
    }

    public bool ShouldShowMessage => _messageTime > 0 || IsPermadead;

    private static string CostLabel(Cost cost) => string.Join(", ", cost.AsDictionary.Select(kv => $"{kv.Value} {kv.Key}"));

    private readonly struct TargetInfo
    {
        public Player? Player { get; }
        public HiredUnit? Unit { get; }
        public Structure? Structure { get; }
        public Vector2? Position { get; }

        private TargetInfo(Player? player, HiredUnit? unit, Structure? structure, Vector2? position)
        {
            Player = player;
            Unit = unit;
            Structure = structure;
            Position = position;
        }

        public static TargetInfo ForPlayer(Player player) => new(player, null, null, player.Position);
        public static TargetInfo ForUnit(HiredUnit unit) => new(null, unit, null, unit.Position);
        public static TargetInfo ForStructure(Structure structure) => new(null, null, structure, structure.Tile.CenterWorld);
    }
}
