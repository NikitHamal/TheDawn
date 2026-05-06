namespace TheDawn;

public static class GameConfig
{
    public const int VirtualWidth = 1280;
    public const int VirtualHeight = 720;
    public const int TileSize = 32;
    public const int ChunkSize = 32;
    public const int ChunkPixelSize = TileSize * ChunkSize;
    public const int InitialWorldWarmupRadiusChunks = 3;
    public const int ActiveChunkRadius = 5;
    public const int MaxLoadedChunks = 180;
    public const int SpawnSafeRadiusTiles = 14;

    public const double DaySeconds = 720.0;
    public const double DuskSeconds = 600.0;
    public const double NightSeconds = 360.0;
    public const double DawnSeconds = 60.0;

    public const float PlayerSpeed = 110f;
    public const int PlayerMaxHealth = 120;
    public const int PlayerMaxHunger = 100;

    public const string SaveSlotName = "slot-1.json";
    public const string GraveyardFile = "graveyard.json";
}
