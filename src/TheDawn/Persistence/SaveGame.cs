using TheDawn.Data;

namespace TheDawn.Persistence;

public sealed class SaveGame
{
    public int Version { get; set; } = 1;
    public int Seed { get; set; }
    public int DayNumber { get; set; }
    public GamePhase Phase { get; set; }
    public double PhaseElapsed { get; set; }
    public PlayerSave Player { get; set; } = new();
    public Dictionary<ItemId, int> Inventory { get; set; } = new();
    public List<long> RemovedNodeIds { get; set; } = new();
    public List<StructureSave> Structures { get; set; } = new();
    public List<UnitSave> Units { get; set; } = new();
}

public sealed class PlayerSave
{
    public float X { get; set; }
    public float Y { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Hunger { get; set; }
    public int WeaponTier { get; set; }
}

public sealed class StructureSave
{
    public Guid Id { get; set; }
    public StructureType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Growth { get; set; }
}

public sealed class UnitSave
{
    public UnitType Type { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Level { get; set; }
    public int NightsSurvived { get; set; }
    public int AssignedX { get; set; }
    public int AssignedY { get; set; }
}

public sealed class DeathRecord
{
    public DateTimeOffset DiedAt { get; set; }
    public int Seed { get; set; }
    public int DayNumber { get; set; }
    public string Summary { get; set; } = "";
}
