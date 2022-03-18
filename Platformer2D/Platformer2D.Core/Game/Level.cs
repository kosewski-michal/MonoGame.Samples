#region File Description

//-----------------------------------------------------------------------------
// Level.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

#endregion File Description

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using TiledSharp;

namespace Platformer2D
{
    /// <summary>
    /// A uniform grid of tiles with collections of gems and enemies.
    /// The level owns the player and controls the game's win and lose
    /// conditions as well as scoring.
    /// </summary>
    internal class Level : IDisposable
    {
        //Moje dopiski
        public List<Texture2D> listTextureGid = new List<Texture2D>();

        // The layer which entities are drawn on top of.
        private const int EntityLayer = 2;

        private const int PointsPerSecond = 5;

        private static readonly Point InvalidPosition = new Point(-1, -1);

        private ContentManager content;

        private List<Enemy> enemies = new List<Enemy>();

        private Point exit = InvalidPosition;

        private SoundEffect exitReachedSound;

        private List<Gem> gems = new List<Gem>();

        private List<BulletFromGem> bullets = new List<BulletFromGem>();


        private Texture2D[] layers;

        private Texture2D layerBackground;


        //tiled
        private TmxMap map;

        private Player player;

        // Level game state.
        private Random random = new Random(354668);

        private bool reachedExit;

        private int score;

        // Key locations in the level.
        private Vector2 start;

        private int tileHeight;

        // Physical structure of the level.
        private Tile[,] tiles;
        // Arbitrary, but constant seed
        private Texture2D tileset;

        private int tilesetTilesHigh;

        private int tilesetTilesWide;

        private int tileWidth;

        private TimeSpan timeRemaining;

        // Level content.
        public ContentManager Content
        {
            get { return content; }
        }

        // Entities in the level.
        public Player Player
        {
            get { return player; }
        }
        public bool ReachedExit
        {
            get { return reachedExit; }
        }

        public int Score
        {
            get { return score; }
        }

        //koniec
        public TimeSpan TimeRemaining
        {
            get { return timeRemaining; }
        }

        public void LoadMapTiles(GraphicsDevice graphics)
        {
            //map = new TmxMap("Content/Maps/world_1_level_0.tmx");
            //tileset = Content.Load<Texture2D>("maps/SMB-Tiles");

            map = new TmxMap("Content/Maps/SMBV2level1.tmx");
            tileset = Content.Load<Texture2D>("maps/SMBV2");

            //tileset = Content.Load<Texture2D>(map.Tilesets[0].Name.ToString());

            tileWidth = map.Tilesets[0].TileWidth;
            tileHeight = map.Tilesets[0].TileHeight;

            tilesetTilesWide = tileset.Width / tileWidth;
            tilesetTilesHigh = tileset.Height / tileHeight;

            for (int y = 0; y < tilesetTilesHigh; y++)
            {
                for (int x = 0; x < tilesetTilesWide; x++)
                {
                    Rectangle tilesetRec = new Rectangle((tileWidth) * x, (tileHeight) * y, tileWidth, tileHeight);

                    Texture2D texture2D = tileset.CreateTexture(graphics, tilesetRec);
                    listTextureGid.Add(texture2D);
                }
            }

            ////

            ////

            int width = map.Width;
            int linesY = map.Height;

            // Allocate the tile grid.
            tiles = new Tile[width, linesY];

            //mój kod
            int i = 0;

            for (int l = 0; l < map.Layers.Count; l++)
            {
                //int l = 2;
                string layerName = map.Layers[l].Name;
                for (i = 0; i < map.Layers[l].Tiles.Count; i++)
                {
                    int gid = map.Layers[l].Tiles[i].Gid;

                    if (gid == 0)
                    {
                    }
                    else
                    {
                        int x = i % map.Width;
                        int y = (int)Math.Floor(i / (double)map.Width);

                        //int tileFrame = gid - 1;/*- 1;*/
                        //int column = tileFrame % tilesetTilesWide;
                        //int row = (int)Math.Floor((double)tileFrame / (double)tilesetTilesWide);

                        tiles[x, y] = LoadTileByGid(gid, x, y);
                    }
                }
            }

            // Loop over every tile position,
            //for (int y = 0; y < Height; ++y)
            //{
            //    for (int x = 0; x < Width; ++x)
            //    {
            //        // to load each tile.
            //        int tileTypeChar = linesY[y][x];
            //        tiles[x, y] = LoadTile(tileTypeChar, x, y);
            //    }
            //}

            // Verify that the level has a beginning and an end.
            if (Player == null)
                throw new NotSupportedException("A level must have a starting point.");
            if (exit == InvalidPosition)
                throw new NotSupportedException("A level must have an exit.");
        }
        #region Loading

