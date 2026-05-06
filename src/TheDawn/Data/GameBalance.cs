namespace TheDawn.Data;

public sealed record Cost(params (ItemId Item, int Amount)[] Entries)
{
    public IReadOnlyDictionary<ItemId, int> AsDictionary { get; } = Entries.ToDictionary(x => x.Item, x => x.Amount);
}

public sealed record StructureDefinition(
    StructureType Type,
    string Name,
    Cost Cost,
    int MaxHealth,
    bool BlocksMovement,
    int Tier,
    string Description);

public sealed record UnitDefinition(
    UnitType Type,
    string Name,
    Cost Cost,
    int MaxHealth,
    float Speed,
    int Damage,
    float Range,
    float AttackSeconds,
    string Description);

public sealed record EnemyDefinition(
    EnemyType Type,
    string Name,
    int MaxHealth,
    float Speed,
    int Damage,
    float Range,
    float AttackSeconds,
    int LootValue);

public static class GameBalance
{
    public static readonly IReadOnlyDictionary<StructureType, StructureDefinition> Structures = new Dictionary<StructureType, StructureDefinition>
    {
        [StructureType.WoodWall] = new(StructureType.WoodWall, "Wood Wall", new Cost((ItemId.Wood, 3)), 45, true, 1, "Breaks quickly but saves lives."),
        [StructureType.StoneWall] = new(StructureType.StoneWall, "Stone Wall", new Cost((ItemId.Stone, 5)), 140, true, 2, "Reliable basic siege wall."),
        [StructureType.IronWall] = new(StructureType.IronWall, "Iron Wall", new Cost((ItemId.Stone, 6), (ItemId.IronIngot, 2)), 280, true, 3, "Holds organized raids."),
        [StructureType.CrystalWall] = new(StructureType.CrystalWall, "Crystal Wall", new Cost((ItemId.Stone, 8), (ItemId.Crystal, 3)), 420, true, 4, "Late-run infused defense."),
        [StructureType.Gate] = new(StructureType.Gate, "Gate", new Cost((ItemId.Wood, 6), (ItemId.IronIngot, 1)), 160, true, 2, "A passable base choke point."),
        [StructureType.Campfire] = new(StructureType.Campfire, "Campfire", new Cost((ItemId.Wood, 4), (ItemId.Stone, 2)), 60, false, 1, "Night light and cooking."),
        [StructureType.Workbench] = new(StructureType.Workbench, "Workbench", new Cost((ItemId.Wood, 10), (ItemId.Stone, 3)), 90, false, 1, "Unlocks basic crafting."),
        [StructureType.Sawmill] = new(StructureType.Sawmill, "Sawmill", new Cost((ItemId.Wood, 18), (ItemId.Stone, 8)), 130, false, 2, "Efficient wood processing."),
        [StructureType.Furnace] = new(StructureType.Furnace, "Furnace", new Cost((ItemId.Stone, 18), (ItemId.IronOre, 6)), 180, false, 2, "Smelts ore into ingots."),
        [StructureType.Anvil] = new(StructureType.Anvil, "Anvil", new Cost((ItemId.IronIngot, 8), (ItemId.Wood, 4)), 160, false, 3, "Weapon and armor chain."),
        [StructureType.Watchtower] = new(StructureType.Watchtower, "Watchtower", new Cost((ItemId.Wood, 16), (ItemId.Stone, 8)), 120, false, 2, "Archers use it for range."),
        [StructureType.Barracks] = new(StructureType.Barracks, "Barracks", new Cost((ItemId.Wood, 22), (ItemId.Stone, 14), (ItemId.IronIngot, 4)), 220, false, 3, "Hire and station units."),
        [StructureType.AlchemyTable] = new(StructureType.AlchemyTable, "Alchemy Table", new Cost((ItemId.Crystal, 6), (ItemId.Gold, 4), (ItemId.Wood, 10)), 170, false, 4, "Mages and area traps."),
        [StructureType.SpikeTrap] = new(StructureType.SpikeTrap, "Spike Trap", new Cost((ItemId.Wood, 5), (ItemId.IronIngot, 1)), 35, false, 3, "Damages enemies that step on it."),
        [StructureType.FarmPlot] = new(StructureType.FarmPlot, "Farm Plot", new Cost((ItemId.Seed, 2), (ItemId.Wood, 2)), 35, false, 1, "Grows food over several dawns.")
    };

