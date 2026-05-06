using System.Collections.ObjectModel;

namespace TheDawn.Data;

public sealed class Inventory
{
    private readonly Dictionary<ItemId, int> _items = new();

    public IReadOnlyDictionary<ItemId, int> Items => new ReadOnlyDictionary<ItemId, int>(_items);

    public int this[ItemId item] => _items.TryGetValue(item, out var count) ? count : 0;

    public void Add(ItemId item, int amount)
    {
        if (amount <= 0) return;
        _items[item] = this[item] + amount;
    }

    public bool Has(ItemId item, int amount) => this[item] >= amount;

    public bool Has(IReadOnlyDictionary<ItemId, int> costs)
    {
        foreach (var cost in costs)
        {
            if (!Has(cost.Key, cost.Value)) return false;
        }
        return true;
    }

    public bool Spend(ItemId item, int amount)
    {
        if (!Has(item, amount)) return false;
        _items[item] -= amount;
        if (_items[item] <= 0) _items.Remove(item);
        return true;
    }

    public bool Spend(IReadOnlyDictionary<ItemId, int> costs)
    {
        if (!Has(costs)) return false;
        foreach (var cost in costs) Spend(cost.Key, cost.Value);
        return true;
    }

    public Dictionary<ItemId, int> ToDictionary() => new(_items);

    public static Inventory FromDictionary(Dictionary<ItemId, int>? source)
    {
        var inventory = new Inventory();
        if (source == null) return inventory;
        foreach (var item in source)
        {
            if (item.Value > 0) inventory._items[item.Key] = item.Value;
        }
        return inventory;
    }
}
