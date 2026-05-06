using TheDawn.Data;

namespace TheDawn.Systems;

public sealed class CraftingSystem
{
    public string LastMessage { get; private set; } = "";

    public bool CraftBasic(Inventory inventory, int slot)
    {
        LastMessage = "";
        return slot switch
        {
            1 => TryCraft(inventory, new Cost((ItemId.Wood, 4), (ItemId.Stone, 2)), ItemId.Sword, 1, "Crafted wood sword."),
            2 => TryCraft(inventory, new Cost((ItemId.Wood, 6), (ItemId.Fiber, 2)), ItemId.Bow, 1, "Crafted bow."),
            3 => TryCraft(inventory, new Cost((ItemId.Fish, 1)), ItemId.Food, 3, "Cooked fish."),
            4 => TryCraft(inventory, new Cost((ItemId.IronOre, 3)), ItemId.IronIngot, 1, "Smelted ingot."),
            5 => TryCraft(inventory, new Cost((ItemId.Crystal, 1), (ItemId.Food, 1)), ItemId.Potion, 2, "Mixed potions."),
            _ => false
        };
    }

    private bool TryCraft(Inventory inventory, Cost cost, ItemId output, int amount, string message)
    {
        if (!inventory.Spend(cost.AsDictionary))
        {
            LastMessage = "Missing materials.";
            return false;
        }
        inventory.Add(output, amount);
        LastMessage = message;
        return true;
    }
}
