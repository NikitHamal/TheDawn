using Microsoft.Xna.Framework;

namespace TheDawn.Rendering;

public sealed class Camera2D
{
    public Vector2 Position;
    public float Zoom { get; set; } = 2.0f;

    public Vector2 WorldToScreen(Vector2 world, int width, int height)
        => (world - Position) * Zoom + new Vector2(width * 0.5f, height * 0.5f);

    public Vector2 ScreenToWorld(Vector2 screen, int width, int height)
        => (screen - new Vector2(width * 0.5f, height * 0.5f)) / Zoom + Position;

    public Rectangle VisibleWorldBounds(int width, int height)
    {
        var half = new Vector2(width, height) / (2f * Zoom);
        var min = Position - half - new Vector2(96);
        var max = Position + half + new Vector2(96);
        return new Rectangle((int)MathF.Floor(min.X), (int)MathF.Floor(min.Y), (int)MathF.Ceiling(max.X - min.X), (int)MathF.Ceiling(max.Y - min.Y));
    }
}