    public static readonly StructureType[] BuildOrder =
    {
        StructureType.WoodWall,
        StructureType.StoneWall,
        StructureType.Gate,
        StructureType.Campfire,
        StructureType.Workbench,
        StructureType.FarmPlot,
        StructureType.Watchtower,
        StructureType.Furnace,
        StructureType.Anvil,
        StructureType.Barracks,
        StructureType.AlchemyTable,
        StructureType.SpikeTrap,
        StructureType.IronWall,
        StructureType.CrystalWall
    };

    public static readonly IReadOnlyDictionary<UnitType, UnitDefinition> Units = new Dictionary<UnitType, UnitDefinition>
    {
        [UnitType.Swordsman] = new(UnitType.Swordsman, "Swordsman", new Cost((ItemId.Wood, 10), (ItemId.Food, 8)), 85, 74, 16, 30, 0.8f, "Melee wall defender. Levels into a knight."),
        [UnitType.Archer] = new(UnitType.Archer, "Archer", new Cost((ItemId.Wood, 14), (ItemId.IronOre, 5)), 55, 70, 12, 168, 1.1f, "Tower and wall ranged cover."),
        [UnitType.Mage] = new(UnitType.Mage, "Mage", new Cost((ItemId.Crystal, 6), (ItemId.Gold, 4)), 48, 62, 24, 144, 1.8f, "Consumes potions for area damage."),
        [UnitType.Miner] = new(UnitType.Miner, "Miner", new Cost((ItemId.Food, 12), (ItemId.Wood, 8)), 65, 60, 8, 28, 1.2f, "Day worker that gathers ore."),
        [UnitType.Farmer] = new(UnitType.Farmer, "Farmer", new Cost((ItemId.Food, 12), (ItemId.Seed, 6)), 58, 62, 6, 28, 1.2f, "Autonomously waters and harvests crops.")
    };

    public static readonly UnitType[] HireOrder = { UnitType.Swordsman, UnitType.Archer, UnitType.Miner, UnitType.Farmer, UnitType.Mage };

    public static readonly IReadOnlyDictionary<EnemyType, EnemyDefinition> Enemies = new Dictionary<EnemyType, EnemyDefinition>
    {
        [EnemyType.SkeletonRogue] = new(EnemyType.SkeletonRogue, "Skeleton Rogue", 34, 58, 6, 26, 1.0f, 1),
        [EnemyType.SkeletonWarrior] = new(EnemyType.SkeletonWarrior, "Skeleton Warrior", 55, 48, 10, 28, 1.2f, 2),
        [EnemyType.SkeletonArcher] = new(EnemyType.SkeletonArcher, "Skeleton Archer", 38, 46, 7, 138, 1.5f, 2),
        [EnemyType.SkeletonMage] = new(EnemyType.SkeletonMage, "Skeleton Mage", 44, 42, 11, 122, 2.2f, 3),
        [EnemyType.OrcRogue] = new(EnemyType.OrcRogue, "Orc Rogue", 62, 58, 12, 28, 0.95f, 3),
        [EnemyType.OrcWarrior] = new(EnemyType.OrcWarrior, "Orc Warrior", 110, 43, 18, 34, 1.35f, 5),
        [EnemyType.OrcShaman] = new(EnemyType.OrcShaman, "Orc Shaman", 84, 38, 16, 135, 2.6f, 6),
        [EnemyType.RaidLeader] = new(EnemyType.RaidLeader, "Raid Leader", 240, 42, 26, 40, 1.5f, 12),
        [EnemyType.DungeonBoss] = new(EnemyType.DungeonBoss, "Dungeon Boss", 520, 36, 38, 56, 1.8f, 30)
    };
}
