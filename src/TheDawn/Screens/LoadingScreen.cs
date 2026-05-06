using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheDawn.Input;
using TheDawn.Systems;

namespace TheDawn.Screens;

public sealed class LoadingScreen : IGameScreen
{
    private readonly DawnGame _game;
    private readonly Func<GameSession> _factory;
    private readonly string _caption;
    private readonly string[] _steps =
    {
        "Reading pixel crawler atlas manifest",
        "Building deterministic chunk cache",
        "Locating rivers and dungeon pressure line",
        "Preparing raid director and save systems",
        "Lighting the campfire"
    };
    private int _step;
    private GameSession? _session;

    public LoadingScreen(DawnGame game, Func<GameSession> factory, string caption)
    {
        _game = game;
        _factory = factory;
        _caption = caption;
    }

    public void Enter() { }
    public void Exit() { }

    public void Update(GameTime gameTime, InputState input)
    {
        if (_step == 0)
        {
            _session = _factory();
            _step++;
            return;
        }
        if (_session != null && _step < _steps.Length)
        {
            _session.World.Warm(_session.Player.Position, Math.Min(GameConfig.ActiveChunkRadius, _step + 2));
            _step++;
            return;
        }
        if (_session != null)
        {
            _game.ChangeScreen(new PlayScreen(_game, _session));
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch batch)
    {
        batch.Begin(samplerState: SamplerState.PointClamp);
        batch.Draw(_game.Pixel, new Rectangle(0, 0, _game.BackBufferWidth, _game.BackBufferHeight), new Color(7, 9, 11));
        _game.Text.DrawShadowed(batch, _caption.ToUpperInvariant(), new Vector2(340, 230), new Color(255, 224, 128), 4);
        var progress = Math.Clamp(_step / (float)_steps.Length, 0f, 1f);
        var bar = new Rectangle(340, 340, 600, 28);
        batch.Draw(_game.Pixel, bar, new Color(40, 42, 46));
        batch.Draw(_game.Pixel, new Rectangle(bar.X + 2, bar.Y + 2, (int)((bar.Width - 4) * progress), bar.Height - 4), new Color(90, 190, 95));
        var stepText = _steps[Math.Min(_step, _steps.Length - 1)];
        _game.Text.DrawShadowed(batch, stepText, new Vector2(340, 390), Color.White, 2);
        batch.End();
    }
}
