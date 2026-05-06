using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheDawn.Assets;
using TheDawn.Input;
using TheDawn.Rendering;
using TheDawn.Screens;

namespace TheDawn;

public sealed class DawnGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private IGameScreen? _screen;
    private InputState? _input;

    public AssetStore Assets { get; private set; } = null!;
    public PixelText Text { get; private set; } = null!;
    public Texture2D Pixel { get; private set; } = null!;
    public int BackBufferWidth => GraphicsDevice.PresentationParameters.BackBufferWidth;
    public int BackBufferHeight => GraphicsDevice.PresentationParameters.BackBufferHeight;

    public DawnGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = GameConfig.VirtualWidth,
            PreferredBackBufferHeight = GameConfig.VirtualHeight,
            SynchronizeWithVerticalRetrace = true
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
    }

    protected override void Initialize()
    {
#if ANDROID
        _graphics.IsFullScreen = true;
        _graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
        _graphics.ApplyChanges();
#endif
        _input = new InputState();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        Pixel = new Texture2D(GraphicsDevice, 1, 1);
        Pixel.SetData(new[] { Color.White });
        Text = new PixelText(Pixel);
        Assets = new AssetStore(GraphicsDevice);
        Assets.LoadAll();
        ChangeScreen(new MainMenuScreen(this));
    }

    public void ChangeScreen(IGameScreen next)
    {
        _screen?.Exit();
        _screen = next;
        _screen.Enter();
    }

    protected override void Update(GameTime gameTime)
    {
        if (_input == null) return;
        _input.Update(this);
        _screen?.Update(gameTime, _input);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(9, 12, 15));
        if (_spriteBatch != null)
        {
            _screen?.Draw(gameTime, _spriteBatch);
        }
        base.Draw(gameTime);
    }
}
