using System.Text.Json;
using System.Text.Json.Serialization;
using TheDawn.Systems;

namespace TheDawn.Persistence;

public static class SaveSystem
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string SaveDirectory
    {
        get
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(root)) root = AppContext.BaseDirectory;
            return Path.Combine(root, "TheDawn");
        }
    }

    public static string LiveSavePath => Path.Combine(SaveDirectory, GameConfig.SaveSlotName);
    public static string GraveyardPath => Path.Combine(SaveDirectory, GameConfig.GraveyardFile);

    public static bool HasLiveSave => File.Exists(LiveSavePath);

    public static void Save(GameSession session)
    {
        Directory.CreateDirectory(SaveDirectory);
        var json = JsonSerializer.Serialize(session.ToSave(), Options);
        var tmp = LiveSavePath + ".tmp";
        File.WriteAllText(tmp, json);
        if (File.Exists(LiveSavePath)) File.Delete(LiveSavePath);
        File.Move(tmp, LiveSavePath);
    }

    public static SaveGame? Load()
    {
        if (!File.Exists(LiveSavePath)) return null;
        var json = File.ReadAllText(LiveSavePath);
        return JsonSerializer.Deserialize<SaveGame>(json, Options);
    }

    public static void DeleteLiveSave()
    {
        if (File.Exists(LiveSavePath)) File.Delete(LiveSavePath);
    }

    public static void ArchiveDeath(GameSession session)
    {
        Directory.CreateDirectory(SaveDirectory);
        var records = new List<DeathRecord>();
        if (File.Exists(GraveyardPath))
        {
            try
            {
                records = JsonSerializer.Deserialize<List<DeathRecord>>(File.ReadAllText(GraveyardPath), Options) ?? new List<DeathRecord>();
            }
            catch
            {
                records = new List<DeathRecord>();
            }
        }
        records.Add(new DeathRecord
        {
            DiedAt = DateTimeOffset.UtcNow,
            Seed = session.World.Seed,
            DayNumber = session.Time.DayNumber,
            Summary = $"Reached day {session.Time.DayNumber} with {session.World.Structures.Count} structures and {session.Units.Count} surviving hires."
        });
        File.WriteAllText(GraveyardPath, JsonSerializer.Serialize(records, Options));
    }
}
