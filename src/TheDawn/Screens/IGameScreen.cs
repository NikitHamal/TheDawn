using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheDawn.Input;

namespace TheDawn.Screens;

public interface IGameScreen
{
    void Enter();
    void Exit();
    void Update(GameTime gameTime, InputState input);
    void Draw(GameTime gameTime, SpriteBatch spriteBatch);
}
