using Microsoft.Xna.Framework;
using TheDawn.Data;
using TheDawn.World;

namespace TheDawn.Entities;

public abstract class Entity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Vector2 Position;
    public Vector2 Velocity;
    public int Health;
    public int MaxHealth;
    public Facing Facing = Facing.Down;
    public double AnimationTime;
    public bool Removed;

    public bool IsAlive => Health > 0 && !Removed;
    public TilePoint CurrentTile => new((int)MathF.Floor(Position.X / GameConfig.TileSize), (int)MathF.Floor(Position.Y / GameConfig.TileSize));

    public virtual void TakeDamage(int amount)
    {
        if (amount <= 0 || !IsAlive) return;
        Health -= amount;
        if (Health <= 0)
        {
            Health = 0;
            Removed = true;
        }
    }
}
