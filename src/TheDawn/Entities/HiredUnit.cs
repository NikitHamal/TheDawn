using Microsoft.Xna.Framework;
using TheDawn.Data;
using TheDawn.World;

namespace TheDawn.Entities;

public sealed class HiredUnit : Entity
{
    public UnitType Type { get; }
    public int Level { get; set; } = 1;
    public int NightsSurvived { get; set; }
    public TilePoint AssignedTile { get; set; }
    public float AttackTimer { get; set; }
    public float WorkTimer { get; set; }
    public Guid? TargetEnemyId { get; set; }

    public HiredUnit(UnitType type, Vector2 position)
    {
        Type = type;
        var def = GameBalance.Units[type];
        MaxHealth = def.MaxHealth;
        Health = MaxHealth;
        Position = position;
        AssignedTile = new TilePoint((int)MathF.Floor(position.X / GameConfig.TileSize), (int)MathF.Floor(position.Y / GameConfig.TileSize));
    }

    public int Damage => GameBalance.Units[Type].Damage + (Level - 1) * 5;
}
