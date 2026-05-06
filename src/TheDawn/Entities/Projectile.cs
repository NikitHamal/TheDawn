using Microsoft.Xna.Framework;

namespace TheDawn.Entities;

public sealed class Projectile
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Radius;
    public int Damage;
    public Guid SourceId;
    public bool FromPlayerSide;
    public float Life;
    public bool AreaDamage;

    public bool Expired => Life <= 0f;
}
