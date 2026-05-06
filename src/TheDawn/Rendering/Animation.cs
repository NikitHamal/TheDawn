using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheDawn.Rendering;

public readonly record struct SpriteAnimation(string TextureId, int FrameWidth, int FrameHeight, int FrameCount, double SecondsPerFrame)
{
    public Rectangle SourceAt(double time)
    {
        var frame = FrameCount <= 1 ? 0 : (int)Math.Floor(time / SecondsPerFrame) % FrameCount;
        return new Rectangle(frame * FrameWidth, 0, FrameWidth, FrameHeight);
    }
}

public static class DrawHelpers
{
    public static void DrawBar(SpriteBatch batch, Texture2D pixel, Rectangle rect, float ratio, Color back, Color fill)
    {
        ratio = MathHelper.Clamp(ratio, 0f, 1f);
        batch.Draw(pixel, rect, back);
        var inner = new Rectangle(rect.X + 1, rect.Y + 1, Math.Max(0, (int)((rect.Width - 2) * ratio)), rect.Height - 2);
        batch.Draw(pixel, inner, fill);
    }
}
