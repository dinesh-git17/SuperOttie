using SuperOttie.Core;
using SuperOttie.Entities;
using SuperOttie.Input;
using SuperOttie.Player;
using SuperOttie.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SuperOttie.Level
{
    public sealed class BuiltLevel
    {
        public GameObject Root;
        public PlayerController Player;
        public GoalPole Goal;

        /// <summary>Area the camera may show.</summary>
        public Rect CameraBounds;

        public void Destroy()
        {
            if (Root != null) Object.Destroy(Root);
        }
    }

    /// <summary>Turns <see cref="LevelData"/> into GameObjects: tilemap terrain, blocks, pickups, enemies, goal.</summary>
    public static class LevelBuilder
    {
        public static BuiltLevel Build(LevelContext ctx, IPlayerInput input, bool withBackground = true)
        {
            var data = ctx.Data;
            var assets = ctx.Assets;
            var root = new GameObject($"Level {data.Name}");
            ctx.Root = root.transform;

            BuildTerrain(data, assets, root.transform);
            BuildBounds(data, root.transform);

            var level = new BuiltLevel { Root = root };
            foreach (var spawn in data.Spawns)
            {
                var goal = SpawnEntity(ctx, spawn, root.transform);
                if (goal != null) level.Goal = goal;
            }

            var start = data.PlayerStart;
            level.Player = PlayerController.Spawn(ctx, input, new Vector2(start.x + 0.5f, start.y), root.transform);
            level.CameraBounds = new Rect(0f, 0f, data.Width, Mathf.Max(data.Height + 2f, PlatformerCamera.DefaultOrthographicSize * 2f));

            if (withBackground && ctx.Camera != null)
            {
                var theme = assets.GetTheme(data.Theme);
                ctx.Camera.backgroundColor = theme.skyColor;
                if (theme.background != null) ParallaxBackground.Create(theme.background, ctx.Camera, root.transform, Sorting.Background);
            }
            return level;
        }

        static void BuildTerrain(LevelData data, Game.GameAssets assets, Transform parent)
        {
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(parent, false);
            gridGo.AddComponent<Grid>();

            var mapGo = new GameObject("Terrain") { layer = Layers.Ground };
            mapGo.transform.SetParent(gridGo.transform, false);
            var map = mapGo.AddComponent<Tilemap>();
            var renderer = mapGo.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = Sorting.Terrain;
            renderer.mode = TilemapRenderer.Mode.Chunk;

            var rb = mapGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            var tileCollider = mapGo.AddComponent<TilemapCollider2D>();
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            var composite = mapGo.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.sharedMaterial = SpriteObjects.Frictionless;

            var grass = MakeTile(assets.tileGrass);
            var dirt = MakeTile(assets.tileDirt);
            var stone = MakeTile(assets.blockStone);

            for (int x = 0; x < data.Width; x++)
            for (int y = 0; y < data.Height; y++)
            {
                switch (data.GetTile(x, y))
                {
                    case TileKind.Ground:
                        bool exposed = !data.IsSolidTile(x, y + 1);
                        map.SetTile(new Vector3Int(x, y, 0), exposed ? grass : dirt);
                        break;
                    case TileKind.Stone:
                        map.SetTile(new Vector3Int(x, y, 0), stone);
                        break;
                }
            }
            map.CompressBounds();
            composite.GenerateGeometry();
        }

        static Tile MakeTile(Sprite sprite)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.Grid;
            return tile;
        }

        /// <summary>Invisible walls at both ends so nobody walks out of the level.</summary>
        static void BuildBounds(LevelData data, Transform parent)
        {
            var go = new GameObject("Bounds") { layer = Layers.Ground };
            go.transform.SetParent(parent, false);
            foreach (float x in new[] { 0f, data.Width })
            {
                var edge = go.AddComponent<EdgeCollider2D>();
                edge.points = new[] { new Vector2(x, -10f), new Vector2(x, data.Height + 20f) };
                edge.sharedMaterial = SpriteObjects.Frictionless;
            }
        }

        static GoalPole SpawnEntity(LevelContext ctx, Spawn spawn, Transform parent)
        {
            var a = ctx.Assets;
            var cell = spawn.Cell;
            var center = new Vector3(cell.x + 0.5f, cell.y + 0.5f);
            var feet = new Vector3(cell.x + 0.5f, cell.y);
            switch (spawn.Kind)
            {
                case SpawnKind.Coin:
                {
                    var go = NewObject("Coin", center, Layers.Item, parent);
                    var col = go.AddComponent<CircleCollider2D>();
                    col.radius = 0.36f;
                    col.isTrigger = true;
                    var sr = SpriteObjects.Create("Visual", a.coin, go.transform, Vector3.zero, Sorting.Item);
                    go.AddComponent<Coin>().Init(ctx, sr.transform);
                    break;
                }
                case SpawnKind.QuestionCoin:
                case SpawnKind.QuestionPowerUp:
                {
                    var block = NewBlock<QuestionBlock>("QuestionBlock", a.blockQuestion, center, parent, ctx);
                    block.Contents = spawn.Kind == SpawnKind.QuestionCoin ? QuestionBlock.Content.Coin : QuestionBlock.Content.PowerUp;
                    break;
                }
                case SpawnKind.Brick:
                    NewBlock<BrickBlock>("Brick", a.blockBrick, center, parent, ctx);
                    break;
                case SpawnKind.Crab:
                {
                    var go = NewObject("Crab", feet, Layers.Enemy, parent);
                    var rb = go.AddComponent<Rigidbody2D>();
                    rb.gravityScale = 4f;
                    rb.freezeRotation = true;
                    rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                    var box = go.AddComponent<BoxCollider2D>();
                    box.size = new Vector2(0.8f, 0.62f);
                    box.offset = new Vector2(0f, 0.31f);
                    box.sharedMaterial = SpriteObjects.Frictionless;
                    var sr = SpriteObjects.Create("Visual", a.crabWalk.Length > 0 ? a.crabWalk[0] : null, go.transform, Vector3.zero, Sorting.Enemy);
                    go.AddComponent<CrabEnemy>().Init(ctx, sr);
                    break;
                }
                case SpawnKind.Puffer:
                {
                    var go = NewObject("Puffer", center, Layers.Enemy, parent);
                    go.AddComponent<Rigidbody2D>();
                    var col = go.AddComponent<CircleCollider2D>();
                    col.radius = 0.4f;
                    col.isTrigger = true;
                    var sr = SpriteObjects.Create("Visual", a.puffer.Length > 0 ? a.puffer[0] : null, go.transform, new Vector3(0f, -0.5f), Sorting.Enemy);
                    go.AddComponent<PufferEnemy>().Init(ctx, sr);
                    break;
                }
                case SpawnKind.Pipe:
                    BuildPipe(a, spawn, parent);
                    break;
                case SpawnKind.Flag:
                    return BuildGoal(ctx, spawn, parent);
                case SpawnKind.Bush:
                case SpawnKind.Flowers:
                case SpawnKind.Reeds:
                case SpawnKind.Sign:
                {
                    var sprite = spawn.Kind switch
                    {
                        SpawnKind.Bush => a.bush,
                        SpawnKind.Flowers => a.flowers,
                        SpawnKind.Reeds => a.reeds,
                        _ => a.sign,
                    };
                    SpriteObjects.Create(spawn.Kind.ToString(), sprite, parent, feet, Sorting.Decor);
                    break;
                }
            }
            return null;
        }

        static GameObject NewObject(string name, Vector3 position, int layer, Transform parent)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go;
        }

        static T NewBlock<T>(string name, Sprite sprite, Vector3 center, Transform parent, LevelContext ctx) where T : BlockBase
        {
            var go = NewObject(name, center, Layers.Ground, parent);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
            box.sharedMaterial = SpriteObjects.Frictionless;
            var sr = SpriteObjects.Create("Visual", sprite, go.transform, Vector3.zero, Sorting.Block);
            var block = go.AddComponent<T>();
            block.Init(ctx, sr);
            return block;
        }

        static void BuildPipe(Game.GameAssets a, Spawn spawn, Transform parent)
        {
            int h = spawn.Size;
            // Top-left cell is spawn.Cell; the pipe spans 2 columns and h rows downward.
            float left = spawn.Cell.x, top = spawn.Cell.y + 1, bottom = top - h;
            var go = NewObject("Pipe", new Vector3(left + 1f, bottom), Layers.Ground, parent);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(2f, h);
            box.offset = new Vector2(0f, h * 0.5f);
            box.sharedMaterial = SpriteObjects.Frictionless;

            if (h > 1 && a.pipeBody != null)
            {
                var body = SpriteObjects.Create("Body", a.pipeBody, go.transform, new Vector3(0f, (h - 1) * 0.5f), Sorting.Pipe);
                body.drawMode = SpriteDrawMode.Tiled;
                body.size = new Vector2(2f, h - 1);
            }
            SpriteObjects.Create("Top", a.pipeTop, go.transform, new Vector3(0f, h - 0.5f), Sorting.Pipe);
        }

        static GoalPole BuildGoal(LevelContext ctx, Spawn spawn, Transform parent)
        {
            var a = ctx.Assets;
            var go = NewObject("GoalPole", new Vector3(spawn.Cell.x + 0.5f, spawn.Cell.y + 1f), Layers.Item, parent);
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            // Generous grab area: the pole plus its base block, so walking into the base also counts.
            trigger.size = new Vector2(1.1f, GoalPole.PoleHeight + 1f);
            trigger.offset = new Vector2(0f, (GoalPole.PoleHeight - 1f) * 0.5f);

            var pole = SpriteObjects.Create("Pole", a.flagPole, go.transform, new Vector3(0f, GoalPole.PoleHeight * 0.5f), Sorting.GoalPole);
            pole.drawMode = SpriteDrawMode.Tiled;
            if (a.flagPole != null) pole.size = new Vector2(a.flagPole.bounds.size.x, GoalPole.PoleHeight);
            SpriteObjects.Create("Ball", a.flagBall, go.transform, new Vector3(0f, GoalPole.PoleHeight + 0.15f), Sorting.GoalPole);
            var flag = SpriteObjects.Create("Flag", a.flag, go.transform, new Vector3(-0.62f, GoalPole.PoleHeight - 0.7f), Sorting.GoalPole - 1);

            var goal = go.AddComponent<GoalPole>();
            goal.Init(ctx, flag.transform);
            return goal;
        }
    }
}
