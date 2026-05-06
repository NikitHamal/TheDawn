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
                DrawTerrainTile(batch, tileRect, tile, x, y);
            }
        }

        foreach (var decoration in _session.World.DecorationsIn(bounds)) DrawDecoration(batch, decoration);

        foreach (var node in _session.World.NodesIn(bounds).OrderBy(n => n.Tile.CenterWorld.Y)) DrawNode(batch, node);
        foreach (var structure in _session.World.Structures.Where(s => !s.IsDestroyed).OrderBy(s => s.Tile.CenterWorld.Y)) DrawStructure(batch, structure, gameTime);

        if (_session.BuildMode)
        {
            var tile = GameWorld.WorldToTile(_lastPointerWorld);
            var rect = new Rectangle(tile.X * GameConfig.TileSize, tile.Y * GameConfig.TileSize, GameConfig.TileSize, GameConfig.TileSize);
            var can = _session.World.CanPlaceStructure(_session.SelectedStructure, tile) && Vector2.Distance(tile.CenterWorld, _session.Player.Position) <= 190f;
            DrawTextureWorld(batch, _game.Pixel, rect, null, can ? new Color(80, 255, 120) * 0.35f : new Color(255, 70, 70) * 0.35f);
        }
    }

    private void DrawDecoration(SpriteBatch batch, WorldDecoration decoration)
    {
        var visual = DecorationVisual(decoration);
        var worldBottom = decoration.Tile.CenterWorld + visual.Offset;
        DrawSpriteWorld(batch, _game.Assets.Texture(visual.Texture), worldBottom, visual.Source, visual.Size * decoration.Scale, new Vector2(0.5f, 1f), Color.White, decoration.Flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
    }

    private static (string Texture, Rectangle Source, Vector2 Size, Vector2 Offset) DecorationVisual(WorldDecoration decoration)
    {
        var v = decoration.Variant;
        return decoration.Type switch
        {
            DecorationType.GrassTuft => ("vegetation", new Rectangle((v % 8) * 16, 144 + (v / 8) * 16, 16, 16), new Vector2(18, 18), new Vector2(0, 18) + decoration.Offset),
            DecorationType.Fern => ("vegetation", new Rectangle(64 + (v % 4) * 16, 160 + (v / 4 % 4) * 16, 16, 24), new Vector2(20, 26), new Vector2(0, 18) + decoration.Offset),
            DecorationType.Flower => ("vegetation", new Rectangle(208 + (v % 4) * 16, 152 + (v / 4 % 3) * 32, 16, 32), new Vector2(18, 32), new Vector2(0, 18) + decoration.Offset),
            DecorationType.Mushroom => ("vegetation", new Rectangle((v % 6) * 16, 300, 16, 16), new Vector2(18, 18), new Vector2(0, 18) + decoration.Offset),
            DecorationType.FallenLeaves => ("vegetation", new Rectangle(112 + (v % 6) * 16, 300, 16, 16), new Vector2(20, 16), new Vector2(0, 18) + decoration.Offset),
            DecorationType.Branch => ("resources", new Rectangle(160 + (v % 6) * 16, 176, 16, 16), new Vector2(22, 16), new Vector2(0, 18) + decoration.Offset),
            DecorationType.Pebble => ("rocks", new Rectangle((v % 4) * 16, 160 + (v / 4 % 4) * 16, 16, 16), new Vector2(18, 18), new Vector2(0, 18) + decoration.Offset),
            DecorationType.Stump => ("vegetation", new Rectangle(352, 0, 32, 32), new Vector2(28, 28), new Vector2(0, 18) + decoration.Offset),
            DecorationType.WaterFoam => ("water", new Rectangle((v % 2) * 80, 80, 80, 80), new Vector2(34, 22), new Vector2(0, 18) + decoration.Offset),
            DecorationType.Reed => ("vegetation", new Rectangle(128 + (v % 4) * 16, 176, 16, 32), new Vector2(18, 30), new Vector2(0, 18) + decoration.Offset),
            DecorationType.CaveDebris => ("dungeon_props", new Rectangle((v % 5) * 32, 224, 32, 32), new Vector2(24, 24), new Vector2(0, 18) + decoration.Offset),
            DecorationType.DungeonRelic => ("dungeon_props", new Rectangle((v % 5) * 32, 0, 32, 48), new Vector2(28, 38), new Vector2(0, 18) + decoration.Offset),
            DecorationType.RuinDebris => ("building_props", new Rectangle((v % 5) * 32, 0, 32, 32), new Vector2(30, 28), new Vector2(0, 18) + decoration.Offset),
            DecorationType.SnowClump => ("floors", new Rectangle((v % 2) * 80, 160, 80, 80), new Vector2(34, 22), new Vector2(0, 18) + decoration.Offset),
            DecorationType.CrystalShard => ("resources", new Rectangle(240, 128, 32, 32), new Vector2(24, 28), new Vector2(0, 18) + decoration.Offset),
            _ => ("vegetation", AssetStore.Cell16(0, 14), new Vector2(16, 16), new Vector2(0, 18) + decoration.Offset)
        };
    }

    private void DrawNode(SpriteBatch batch, ResourceNode node)
    {
        var visual = NodeVisual(node);
        DrawSpriteWorld(batch, _game.Assets.Texture(visual.Texture), node.Tile.CenterWorld + visual.Offset, visual.Source, visual.Size, new Vector2(0.5f, 1f), Color.White, visual.Effects);
        if (node.Health < node.MaxHealth)
        {
            var screen = _camera.WorldToScreen(node.Tile.CenterWorld + new Vector2(-16, -20), _game.BackBufferWidth, _game.BackBufferHeight);
            DrawHelpers.DrawBar(batch, _game.Pixel, new Rectangle((int)screen.X, (int)screen.Y, 32, 5), node.Health / (float)node.MaxHealth, Color.Black * 0.8f, new Color(90, 220, 90));
        }
    }

    private static (string Texture, Rectangle Source, Vector2 Size, Vector2 Offset, SpriteEffects Effects) NodeVisual(ResourceNode node)
    {
        var variant = (int)((node.Id >> 8) & 15);
        var jitter = NodeJitter(node.Id, node.Type == ResourceType.Tree ? 7f : 5f);
        var effect = ((node.Id >> 18) & 1) == 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        if (node.Type == ResourceType.Tree)
        {
            return variant switch
            {
                0 => ("tree_m1_s2", new Rectangle(0, 0, 64, 64), new Vector2(76, 76), new Vector2(jitter.X, 18 + jitter.Y), effect),
                1 => ("tree_m1_s2", new Rectangle(64, 0, 64, 64), new Vector2(76, 76), new Vector2(jitter.X, 18 + jitter.Y), effect),
                2 => ("tree_m1_s3", new Rectangle(0, 0, 52, 96), new Vector2(68, 118), new Vector2(jitter.X, 18 + jitter.Y), effect),
                3 => ("tree_m1_s3", new Rectangle(52, 0, 52, 96), new Vector2(68, 118), new Vector2(jitter.X, 18 + jitter.Y), effect),
                4 => ("tree_m2_s2", new Rectangle(0, 0, 64, 48), new Vector2(78, 60), new Vector2(jitter.X, 18 + jitter.Y), effect),
                5 => ("tree_m2_s2", new Rectangle(64, 0, 64, 48), new Vector2(78, 60), new Vector2(jitter.X, 18 + jitter.Y), effect),
                6 => ("tree_m2_s3", new Rectangle(0, 0, 72, 80), new Vector2(86, 98), new Vector2(jitter.X, 18 + jitter.Y), effect),
                7 => ("tree_m3_s2", new Rectangle(0, 0, 64, 80), new Vector2(74, 94), new Vector2(jitter.X, 18 + jitter.Y), effect),
                8 => ("tree_m3_s2", new Rectangle(64, 0, 64, 80), new Vector2(74, 94), new Vector2(jitter.X, 18 + jitter.Y), effect),
                9 => ("tree_m3_s3", new Rectangle(0, 0, 96, 144), new Vector2(96, 144), new Vector2(jitter.X, 18 + jitter.Y), effect),
                _ => ("tree_m1_s2", new Rectangle((variant % 4) * 64, (variant / 4 % 2) * 64, 64, 64), new Vector2(76, 76), new Vector2(jitter.X, 18 + jitter.Y), effect)
            };
        }
        return node.Type switch
        {
            ResourceType.BerryBush => ("vegetation", AssetStore.Cell32(variant % 4, (variant / 4) % 3), new Vector2(46, 46), new Vector2(jitter.X, 17 + jitter.Y), SpriteEffects.None),
            ResourceType.WildCrop => ("farm", AssetStore.Cell32(1 + (variant % 5), 2 + ((variant / 5) % 3)), new Vector2(34, 30), new Vector2(jitter.X, 16 + jitter.Y), SpriteEffects.None),
            ResourceType.Rock => ("rocks", AssetStore.Cell32(variant % 6, (variant / 6) % 3), new Vector2(44, 44), new Vector2(jitter.X, 17 + jitter.Y), SpriteEffects.None),
            ResourceType.IronOre => ("rocks", AssetStore.Cell32(3 + (variant % 3), 0), new Vector2(38, 38), new Vector2(jitter.X, 17 + jitter.Y), SpriteEffects.None),
            ResourceType.GoldVein => ("rocks", AssetStore.Cell32(0 + (variant % 3), 3), new Vector2(38, 38), new Vector2(jitter.X, 17 + jitter.Y), SpriteEffects.None),
            ResourceType.CrystalDeposit => ("rocks", new Rectangle(144 + (variant % 2) * 16, 272, 16, 32), new Vector2(30, 42), new Vector2(jitter.X, 17 + jitter.Y), SpriteEffects.None),
            _ => ("rocks", AssetStore.Cell32(0, 0), new Vector2(34, 34), new Vector2(jitter.X, 17 + jitter.Y), SpriteEffects.None)
        };
    }

    private static Vector2 NodeJitter(long id, float amount)
    {
        var x = (((id >> 21) & 255) / 255f - 0.5f) * amount * 2f;
        var y = (((id >> 11) & 255) / 255f - 0.5f) * amount * 2f;
        return new Vector2(x, y);
    }

    private void DrawStructure(SpriteBatch batch, Structure structure, GameTime gameTime)
    {
        var pos = structure.Tile.CenterWorld + new Vector2(0, GameConfig.TileSize / 2f);
        switch (structure.Type)
        {
            case StructureType.WoodWall:
                DrawStructureSprite(batch, "building_walls", AssetStore.Cell80(0, 2), pos, new Vector2(52, 38));
                break;
            case StructureType.StoneWall:
                DrawStructureSprite(batch, "walls", AssetStore.Cell80(1, 3), pos, new Vector2(52, 38));
                break;
            case StructureType.IronWall:
                DrawStructureSprite(batch, "walls", AssetStore.Cell80(2, 3), pos, new Vector2(52, 38));
                break;
            case StructureType.CrystalWall:
                DrawStructureSprite(batch, "building_walls", AssetStore.Cell80(0, 5), pos, new Vector2(54, 42));
                break;
            case StructureType.Gate:
                DrawStructureSprite(batch, "building_walls", AssetStore.Cell80(1, 2), pos, new Vector2(58, 48));
                break;
            case StructureType.Campfire:
                DrawStructureSprite(batch, "campfire", AssetStore.Cell64(0, 1), pos, new Vector2(42, 52));
                DrawAnimatedSheet(batch, "fire", pos + new Vector2(0, -10), 32, 48, new Vector2(40, 50), gameTime, 0.11f);
                DrawAnimatedSheet(batch, "smoke", pos + new Vector2(2, -34), 32, 48, new Vector2(34, 44), gameTime, 0.18f, Color.White * 0.75f);
                break;
            case StructureType.Workbench:
                DrawStructureSprite(batch, "workbench", AssetStore.Cell64(1, 1), pos, new Vector2(58, 48));
                break;
            case StructureType.Sawmill:
                DrawStructureSprite(batch, "sawmill", new Rectangle(0, 0, 96, 80), pos, new Vector2(68, 56));
                break;
            case StructureType.Furnace:
                DrawStructureSprite(batch, "furnace", AssetStore.Cell64(1, 1), pos, new Vector2(56, 56));
                DrawAnimatedSheet(batch, "fire", pos + new Vector2(0, -12), 32, 48, new Vector2(30, 34), gameTime, 0.13f);
                break;
            case StructureType.Anvil:
                DrawStructureSprite(batch, "anvil", new Rectangle(64, 0, 96, 80), pos, new Vector2(60, 48));
                break;
            case StructureType.AlchemyTable:
                DrawAnimatedSheet(batch, "alchemy", pos, 80, 80, new Vector2(56, 56), gameTime, 0.14f);
                var bubble = 0.85f + 0.15f * MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * 5f);
                DrawSpriteWorld(batch, _game.Pixel, pos + new Vector2(10, -30), null, new Vector2(5, 5) * bubble, new Vector2(0.5f, 1f), new Color(120, 210, 255) * 0.75f, SpriteEffects.None);
                break;
            case StructureType.Watchtower:
                DrawStructureSprite(batch, "building_walls", AssetStore.Cell80(1, 5), pos, new Vector2(54, 70));
                break;
            case StructureType.Barracks:
                DrawStructureSprite(batch, "building_walls", AssetStore.Cell80(1, 0), pos, new Vector2(72, 72));
                break;
            case StructureType.SpikeTrap:
                DrawStructureSprite(batch, "dungeon_props", AssetStore.Cell32(2, 5), pos, new Vector2(30, 30));
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

    private void DrawAnimatedSheet(SpriteBatch batch, string textureId, Vector2 worldBottomCenter, int frameWidth, int frameHeight, Vector2 size, GameTime gameTime, float secondsPerFrame, Color? tint = null)
    {
        var texture = _game.Assets.Texture(textureId);
        var frames = Math.Max(1, texture.Width / frameWidth);
        var frame = (int)(gameTime.TotalGameTime.TotalSeconds / secondsPerFrame) % frames;
        DrawSpriteWorld(batch, texture, worldBottomCenter, new Rectangle(frame * frameWidth, 0, frameWidth, frameHeight), size, new Vector2(0.5f, 1f), tint ?? Color.White, SpriteEffects.None);
    }

    private void DrawStructureSprite(SpriteBatch batch, string textureId, Rectangle source, Vector2 worldBottomCenter, Vector2 size)
    {
        DrawSpriteWorld(batch, _game.Assets.Texture(textureId), worldBottomCenter, source, size, new Vector2(0.5f, 1f), Color.White, SpriteEffects.None);
    }

    private void DrawFarmPlot(SpriteBatch batch, Structure structure)
    {
        var pos = structure.Tile.CenterWorld + new Vector2(0, GameConfig.TileSize / 2f);
        DrawStructureSprite(batch, "farm", AssetStore.Cell32(10, 3), pos, new Vector2(42, 42));
        if (structure.Growth > 1)
        {
            var source = structure.Growth >= 4 ? AssetStore.Cell32(3, 2) : AssetStore.Cell32(2, 2);
            var size = structure.Growth >= 4 ? new Vector2(30, 30) : new Vector2(30, 22);
            DrawStructureSprite(batch, "farm", source, structure.Tile.CenterWorld + new Vector2(0, 13), size);
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
        var action = player.VisualActionTimer > 0 ? player.VisualAction : PlayerVisualAction.None;
        string texId;
        var frameWidth = 64;
        var frameHeight = 64;
        var secondsPerFrame = 0.11;
        if (action != PlayerVisualAction.None)
        {
            var prefix = action switch
            {
                PlayerVisualAction.Slice => "player_slice",
                PlayerVisualAction.Crush => "player_crush",
                PlayerVisualAction.Collect => "player_collect",
                PlayerVisualAction.Fish => "player_fishing",
                PlayerVisualAction.Water => "player_watering",
                PlayerVisualAction.Hit => "player_hit",
                _ => "player_collect"
            };
            texId = DirectionalTexture(prefix, player.Facing);
            secondsPerFrame = action == PlayerVisualAction.Fish ? 0.16 : 0.075;
        }
        else
        {
            texId = (moving, player.Facing) switch
            {
                (true, Facing.Up) => "player_run_up",
                (true, Facing.Left or Facing.Right) => "player_run_side",
                (true, _) => "player_run_down",
                (false, Facing.Up) => "player_idle_up",
                (false, Facing.Left or Facing.Right) => "player_idle_side",
                _ => "player_idle_down"
            };
            secondsPerFrame = moving ? 0.11 : 0.22;
        }
        var tex = _game.Assets.Texture(texId);
        var frames = Math.Max(1, tex.Width / frameWidth);
        var frame = ((int)(player.AnimationTime / secondsPerFrame) % frames) * frameWidth;
        var effect = player.Facing == Facing.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        DrawSpriteWorld(batch, tex, player.Position + new Vector2(0, 18), new Rectangle(frame, 0, frameWidth, frameHeight), new Vector2(46, 46), new Vector2(0.5f, 1f), Color.White, effect);
    }

    private static string DirectionalTexture(string prefix, Facing facing)
    {
        return facing switch
        {
            Facing.Up => prefix + "_up",
            Facing.Left or Facing.Right => prefix + "_side",
            _ => prefix + "_down"
        };
    }

    private void DrawEnemy(SpriteBatch batch, Enemy enemy)
    {
        var moving = enemy.Velocity.LengthSquared() > 0.001f;
        var texId = enemy.Type switch
        {
            EnemyType.SkeletonWarrior => moving ? "skeleton_warrior_run" : "skeleton_warrior_idle",
            EnemyType.SkeletonMage or EnemyType.SkeletonArcher => moving ? "skeleton_mage_run" : "skeleton_mage_idle",
            EnemyType.OrcRogue => moving ? "orc_rogue_run" : "orc_rogue_idle",
            EnemyType.OrcWarrior or EnemyType.RaidLeader or EnemyType.DungeonBoss => moving ? "orc_warrior_run" : "orc_warrior_idle",
            EnemyType.OrcShaman => moving ? "orc_shaman_run" : "orc_shaman_idle",
            _ => moving ? "skeleton_rogue_run" : "skeleton_rogue_idle"
        };
        var tex = _game.Assets.Texture(texId);
        var frameSize = tex.Height <= 32 ? 32 : 64;
        var frames = Math.Max(1, tex.Width / frameSize);
        var frame = ((int)(enemy.AnimationTime / (moving ? 0.14 : 0.23)) % frames) * frameSize;
        var effect = enemy.Facing == Facing.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        var size = enemy.Type == EnemyType.DungeonBoss ? new Vector2(72, 72) : enemy.Type == EnemyType.RaidLeader ? new Vector2(58, 58) : new Vector2(43, 43);
        DrawSpriteWorld(batch, tex, enemy.Position + new Vector2(0, 18), new Rectangle(frame, 0, frameSize, tex.Height), size, new Vector2(0.5f, 1f), Color.White, effect);
        var screen = _camera.WorldToScreen(enemy.Position + new Vector2(-18, -28), _game.BackBufferWidth, _game.BackBufferHeight);
        DrawHelpers.DrawBar(batch, _game.Pixel, new Rectangle((int)screen.X, (int)screen.Y, 36, 5), enemy.Health / (float)enemy.MaxHealth, Color.Black * 0.8f, new Color(210, 70, 60));
    }

    private void DrawUnit(SpriteBatch batch, HiredUnit unit)
    {
        var moving = unit.Velocity.LengthSquared() > 0.001f;
        var texId = unit.Type switch
        {
            UnitType.Swordsman => moving ? "knight_run" : "knight_idle",
            UnitType.Archer => moving ? "rogue_run" : "rogue_idle",
            UnitType.Mage => moving ? "wizard_run" : "wizard_idle",
            UnitType.Miner => moving ? "rogue_run" : "rogue_idle",
            UnitType.Farmer => moving ? "knight_run" : "knight_idle",
            _ => moving ? "knight_run" : "knight_idle"
        };
        var tex = _game.Assets.Texture(texId);
        var frameSize = tex.Height <= 32 ? 32 : 64;
        var frames = Math.Max(1, tex.Width / frameSize);
        var frame = ((int)(unit.AnimationTime / (moving ? 0.16 : 0.24)) % frames) * frameSize;
        var effect = unit.Facing == Facing.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        DrawSpriteWorld(batch, tex, unit.Position + new Vector2(0, 18), new Rectangle(frame, 0, frameSize, tex.Height), new Vector2(43, 43), new Vector2(0.5f, 1f), Color.White, effect);
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

    private void DrawTerrainTile(SpriteBatch batch, Rectangle worldRect, TileType tile, int tileX, int tileY)
    {
        var visual = AssetStore.TerrainVisual(tile, tileX, tileY);
        DrawTextureWorld(batch, _game.Pixel, worldRect, null, visual.BaseColor);
        DrawTextureWorld(batch, _game.Assets.Texture(visual.Texture), worldRect, visual.Source, visual.Tint);

        var detailHash = AssetStore.StableHash(tileX, tileY, 911);
        if (tile == TileType.Grass && (detailHash & 7) == 0)
        {
            var source = AssetStore.Cell16((detailHash >> 3) & 5, 14 + ((detailHash >> 7) & 1));
            var center = new Vector2(worldRect.Center.X, worldRect.Bottom - 4);
            DrawSpriteWorld(batch, _game.Assets.Texture("vegetation"), center, source, new Vector2(16, 16), new Vector2(0.5f, 1f), Color.White * 0.65f, SpriteEffects.None);
        }
        else if (tile == TileType.Water && (detailHash & 15) == 0)
        {
            var source = AssetStore.Cell80((detailHash >> 4) & 1, 1);
            DrawTextureWorld(batch, _game.Assets.Texture("water"), worldRect, source, Color.White * 0.35f);
        }
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
