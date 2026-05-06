using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheDawn.Input;
using TheDawn.Systems;

namespace TheDawn.Screens;

public sealed class CreateWorldScreen : IGameScreen
{
    private readonly DawnGame _game;
    private int _seed;

    public CreateWorldScreen(DawnGame game)
    {
        _game = game;
        _seed = Environment.TickCount;
    }

    public void Enter() { }
    public void Exit() { }

    public void Update(GameTime gameTime, InputState input)
    {
        if (input.LeftPressed) _seed -= 97;
        if (input.RightPressed) _seed += 97;
        if (input.BackPressed) _game.ChangeScreen(new MainMenuScreen(_game));
        if (input.ConfirmPressed)
        {
            var seed = _seed;
            _game.ChangeScreen(new LoadingScreen(_game, () => new GameSession(seed), "Generating new world"));
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch batch)
    {
        batch.Begin(samplerState: SamplerState.PointClamp);
        batch.Draw(_game.Pixel, new Rectangle(0, 0, _game.BackBufferWidth, _game.BackBufferHeight), new Color(10, 14, 12));
        _game.Text.DrawShadowed(batch, "CREATE NEW WORLD", new Vector2(365, 130), new Color(255, 224, 128), 5);
        _game.Text.DrawShadowed(batch, "WORLD NAME: THE DAWN RUN", new Vector2(420, 250), Color.White, 3);
        _game.Text.DrawShadowed(batch, $"SEED: {_seed}", new Vector2(420, 300), new Color(180, 230, 255), 3);
        _game.Text.DrawShadowed(batch, "LEFT/RIGHT CHANGES SEED. ENTER STARTS. ESC RETURNS.", new Vector2(300, 390), Color.White, 2);
        _game.Text.DrawShadowed(batch, "EACH RUN IS A NEW INFINITE JUNGLE. DEATH DELETES THE LIVE SAVE.", new Vector2(250, 435), new Color(255, 160, 160), 2);
        batch.End();
    }
}
