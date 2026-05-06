using Microsoft.Xna.Framework;
using TheDawn.Data;

namespace TheDawn.Entities;

public sealed class Player : Entity
{
    public Inventory Inventory { get; set; } = new();
    public int Hunger { get; set; } = GameConfig.PlayerMaxHunger;
    public int WeaponTier { get; set; } = 1;
    public float AttackCooldown { get; set; }
    public float ActionCooldown { get; set; }
    public float HungerClock { get; set; }
    public int DaysSurvived { get; set; }

    public Player()
    {
        MaxHealth = GameConfig.PlayerMaxHealth;
        Health = MaxHealth;
        Position = Vector2.Zero;
        Inventory.Add(ItemId.Wood, 8);
        Inventory.Add(ItemId.Stone, 4);
        Inventory.Add(ItemId.Food, 6);
        Inventory.Add(ItemId.Seed, 3);
    }

    public int AttackDamage => 14 + WeaponTier * 7;
    public float AttackRange => 46 + WeaponTier * 4;
}