        /// <summary>
        /// Constructs a new level.
        /// </summary>
        /// <param name="serviceProvider">
        /// The service provider that will be used to construct a ContentManager.
        /// </param>
        /// <param name="fileStream">
        /// A stream containing the tile data.
        /// </param>
        public Level(IServiceProvider serviceProvider, Stream fileStream, int levelIndex, GraphicsDevice graphicsDevice)
        {
            // Create a new content manager to load content used just by this level.
            content = new ContentManager(serviceProvider, "Content");
            LoadMapTiles(graphicsDevice);

            timeRemaining = TimeSpan.FromMinutes(2.0);

            //LoadTiles(fileStream);

            // Load background layer textures. For now, all levels must
            // use the same backgrounds and only use the left-most part of them.
            layers = new Texture2D[3];
            for (int i = 0; i < layers.Length; ++i)
            {
                // Choose a random segment if each background layer for level variety.
                int segmentIndex = levelIndex;
                layers[i] = Content.Load<Texture2D>("Backgrounds/Layer" + i + "_" + segmentIndex);
            }

            layerBackground = Content.Load<Texture2D>("Backgrounds/Layer0_0");



            // Load sounds.
            exitReachedSound = Content.Load<SoundEffect>("Sounds/ExitReached");
        }

        /// <summary>
        /// Unloads the level content.
        /// </summary>
        public void Dispose()
        {
            Content.Unload();
        }

