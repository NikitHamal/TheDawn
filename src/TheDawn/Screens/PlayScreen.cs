using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheDawn.Assets;
using TheDawn.Data;
using TheDawn.Entities;
using TheDawn.Input;
using TheDawn.Persistence;
using TheDawn.Rendering;
using TheDawn.Systems;
using TheDawn.World;

namespace TheDawn.Screens;

public sealed class PlayScreen : IGameScreen
{
    private readonly DawnGame _game;
    private readonly GameSession _session;
    private readonly Camera2D _camera = new();
    private bool _paused;
    private Vector2 _lastPointerWorld;

    public PlayScreen(DawnGame game, GameSession session)
    {
        _game = game;
        _session = session;
    }

    public void Enter() { }

    public void Exit()
    {
        if (!_session.IsPermadead) SaveSystem.Save(_session);
    }

    public void Update(GameTime gameTime, InputState input)
    {
        _camera.Position = Vector2.Lerp(_camera.Position, _session.Player.Position, 0.14f);
        _lastPointerWorld = _camera.ScreenToWorld(input.Pointer, _game.BackBufferWidth, _game.BackBufferHeight);
        if (input.PausePressed)
        {
            _paused = !_paused;
            if (!_paused) _session.SetMessage("Returned to the jungle.", 2f);
        }
        if (_session.IsPermadead && input.ConfirmPressed)
        {
            _game.ChangeScreen(new MainMenuScreen(_game));
            return;
        }
        if (_paused)
        {
            if (input.SavePressed)
            {
                SaveSystem.Save(_session);
                _session.SetMessage("Saved.", 2f);
            }
            return;
        }
        _session.Update(gameTime, input, _lastPointerWorld);
        if (input.SavePressed && !_session.IsPermadead)
        {
            SaveSystem.Save(_session);
            _session.SetMessage("Saved.", 2f);
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch batch)
    {
        batch.Begin(samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.Deferred, blendState: BlendState.AlphaBlend);
        DrawWorld(batch, gameTime);
        DrawEntities(batch, gameTime);
        DrawLighting(batch);
        DrawUi(batch);
        if (_paused) DrawPause(batch);
        batch.End();
    }

    private void DrawWorld(SpriteBatch batch, GameTime gameTime)
    {
        var bounds = _camera.VisibleWorldBounds(_game.BackBufferWidth, _game.BackBufferHeight);
        var minTileX = Math.Max(-100000, (int)MathF.Floor(bounds.Left / (float)GameConfig.TileSize) - 1);
        var maxTileX = (int)MathF.Ceiling(bounds.Right / (float)GameConfig.TileSize) + 1;
        var minTileY = (int)MathF.Floor(bounds.Top / (float)GameConfig.TileSize) - 1;
        var maxTileY = (int)MathF.Ceiling(bounds.Bottom / (float)GameConfig.TileSize) + 1;
        for (var y = minTileY; y <= maxTileY; y++)
        {
            for (var x = minTileX; x <= maxTileX; x++)
            {
                var tile = _session.World.GetTile(x, y);
                var tileRect = new Rectangle(x * GameConfig.TileSize, y * GameConfig.TileSize, GameConfig.TileSize, GameConfig.TileSize);
                DrawTextureWorld(batch, _game.Assets.Texture("terrain_atlas"), tileRect, AssetStore.TerrainSource(tile, x, y), Color.White);
            }
        }

        foreach (var node in _session.World.NodesIn(bounds)) DrawNode(batch, node);
        foreach (var structure in _session.World.Structures.Where(s => !s.IsDestroyed)) DrawStructure(batch, structure, gameTime);

        if (_session.BuildMode)
        {
            var tile = GameWorld.WorldToTile(_lastPointerWorld);
            var rect = new Rectangle(tile.X * GameConfig.TileSize, tile.Y * GameConfig.TileSize, GameConfig.TileSize, GameConfig.TileSize);
            var can = _session.World.CanPlaceStructure(_session.SelectedStructure, tile) && Vector2.Distance(tile.CenterWorld, _session.Player.Position) <= 190f;
            DrawTextureWorld(batch, _game.Pixel, rect, null, can ? new Color(80, 255, 120) * 0.35f : new Color(255, 70, 70) * 0.35f);
        }
    }

    private void DrawNode(SpriteBatch batch, ResourceNode node)
    {
        var visual = NodeVisual(node);
        DrawSpriteWorld(batch, _game.Assets.Texture("objects_atlas"), node.Tile.CenterWorld + visual.Offset, visual.Source, visual.Size, new Vector2(0.5f, 1f), Color.White, SpriteEffects.None);
        if (node.Health < node.MaxHealth)
        {
            var screen = _camera.WorldToScreen(node.Tile.CenterWorld + new Vector2(-16, -20), _game.BackBufferWidth, _game.BackBufferHeight);
            DrawHelpers.DrawBar(batch, _game.Pixel, new Rectangle((int)screen.X, (int)screen.Y, 32, 5), node.Health / (float)node.MaxHealth, Color.Black * 0.8f, new Color(90, 220, 90));
        }
    }

    private static (Rectangle Source, Vector2 Size, Vector2 Offset) NodeVisual(ResourceNode node)
    {
        var variant = (int)((node.Id >> 8) & 7);
        return node.Type switch
        {
            ResourceType.Tree => (AssetStore.ObjectCell(variant % 6), new Vector2(64, 64), new Vector2(0, 18)),
            ResourceType.BerryBush => (AssetStore.ObjectCell(6 + (variant & 1)), new Vector2(42, 42), new Vector2(0, 13)),
            ResourceType.WildCrop => (AssetStore.ObjectCell(10), new Vector2(34, 24), new Vector2(0, 13)),
            ResourceType.Rock => (AssetStore.ObjectCell(12 + (variant % 3)), new Vector2(42, 42), new Vector2(0, 13)),
            ResourceType.IronOre => (AssetStore.ObjectCell(16), new Vector2(34, 34), new Vector2(0, 13)),
            ResourceType.GoldVein => (AssetStore.ObjectCell(17), new Vector2(34, 34), new Vector2(0, 13)),
            ResourceType.CrystalDeposit => (AssetStore.ObjectCell(15), new Vector2(44, 44), new Vector2(0, 13)),
            _ => (AssetStore.ObjectCell(12), new Vector2(34, 34), new Vector2(0, 13))
        };
    }

    private void DrawStructure(SpriteBatch batch, Structure structure, GameTime gameTime)
    {
        var pos = structure.Tile.CenterWorld + new Vector2(0, GameConfig.TileSize / 2f);
        switch (structure.Type)
        {
            case StructureType.WoodWall:
                DrawAtlasObject(batch, 32, pos, new Vector2(46, 34));
                break;
            case StructureType.StoneWall:
                DrawAtlasObject(batch, 33, pos, new Vector2(46, 34));
                break;
            case StructureType.IronWall:
                DrawAtlasObject(batch, 34, pos, new Vector2(46, 34));
                break;
            case StructureType.CrystalWall:
                DrawAtlasObject(batch, 35, pos, new Vector2(46, 34));
                break;
            case StructureType.Gate:
                DrawAtlasObject(batch, 36, pos, new Vector2(50, 50));
                break;
            case StructureType.Campfire:
                DrawAtlasObject(batch, 20, pos, new Vector2(40, 52));
                break;
            case StructureType.Workbench:
                DrawAtlasObject(batch, 21, pos, new Vector2(58, 48));
                break;
            case StructureType.Sawmill:
                DrawAtlasObject(batch, 22, pos, new Vector2(64, 52));
                break;
            case StructureType.Furnace:
                DrawAtlasObject(batch, 23, pos, new Vector2(54, 54));
                break;
            case StructureType.Anvil:
                DrawAtlasObject(batch, 24, pos, new Vector2(56, 44));
                break;
            case StructureType.AlchemyTable:
                DrawAtlasObject(batch, 25, pos, new Vector2(52, 52));
                break;
            case StructureType.Watchtower:
                DrawAtlasObject(batch, 26, pos, new Vector2(46, 62));
                break;
            case StructureType.Barracks:
                DrawAtlasObject(batch, 27, pos, new Vector2(64, 64));
                break;
            case StructureType.SpikeTrap:
                DrawAtlasObject(batch, 28, pos, new Vector2(30, 30));
                break;
            case StructureType.FarmPlot:
                DrawFarmPlot(batch, structure);
                break;
        }
        if (structure.Health < structure.MaxHealth)
        {
            var screen = _camera.WorldToScreen(structure.Tile.CenterWorld + new Vector2(-18, -22), _game.BackBufferWidth, _game.BackBufferHeight);
            DrawHelpers.DrawBar(batch, _game.Pixel, new Rectangle((int)screen.X, (int)screen.Y, 36, 5), structure.Health / (float)structure.MaxHealth, Color.Black * 0.8f, new Color(200, 170, 80));
        }
    }

    private void DrawAtlasObject(SpriteBatch batch, int cell, Vector2 worldBottomCenter, Vector2 size)
    {
        DrawSpriteWorld(batch, _game.Assets.Texture("objects_atlas"), worldBottomCenter, AssetStore.ObjectCell(cell), size, new Vector2(0.5f, 1f), Color.White, SpriteEffects.None);
    }

    private void DrawFarmPlot(SpriteBatch batch, Structure structure)
    {
        var pos = structure.Tile.CenterWorld + new Vector2(0, GameConfig.TileSize / 2f);
        DrawAtlasObject(batch, 29, pos, new Vector2(42, 42));
        if (structure.Growth > 1)
        {
            var cell = structure.Growth >= 4 ? 11 : 10;
            var size = structure.Growth >= 4 ? new Vector2(30, 30) : new Vector2(30, 22);
            DrawAtlasObject(batch, cell, structure.Tile.CenterWorld + new Vector2(0, 13), size);
        }
    }

    private void DrawEntities(SpriteBatch batch, GameTime gameTime)
    {
        foreach (var unit in _session.Units.Where(u => u.IsAlive).OrderBy(u => u.Position.Y)) DrawUnit(batch, unit);
        foreach (var enemy in _session.Enemies.Where(e => e.IsAlive).OrderBy(e => e.Position.Y)) DrawEnemy(batch, enemy);
        DrawPlayer(batch, _session.Player);
        foreach (var projectile in _session.Projectiles)
        {
            var rect = new Rectangle((int)(projectile.Position.X - 3), (int)(projectile.Position.Y - 3), 6, 6);
            DrawTextureWorld(batch, _game.Pixel, rect, null, projectile.FromPlayerSide ? new Color(255, 230, 80) : new Color(200, 80, 255));
        }
    }

    private void DrawPlayer(SpriteBatch batch, Player player)
    {
        var moving = player.Velocity.LengthSquared() > 0.01f;
        var texId = (moving, player.Facing) switch
        {
            (true, Facing.Up) => "player_run_up",
            (true, Facing.Left or Facing.Right) => "player_run_side",
            (true, _) => "player_run_down",
            (false, Facing.Up) => "player_idle_up",
            (false, Facing.Left or Facing.Right) => "player_idle_side",
            _ => "player_idle_down"
        };
        var frames = moving ? 6 : 4;
        var frame = ((int)(player.AnimationTime / (moving ? 0.11 : 0.22)) % frames) * 64;
        var effect = player.Facing == Facing.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        DrawSpriteWorld(batch, _game.Assets.Texture(texId), player.Position + new Vector2(0, 18), new Rectangle(frame, 0, 64, 64), new Vector2(44, 44), new Vector2(0.5f, 1f), Color.White, effect);
    }

    private void DrawEnemy(SpriteBatch batch, Enemy enemy)
    {
        var texId = enemy.Type switch
        {
            EnemyType.SkeletonWarrior => "skeleton_warrior_run",
            EnemyType.SkeletonMage or EnemyType.SkeletonArcher => "skeleton_mage_run",
            EnemyType.OrcRogue => "orc_rogue_run",
            EnemyType.OrcWarrior or EnemyType.RaidLeader or EnemyType.DungeonBoss => "orc_warrior_run",
            EnemyType.OrcShaman => "orc_shaman_run",
            _ => "skeleton_rogue_run"
        };
        var frame = ((int)(enemy.AnimationTime / 0.14) % 6) * 64;
        var effect = enemy.Facing == Facing.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        var size = enemy.Type == EnemyType.DungeonBoss ? new Vector2(72, 72) : enemy.Type == EnemyType.RaidLeader ? new Vector2(56, 56) : new Vector2(42, 42);
        DrawSpriteWorld(batch, _game.Assets.Texture(texId), enemy.Position + new Vector2(0, 18), new Rectangle(frame, 0, 64, 64), size, new Vector2(0.5f, 1f), Color.White, effect);
        var screen = _camera.WorldToScreen(enemy.Position + new Vector2(-18, -28), _game.BackBufferWidth, _game.BackBufferHeight);
        DrawHelpers.DrawBar(batch, _game.Pixel, new Rectangle((int)screen.X, (int)screen.Y, 36, 5), enemy.Health / (float)enemy.MaxHealth, Color.Black * 0.8f, new Color(210, 70, 60));
    }

    private void DrawUnit(SpriteBatch batch, HiredUnit unit)
    {
        var texId = unit.Type switch
        {
            UnitType.Swordsman => "knight_run",
            UnitType.Archer => "rogue_run",
            UnitType.Mage => "wizard_run",
            UnitType.Miner => "rogue_run",
            UnitType.Farmer => "knight_run",
            _ => "knight_run"
        };
        var frame = ((int)(unit.AnimationTime / 0.16) % 6) * 64;
        var effect = unit.Facing == Facing.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        DrawSpriteWorld(batch, _game.Assets.Texture(texId), unit.Position + new Vector2(0, 18), new Rectangle(frame, 0, 64, 64), new Vector2(42, 42), new Vector2(0.5f, 1f), Color.White, effect);
        var screen = _camera.WorldToScreen(unit.Position + new Vector2(-17, -26), _game.BackBufferWidth, _game.BackBufferHeight);
        DrawHelpers.DrawBar(batch, _game.Pixel, new Rectangle((int)screen.X, (int)screen.Y, 34, 5), unit.Health / (float)unit.MaxHealth, Color.Black * 0.8f, new Color(75, 195, 255));
    }

    private void DrawLighting(SpriteBatch batch)
    {
        var darkness = _session.Time.Phase switch
        {
            GamePhase.Day => 0f,
            GamePhase.Dusk => (float)_session.Time.PhaseProgress * 0.35f,
            GamePhase.Night => 0.55f,
            GamePhase.Dawn => 0.42f * (1f - (float)_session.Time.PhaseProgress),
            _ => 0f
        };
        if (darkness > 0)
        {
            batch.Draw(_game.Pixel, new Rectangle(0, 0, _game.BackBufferWidth, _game.BackBufferHeight), new Color(0, 0, 20) * darkness);
        }
    }

    private void DrawUi(SpriteBatch batch)
    {
        batch.Draw(_game.Pixel, new Rectangle(0, 0, _game.BackBufferWidth, 96), new Color(0, 0, 0) * 0.62f);
        _game.Text.DrawShadowed(batch, _session.Time.ClockLabel(), new Vector2(18, 16), new Color(255, 224, 128), 3);
        DrawHelpers.DrawBar(batch, _game.Pixel, new Rectangle(20, 58, 190, 14), _session.Player.Health / (float)_session.Player.MaxHealth, Color.Black, new Color(200, 50, 50));
        DrawHelpers.DrawBar(batch, _game.Pixel, new Rectangle(20, 77, 190, 14), _session.Player.Hunger / (float)GameConfig.PlayerMaxHunger, Color.Black, new Color(230, 170, 65));
        _game.Text.DrawShadowed(batch, $"HP {_session.Player.Health}/{_session.Player.MaxHealth}", new Vector2(218, 57), Color.White, 2);
        _game.Text.DrawShadowed(batch, $"FOOD {_session.Player.Hunger}", new Vector2(218, 76), Color.White, 2);
        var inv = _session.Player.Inventory;
        var invText = $"WOOD {inv[ItemId.Wood]}  STONE {inv[ItemId.Stone]}  FOOD {inv[ItemId.Food]}  FISH {inv[ItemId.Fish]}  IRON {inv[ItemId.IronOre]}/{inv[ItemId.IronIngot]}  CRYSTAL {inv[ItemId.Crystal]}  GOLD {inv[ItemId.Gold]}";
        _game.Text.DrawShadowed(batch, invText, new Vector2(440, 18), Color.White, 2);
        var build = GameBalance.Structures[_session.SelectedStructure];
        var hire = GameBalance.Units[_session.SelectedHire];
        _game.Text.DrawShadowed(batch, $"B BUILD: {build.Name}   H HIRE: {hire.Name}   UNITS {_session.Units.Count}   ENEMIES {_session.Enemies.Count}", new Vector2(440, 49), new Color(190, 235, 190), 2);
        if (_session.ShouldShowMessage)
        {
            batch.Draw(_game.Pixel, new Rectangle(260, _game.BackBufferHeight - 80, 760, 44), new Color(0, 0, 0) * 0.72f);
            _game.Text.DrawShadowed(batch, _session.Message, new Vector2(280, _game.BackBufferHeight - 66), new Color(255, 240, 200), 2);
        }
#if ANDROID
        DrawMobileOverlay(batch);
#endif
    }

    private void DrawMobileOverlay(SpriteBatch batch)
    {
        var h = _game.BackBufferHeight;
        var w = _game.BackBufferWidth;
        batch.Draw(_game.Pixel, new Rectangle((int)(w * 0.12f), (int)(h * 0.70f), 96, 96), new Color(255, 255, 255) * 0.08f);
        _game.Text.DrawShadowed(batch, "MOVE", new Vector2(w * 0.12f + 14, h * 0.70f + 36), Color.White * 0.35f, 2);
        batch.Draw(_game.Pixel, new Rectangle(w - 150, h - 150, 96, 96), new Color(255, 255, 255) * 0.08f);
        _game.Text.DrawShadowed(batch, "ACT", new Vector2(w - 128, h - 112), Color.White * 0.35f, 2);
    }

    private void DrawPause(SpriteBatch batch)
    {
        batch.Draw(_game.Pixel, new Rectangle(0, 0, _game.BackBufferWidth, _game.BackBufferHeight), Color.Black * 0.55f);
        _game.Text.DrawShadowed(batch, "PAUSED", new Vector2(520, 260), new Color(255, 224, 128), 5);
        _game.Text.DrawShadowed(batch, "ESC RESUME  F5 SAVE", new Vector2(470, 340), Color.White, 3);
    }

    private void DrawTextureWorld(SpriteBatch batch, Texture2D texture, Rectangle worldRect, Rectangle? source, Color color)
    {
        var topLeft = _camera.WorldToScreen(new Vector2(worldRect.X, worldRect.Y), _game.BackBufferWidth, _game.BackBufferHeight);
        var dest = new Rectangle((int)MathF.Round(topLeft.X), (int)MathF.Round(topLeft.Y), (int)MathF.Ceiling(worldRect.Width * _camera.Zoom), (int)MathF.Ceiling(worldRect.Height * _camera.Zoom));
        batch.Draw(texture, dest, source, color);
    }

    private void DrawSpriteWorld(SpriteBatch batch, Texture2D texture, Vector2 worldBottomCenter, Rectangle? source, Vector2 worldSize, Vector2 normalizedOrigin, Color color, SpriteEffects effects)
    {
        var screen = _camera.WorldToScreen(worldBottomCenter, _game.BackBufferWidth, _game.BackBufferHeight);
        var width = worldSize.X * _camera.Zoom;
        var height = worldSize.Y * _camera.Zoom;
        var dest = new Rectangle((int)MathF.Round(screen.X - width * normalizedOrigin.X), (int)MathF.Round(screen.Y - height * normalizedOrigin.Y), (int)MathF.Round(width), (int)MathF.Round(height));
        batch.Draw(texture, dest, source, color, 0f, Vector2.Zero, effects, 0f);
    }
}
