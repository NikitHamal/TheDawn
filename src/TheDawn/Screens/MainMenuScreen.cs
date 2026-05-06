using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheDawn.Input;
using TheDawn.Persistence;
using TheDawn.Systems;

namespace TheDawn.Screens;

public sealed class MainMenuScreen : IGameScreen
{
    private readonly DawnGame _game;
    private readonly string[] _items = { "Create New World", "Load World", "Options / Controls", "Quit" };
    private int _selected;
    private bool _showControls;
    private string _status = "";

    public MainMenuScreen(DawnGame game) => _game = game;

    public void Enter() { }
    public void Exit() { }

    public void Update(GameTime gameTime, InputState input)
    {
        if (input.UpPressed) _selected = (_selected + _items.Length - 1) % _items.Length;
        if (input.DownPressed) _selected = (_selected + 1) % _items.Length;
        if (input.ConfirmPressed)
        {
            switch (_selected)
            {
                case 0:
                    _game.ChangeScreen(new CreateWorldScreen(_game));
                    break;
                case 1:
                    var save = SaveSystem.Load();
                    if (save == null) _status = "No living run exists. Create a world.";
                    else _game.ChangeScreen(new LoadingScreen(_game, () => GameSession.FromSave(save), "Loading saved world"));
                    break;
                case 2:
                    _showControls = !_showControls;
                    break;
                case 3:
                    _game.Exit();
                    break;
            }
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch batch)
    {
        batch.Begin(samplerState: SamplerState.PointClamp);
        batch.Draw(_game.Pixel, new Rectangle(0, 0, _game.BackBufferWidth, _game.BackBufferHeight), new Color(8, 10, 12));
        var title = "THE DAWN";
        var titleScale = 7;
        var x = (_game.BackBufferWidth - _game.Text.MeasureWidth(title, titleScale)) / 2;
        _game.Text.DrawShadowed(batch, title, new Vector2(x, 90), new Color(255, 214, 116), titleScale);
        _game.Text.DrawShadowed(batch, "OPEN WORLD SURVIVAL - PERMADEATH - INFINITE JUNGLE", new Vector2(250, 170), Color.White, 2);
        for (var i = 0; i < _items.Length; i++)
        {
            var y = 255 + i * 48;
            var color = i == _selected ? new Color(255, 224, 128) : new Color(210, 218, 210);
            var prefix = i == _selected ? "> " : "  ";
            _game.Text.DrawShadowed(batch, prefix + _items[i], new Vector2(430, y), color, 3);
        }
        if (!string.IsNullOrWhiteSpace(_status)) _game.Text.DrawShadowed(batch, _status, new Vector2(350, 485), new Color(255, 120, 120), 2);
        if (_showControls)
        {
            var panel = new Rectangle(238, 505, 804, 150);
            batch.Draw(_game.Pixel, panel, new Color(0, 0, 0) * 0.72f);
            _game.Text.DrawShadowed(batch, "WASD MOVE  LEFT/E ACTION  RIGHT CLICK EAT", new Vector2(270, 525), Color.White, 2);
            _game.Text.DrawShadowed(batch, "B CYCLE BUILD  CLICK PLACE  H HIRE  1-5 CRAFT", new Vector2(270, 555), Color.White, 2);
            _game.Text.DrawShadowed(batch, "F5 SAVE  ESC PAUSE  ANDROID TOUCH JOYSTICK/ACTION", new Vector2(270, 585), Color.White, 2);
            _game.Text.DrawShadowed(batch, "RIVERS BLOCK RAIDERS. BUILD WHERE WATER FORCES CHOKEPOINTS.", new Vector2(270, 615), new Color(160, 230, 255), 2);
        }
        batch.End();
    }
}