        /// <summary>
        /// Instantiates an enemy and puts him in the level.
        /// </summary>
        private Tile LoadEnemyTile(int x, int y, string spriteSet)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            enemies.Add(new Enemy(this, position, spriteSet));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Remembers the location of the level's exit.
        /// </summary>
        private Tile LoadExitTile(int x, int y)
        {
            if (exit != InvalidPosition)
                throw new NotSupportedException("A level may only have one exit.");

            exit = GetBounds(x, y).Center;

            return LoadTile("Exit", TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates a gem and puts it in the level.
        /// </summary>
        private Tile LoadGemTile(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            gems.Add(new Gem(this, new Vector2(position.X, position.Y)));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates a player, puts him in the level, and remembers where to put him when he is resurrected.
        /// </summary>
        private Tile LoadStartTile(int x, int y)
        {
            if (Player != null)
                throw new NotSupportedException("A level may only have one starting point.");

            start = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            player = new Player(this, start);

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Loads an individual tile's appearance and behavior.
        /// </summary>
        /// <param name="tileType">
        /// The character loaded from the structure file which
        /// indicates what should be loaded.
        /// </param>
        /// <param name="x">
        /// The X location of this tile in tile space.
        /// </param>
        /// <param name="y">
        /// The Y location of this tile in tile space.
        /// </param>
        /// <returns>The loaded tile.</returns>
        private Tile LoadTile(char tileType, int x, int y)
        {
            switch (tileType)
            {
                // Blank space
                case '.':
                    return new Tile(null, TileCollision.Passable);

                // Exit
                case 'X':
                    return LoadExitTile(x, y);

                // Gem
                case 'G':
                    return LoadGemTile(x, y);

                // Floating platform
                case '-':
                    return LoadTile("Platform", TileCollision.Platform);

                // Various enemies
                case 'A':
                    return LoadEnemyTile(x, y, "MonsterA");

                case 'B':
                    return LoadEnemyTile(x, y, "MonsterB");

                case 'C':
                    return LoadEnemyTile(x, y, "MonsterC");

                case 'D':
                    return LoadEnemyTile(x, y, "MonsterD");

                // Platform block
                case '~':
                    return LoadVarietyTile("BlockB", 2, TileCollision.Platform);

                // Passable block
                case ':':
                    return LoadVarietyTile("BlockB", 2, TileCollision.Passable);

                // Player 1 start point
                case '1':
                    return LoadStartTile(x, y);

                // Impassable block
                case '#':
                    return LoadVarietyTile("BlockA", 7, TileCollision.Solid);

                // Unknown tile type character
                default:
                    throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position {1}, {2}.", tileType, x, y));
            }
        }

        /// <summary>
        /// Creates a new tile. The other tile loading methods typically chain to this
        /// method after performing their special logic.
        /// </summary>
        /// <param name="name">
        /// Path to a tile texture relative to the Content/Tiles directory.
        /// </param>
        /// <param name="collision">
        /// The tile collision type for the new tile.
        /// </param>
        /// <returns>The new tile.</returns>
        private Tile LoadTile(string name, TileCollision collision)
        {
            int index = random.Next(30);
            if (listTextureGid.Count > index)
            {
                return new Tile(listTextureGid[index], collision);
            }
            else
            {
                return new Tile(Content.Load<Texture2D>("Tiles/" + name), collision);
            }
        }

        private Tile LoadTileByGid(int gid, int x, int y)
        {
            // Blank space

            //if ( (gid > 0 && gid < 11) || (gid>22 && gid<40))
            //        return LoadTileGid(gid, TileCollision.Passable);

            switch (gid)
            {
                //// Blank space
                //case '.':
                //    return new Tile(null, TileCollision.Passable);

                // Exit
                case 42:
                    return LoadExitTile(x, y);

                // Gem
                case 11:
                    return LoadGemTile(x, y);

                case 12:
                    return LoadGemTile(x, y);
                //case 14:
                //    return LoadGemTile(x, y);

                // Floating platform
                case 16:
                    return LoadTileGid(gid, TileCollision.Platform);// LoadTile("Platform", TileCollision.Platform);

                // Various enemies
                case 43:
                    return LoadEnemyTile(x, y, "MonsterA");
                //case 'B':
                //    return LoadEnemyTile(x, y, "MonsterB");
                //case 'C':
                //    return LoadEnemyTile(x, y, "MonsterC");
                //case 'D':
                //    return LoadEnemyTile(x, y, "MonsterD");

                // Platform block
                //case '~':
                //    return LoadVarietyTile("BlockB", 2, TileCollision.Platform);

                // Passable block
                //case ':':
                //    return LoadVarietyTile("BlockB", 2, TileCollision.Passable);

                // Player 1 start point
                case 41:
                    return LoadStartTile(x, y);

                // Impassable block
                case 40:
                    return LoadTileGid(gid, TileCollision.Solid);
                // LoadVarietyTile("BlockA", 7, TileCollision.Solid);

                case 21:
                    return LoadTileGid(gid, TileCollision.Solid);

                case 22:
                    return LoadTileGid(gid, TileCollision.Solid);

                case 27:
                    return LoadTileGid(gid, TileCollision.Solid);

                case 28:
                    return LoadTileGid(gid, TileCollision.Solid);

                case 14:
                    return LoadTileGid(gid, TileCollision.Solid);
                case 15:
                    return LoadTileGid(gid, TileCollision.Solid);

                default:
                    return new Tile(null, TileCollision.Passable);

                    //return LoadTileGid(gid, TileCollision.Passable);
            }
        }


        private Tile LoadTileByGidFromContra(int gid, int x, int y)
        {
            // Blank space

            //if ( (gid > 0 && gid < 11) || (gid>22 && gid<40))
            //        return LoadTileGid(gid, TileCollision.Passable);

            switch (gid)
            {
                //// Blank space
                //case '.':
                //    return new Tile(null, TileCollision.Passable);

                // Exit
                case 42:
                    return LoadExitTile(x, y);

                // Gem
                case 11:
                    return LoadGemTile(x, y);

                case 12:
                    return LoadGemTile(x, y);
                //case 14:
                //    return LoadGemTile(x, y);

                // Floating platform
                case 16:
                    return LoadTileGid(gid, TileCollision.Platform);// LoadTile("Platform", TileCollision.Platform);

                // Various enemies
                case 43:
                    return LoadEnemyTile(x, y, "MonsterA");
                //case 'B':
                //    return LoadEnemyTile(x, y, "MonsterB");
                //case 'C':
                //    return LoadEnemyTile(x, y, "MonsterC");
                //case 'D':
                //    return LoadEnemyTile(x, y, "MonsterD");

                // Platform block
                //case '~':
                //    return LoadVarietyTile("BlockB", 2, TileCollision.Platform);

                // Passable block
                //case ':':
                //    return LoadVarietyTile("BlockB", 2, TileCollision.Passable);

                // Player 1 start point
                case 41:
                    return LoadStartTile(x, y);

                // Impassable block
                case 40:
                    return LoadTileGid(gid, TileCollision.Solid);
                // LoadVarietyTile("BlockA", 7, TileCollision.Solid);

                case 21:
                    return LoadTileGid(gid, TileCollision.Solid);

                case 22:
                    return LoadTileGid(gid, TileCollision.Solid);

                case 27:
                    return LoadTileGid(gid, TileCollision.Solid);

                case 28:
                    return LoadTileGid(gid, TileCollision.Solid);

                case 14:
                    return LoadTileGid(gid, TileCollision.Solid);
                case 15:
                    return LoadTileGid(gid, TileCollision.Solid);

                default:
                    return new Tile(null, TileCollision.Passable);

                    //return LoadTileGid(gid, TileCollision.Passable);
            }
        }

        private Tile LoadTileGid(int gid, TileCollision collision)
        {
            return new Tile(listTextureGid[gid - 1], collision);
        }

        /// <summary>
        /// Iterates over every tile in the structure file and loads its
        /// appearance and behavior. This method also validates that the
        /// file is well-formed with a player start point, exit, etc.
        /// </summary>
        /// <param name="fileStream">
        /// A stream containing the tile data.
        /// </param>
        private void LoadTiles(Stream fileStream)
        {
            // Load the level and ensure all of the lines are the same length.
            int width;
            List<string> linesY = new List<string>();
            using (StreamReader reader = new StreamReader(fileStream))
            {
                string line = reader.ReadLine();
                width = line.Length;
                while (line != null)
                {
                    linesY.Add(line);
                    if (line.Length != width)
                        throw new Exception(String.Format("The length of line {0} is different from all preceeding lines.", linesY.Count));
                    line = reader.ReadLine();
                }
            }

            // Allocate the tile grid.
            tiles = new Tile[width, linesY.Count];

            // Loop over every tile position,
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    // to load each tile.
                    char tileTypeChar = linesY[y][x];

                    // usuniete ładowanie
                    //  tiles[x, y] = LoadTile(tileTypeChar, x, y);
                }
            }

            // Verify that the level has a beginning and an end.
            if (Player == null)
                throw new NotSupportedException("A level must have a starting point.");
            if (exit == InvalidPosition)
                throw new NotSupportedException("A level must have an exit.");
        }
        /// <summary>
        /// Loads a tile with a random appearance.
        /// </summary>
        /// <param name="baseName">
        /// The content name prefix for this group of tile variations. Tile groups are
        /// name LikeThis0.png and LikeThis1.png and LikeThis2.png.
        /// </param>
        /// <param name="variationCount">
        /// The number of variations in this group.
        /// </param>
        private Tile LoadVarietyTile(string baseName, int variationCount, TileCollision collision)
        {
            int index = random.Next(variationCount);
            return LoadTile(baseName + index, collision);
        }
        #endregion Loading

        #region Bounds and collision

        /// <summary>
        /// Height of the level measured in tiles.
        /// </summary>
        public int Height
        {
            get { return tiles.GetLength(1); }
        }

        /// <summary>
        /// Width of level measured in tiles.
        /// </summary>
        public int Width
        {
            get { return tiles.GetLength(0); }
        }

        /// <summary>
        /// Gets the bounding rectangle of a tile in world space.
        /// </summary>
        public Rectangle GetBounds(int x, int y)
        {
            return new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
        }

        /// <summary>
        /// Gets the collision mode of the tile at a particular location.
        /// This method handles tiles outside of the levels boundries by making it
        /// impossible to escape past the left or right edges, but allowing things
        /// to jump beyond the top of the level and fall off the bottom.
        /// </summary>
        public TileCollision GetCollision(int x, int y)
        {
            // Prevent escaping past the level ends.
            if (x < 0 || x >= Width)
                return TileCollision.Solid;
            // Allow jumping past the level top and falling through the bottom.
            if (y < 0 || y >= Height)
                return TileCollision.Passable;

            return tiles[x, y].Collision;
        }
        #endregion Bounds and collision

        #region Update

        /// <summary>
        /// Restores the player to the starting point to try the level again.
        /// </summary>
        public void StartNewLife()
        {
            Player.Reset(start);
        }

        /// <summary>
        /// Updates all objects in the world, performs collision between them,
        /// and handles the time limit with scoring.
        /// </summary>
        public void Update(
            GameTime gameTime,
            KeyboardState keyboardState,
            GamePadState gamePadState,
            AccelerometerState accelState,
            DisplayOrientation orientation)
        {
            // Pause while the player is dead or time is expired.
            if (!Player.IsAlive || TimeRemaining == TimeSpan.Zero)
            {
                // Still want to perform physics on the player.
                Player.ApplyPhysics(gameTime);
            }
            else if (ReachedExit)
            {
                // Animate the time being converted into points.
                int seconds = (int)Math.Round(gameTime.ElapsedGameTime.TotalSeconds * 100.0f);
                seconds = Math.Min(seconds, (int)Math.Ceiling(TimeRemaining.TotalSeconds));
                timeRemaining -= TimeSpan.FromSeconds(seconds);
                score += seconds * PointsPerSecond;
            }
            else
            {
                timeRemaining -= gameTime.ElapsedGameTime;
                Player.Update(gameTime, keyboardState, gamePadState, accelState, orientation);
                UpdateGems(gameTime);

                UpdateBullets(gameTime);


                // Falling off the bottom of the level kills the player.
                if (Player.BoundingRectangle.Top >= Height * Tile.Height)
                    OnPlayerKilled(null);

                UpdateEnemies(gameTime);

                if (Player.isFire)
                {
                    TimeSpan totalGameTime = gameTime.TotalGameTime;

                    double timeBetween = totalGameTime.TotalMilliseconds - player.lastFireTime.TotalMilliseconds;

                    if (timeBetween > 200)
                    {
                        int x = (int)Math.Floor(player.Position.X / Tile.Width);
                        int y = (int)Math.Floor(player.Position.Y / Tile.Height);



                        for (int i = 0; i < 1; i++)
                        {
                            for (int j = 0; j < 1; j++)
                            {
                                Point position = GetBounds(x + 0 + j, y - 2 - i).Center;
                                var bullet = new BulletFromGem(this, new Vector2(position.X, position.Y));
                                bullet.gemType = "special";
                                if (player.Velocity.X >= 0)
                                {
                                    bullet.direction = new Vector2(1, 0);

                                }
                                else
                                {
                                    bullet.direction = new Vector2(-1, 0);

                                }


                                bullets.Add(bullet);
                            }

                        }
                        player.lastFireTime = totalGameTime;
                    }

                    Player.isFire = false;


                    //return new Tile(null, TileCollision.Passable);
                    //gems.Add();

                }


                // The player has reached the exit if they are standing on the ground and
                // his bounding rectangle contains the center of the exit tile. They can only
                // exit when they have collected all of the gems.
                if (Player.IsAlive &&
                    Player.IsOnGround &&
                    Player.BoundingRectangle.Contains(exit))
                {
                    OnExitReached();
                }
            }

            // Clamp the time remaining at zero.
            if (timeRemaining < TimeSpan.Zero)
                timeRemaining = TimeSpan.Zero;
        }

        private void UpdateBullets(GameTime gameTime)
        {

            //for (int i = 0; i < gems.Count; ++i)
            //{
            //    Gem gem = gems[i];

            //    gem.Update(gameTime);

            //    if (gem.BoundingCircle.Intersects(Player.BoundingRectangle))
            //    {
            //        gems.RemoveAt(i--);
            //        OnGemCollected(gem, Player);
            //    }
            //}

            for (int i = 0; i < bullets.Count; ++i)
            {
                BulletFromGem bullet = bullets[i];
                bullet.Update(gameTime);

                Rectangle bounds = bullet.BoundingRectangle;
                int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
                int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
                int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
                int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;


                for (int y = topTile; y <= bottomTile; ++y)
                {
                    for (int x = leftTile; x <= rightTile; ++x)
                    {
                        // If this tile is collidable,
                        TileCollision collision = GetCollision(x, y);
                        if (collision != TileCollision.Passable)
                        {
                            // Determine collision depth (with direction) and magnitude.
                            Rectangle tileBounds = GetBounds(x, y);
                            Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);

                            if (bullet.BoundingCircle.Intersects(tileBounds))
                            {
                                bullets.RemoveAt(i);
                            }


                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when the player reaches the level's exit.
        /// </summary>
        private void OnExitReached()
        {
            Player.OnReachedExit();
            exitReachedSound.Play();
            reachedExit = true;
        }

        /// <summary>
        /// Called when a gem is collected.
        /// </summary>
        /// <param name="gem">The gem that was collected.</param>
        /// <param name="collectedBy">The player who collected this gem.</param>
        private void OnGemCollected(Gem gem, Player collectedBy)
        {
            score += gem.PointValue;

            gem.OnCollected(collectedBy);
        }

        /// <summary>
        /// Called when the player is killed.
        /// </summary>
        /// <param name="killedBy">
        /// The enemy who killed the player. This is null if the player was not killed by an
        /// enemy, such as when a player falls into a hole.
        /// </param>
        private void OnPlayerKilled(Enemy killedBy)
        {
            Player.OnKilled(killedBy);
        }

        /// <summary>
        /// Animates each enemy and allow them to kill the player.
        /// </summary>
        private void UpdateEnemies(GameTime gameTime)
        {
            int i = 0;
            foreach (Enemy enemy in enemies)
            {
                enemy.Update(gameTime);


                if (enemy.isAlive)
                {


                    // Touching an enemy instantly kills the player
                    if (enemy.BoundingRectangle.Intersects(Player.BoundingRectangle))
                    {
                        var vectorDepth = enemy.BoundingRectangle.GetIntersectionDepth(Player.BoundingRectangle);

                        if (vectorDepth.Y < 10)
                        {
                            //kill enemy
                            enemy.isAlive = false;
                            Player.velocity.Y -= 5000;
                        }
                        //enemy.isAlive = false;
                        //  OnPlayerKilled(enemy);
                    }


                    for (int j = 0; j < bullets.Count; j++)
                    {
                        if (bullets[j].BoundingCircle.Intersects(enemy.BoundingRectangle))
                        {
                            enemy.isAlive = false;
                            bullets.RemoveAt(j--);
                        }
                    }



                }
                i++;

            }
        }

        /// <summary>
        /// Animates each gem and checks to allows the player to collect them.
        /// </summary>
        private void UpdateGems(GameTime gameTime)
        {
            for (int i = 0; i < gems.Count; ++i)
            {
                Gem gem = gems[i];

                gem.Update(gameTime);

                if (gem.BoundingCircle.Intersects(Player.BoundingRectangle))
                {
                    gems.RemoveAt(i--);
                    OnGemCollected(gem, Player);
                }
            }


            //test

        }
        #endregion Update

        #region Draw

        /// <summary>
        /// Draw everything in the level from background to foreground.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Vector2 scale = new Vector2(2, 2);

            //spriteBatch.Draw(layerBackground, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            for (int x = -1; x < 10; ++x)
            {
                //Vector2 position = new Vector2(x, 0) * Tile.Size;
                Vector2 position = new Vector2(x, 0) * layerBackground.Width*2;
                spriteBatch.Draw(layers[Math.Abs( x%layers.Length)], position, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }


            //for (int i = 0; i <= EntityLayer; ++i)
            //    spriteBatch.Draw(layers[i], Vector2.Zero, Color.White);

            DrawTiles(spriteBatch);

            foreach (Gem gem in gems)
                gem.Draw(gameTime, spriteBatch);

            foreach (BulletFromGem bullet in bullets)
                bullet.Draw(gameTime, spriteBatch);

            Player.Draw(gameTime, spriteBatch);

            foreach (Enemy enemy in enemies)
                enemy.Draw(gameTime, spriteBatch);

            //for (int i = EntityLayer + 1; i < layers.Length; ++i)
            //    spriteBatch.Draw(layers[i], Vector2.Zero, Color.White);



            //spriteBatch.Draw(layerContra, Vector2.Zero, Color.White)
        }

        /// <summary>
        /// Draws each tile in the level.
        /// </summary>
        private void DrawTiles(SpriteBatch spriteBatch)
        {
            // For each tile position
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    // If there is a visible tile in that position
                    Texture2D texture = tiles[x, y].Texture;
                    if (texture != null)
                    {
                        // Draw it in screen space.
                        Vector2 position = new Vector2(x, y) * Tile.Size;
                        //spriteBatch.Draw(texture, position, Color.White);

                        Vector2 scale = new Vector2((float)Tile.Width / texture.Width, (float)Tile.Height / texture.Height);

                        spriteBatch.Draw(texture, position, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    }
                }
            }
        }

        #endregion Draw
    }
}