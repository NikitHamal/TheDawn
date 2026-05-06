using Microsoft.Xna.Framework;
using TheDawn.Data;
using TheDawn.World;

namespace TheDawn.Entities;

public sealed class Enemy : Entity
{
    public EnemyType Type { get; }
    public float AttackTimer;
    public TilePoint? CachedStep;
    public float PathTimer;
    public Guid? TargetEntityId;
    public Guid? TargetStructureId;
    public bool HasReachedBase;

    public Enemy(EnemyType type, Vector2 position)
    {
        Type = type;
        var def = GameBalance.Enemies[type];
        MaxHealth = def.MaxHealth;
        Health = MaxHealth;
        Position = position;
    }
}
