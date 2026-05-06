using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace TheDawn.Input;

public sealed class InputState
{
    private KeyboardState _previousKeyboard;
    private KeyboardState _keyboard;
    private MouseState _previousMouse;
    private MouseState _mouse;
    private GamePadState _previousPad;
    private GamePadState _pad;
    private TouchCollection _touches;

    public Vector2 Move { get; private set; }
    public Vector2 Pointer { get; private set; }
    public bool HasPointer { get; private set; }
    public bool PrimaryPressed { get; private set; }
    public bool PrimaryHeld { get; private set; }
    public bool SecondaryPressed { get; private set; }
    public bool UsePressed { get; private set; }
    public bool BuildPressed { get; private set; }
    public bool CraftPressed { get; private set; }
    public bool HirePressed { get; private set; }
    public bool PausePressed { get; private set; }
    public bool ConfirmPressed { get; private set; }
    public bool BackPressed { get; private set; }
    public bool UpPressed { get; private set; }
    public bool DownPressed { get; private set; }
    public bool LeftPressed { get; private set; }
    public bool RightPressed { get; private set; }
    public bool SavePressed { get; private set; }
    public bool AnyPressed { get; private set; }
    public int NumberPressed { get; private set; }

    public void Update(DawnGame game)
    {
        _previousKeyboard = _keyboard;
        _previousMouse = _mouse;
        _previousPad = _pad;
        _keyboard = Keyboard.GetState();
        _mouse = Mouse.GetState();
        _pad = GamePad.GetState(PlayerIndex.One);
        _touches = TouchPanel.GetState();
        NumberPressed = -1;

        var move = Vector2.Zero;
        if (Down(Keys.W) || Down(Keys.Up)) move.Y -= 1;
        if (Down(Keys.S) || Down(Keys.Down)) move.Y += 1;
        if (Down(Keys.A) || Down(Keys.Left)) move.X -= 1;
        if (Down(Keys.D) || Down(Keys.Right)) move.X += 1;
        var stick = _pad.ThumbSticks.Left;
        move += new Vector2(stick.X, -stick.Y);

        HasPointer = true;
        Pointer = new Vector2(_mouse.X, _mouse.Y);
        PrimaryPressed = PressedMouseLeft();
        PrimaryHeld = _mouse.LeftButton == ButtonState.Pressed;
        SecondaryPressed = PressedMouseRight();

        foreach (var touch in _touches)
        {
            HasPointer = true;
            Pointer = touch.Position;
            if (touch.State == TouchLocationState.Pressed)
            {
                PrimaryPressed = true;
                PrimaryHeld = true;
            }
            if (touch.State == TouchLocationState.Moved || touch.State == TouchLocationState.Pressed)
            {
                if (touch.Position.X < game.BackBufferWidth * 0.32f && touch.Position.Y > game.BackBufferHeight * 0.52f)
                {
                    var center = new Vector2(game.BackBufferWidth * 0.16f, game.BackBufferHeight * 0.76f);
                    var delta = touch.Position - center;
                    if (delta.LengthSquared() > 16)
                    {
                        delta.Normalize();
                        move += delta;
                    }
                }
            }
        }

        if (move.LengthSquared() > 1f) move.Normalize();
        Move = move;

        UsePressed = Pressed(Keys.E) || Pressed(Buttons.A) || PrimaryPressed;
        BuildPressed = Pressed(Keys.B) || Pressed(Buttons.X);
        CraftPressed = Pressed(Keys.C) || Pressed(Buttons.Y);
        HirePressed = Pressed(Keys.H) || Pressed(Buttons.RightShoulder);
        PausePressed = Pressed(Keys.Escape) || Pressed(Buttons.Start);
        ConfirmPressed = Pressed(Keys.Enter) || Pressed(Keys.Space) || Pressed(Buttons.A) || PrimaryPressed;
        BackPressed = Pressed(Keys.Back) || Pressed(Keys.Escape) || Pressed(Buttons.B);
        UpPressed = Pressed(Keys.Up) || Pressed(Keys.W) || Pressed(Buttons.DPadUp);
        DownPressed = Pressed(Keys.Down) || Pressed(Keys.S) || Pressed(Buttons.DPadDown);
        LeftPressed = Pressed(Keys.Left) || Pressed(Keys.A) || Pressed(Buttons.DPadLeft);
        RightPressed = Pressed(Keys.Right) || Pressed(Keys.D) || Pressed(Buttons.DPadRight);
        SavePressed = Pressed(Keys.F5);

        var numberKeys = new[] { Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9 };
        for (var i = 0; i < numberKeys.Length; i++)
        {
            if (Pressed(numberKeys[i])) NumberPressed = i;
        }

        AnyPressed = _keyboard.GetPressedKeys().Length > 0 || _mouse.LeftButton == ButtonState.Pressed || _touches.Count > 0 || _pad.Buttons.A == ButtonState.Pressed;
    }

    private bool Down(Keys key) => _keyboard.IsKeyDown(key);
    private bool Pressed(Keys key) => _keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
    private bool Pressed(Buttons button) => _pad.IsButtonDown(button) && !_previousPad.IsButtonDown(button);
    private bool PressedMouseLeft() => _mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
    private bool PressedMouseRight() => _mouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Released;
}
