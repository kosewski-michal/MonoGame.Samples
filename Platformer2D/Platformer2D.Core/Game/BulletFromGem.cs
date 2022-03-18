﻿#region File Description
//-----------------------------------------------------------------------------
// Gem.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace Platformer2D
{
    /// <summary>
    /// A valuable item the player can collect.
    /// </summary>
    class BulletFromGem
    {
        private Texture2D texture;
        private Vector2 origin;
        public Vector2 direction;


        private SoundEffect collectedSound;

        public readonly int PointValue = 30;
        public readonly Color Color = Color.White;

        // The gem is animated from a base position along the Y axis.
        private Vector2 basePosition;
        private float bounce;

        public string gemType;


        public Level Level
        {
            get { return level; }
        }
        Level level;

        /// <summary>
        /// Gets the current position of this gem in world space.
        /// </summary>
        public Vector2 Position
        {
            get
            {
                return basePosition + new Vector2(0.0f, bounce);
            }
        }

        /// <summary>
        /// Gets a circle which bounds this gem in world space.
        /// </summary>
        public Circle BoundingCircle
        {
            get
            {
                
                                    //return new Circle(Position, Tile.Width / 3.0f);

                return new Circle(basePosition, Tile.Width / 3.0f);
            }
        }


        public Rectangle BoundingRectangle
        {
            get
            {

                //int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
                //int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;
                
                                    return new Rectangle((int)basePosition.X, (int)basePosition.Y, Tile.Width, Tile.Width);

                //return new Rectangle((int)Position.X, (int)Position.Y, Tile.Width, Tile.Width);
                //return new Circle(Position, Tile.Width / 3.0f);
            }
        }

        /// <summary>
        /// Constructs a new gem.
        /// </summary>
        public BulletFromGem(Level level, Vector2 position)
        {
            this.level = level;
            this.basePosition = position;

            LoadContent();
        }

        /// <summary>
        /// Loads the gem texture and collected sound.
        /// </summary>
        public void LoadContent()
        {
            texture = Level.Content.Load<Texture2D>("Sprites/Bullet");
            origin = new Vector2(texture.Width / 2.0f, texture.Height / 2.0f);
            collectedSound = Level.Content.Load<SoundEffect>("Sounds/GemCollected");
        }

        /// <summary>
        /// Bounces up and down in the air to entice players to collect them.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // Bounce control constants
            const float BounceHeight = 0;//0.18f;
            const float BounceRate = 3.0f;
            const float BounceSync = -0.75f;

            // Bounce along a sine curve over time.
            // Include the X coordinate so that neighboring gems bounce in a nice wave pattern.            
            double t = gameTime.TotalGameTime.TotalSeconds * BounceRate + Position.X * BounceSync;
            bounce = (float)Math.Sin(t) * BounceHeight * texture.Height;

            if (gemType=="special")
            {
                basePosition = basePosition + 5*direction;
                //basePosition.X+=5;
            }
          

        }

        /// <summary>
        /// Called when this gem has been collected by a player and removed from the level.
        /// </summary>
        /// <param name="collectedBy">
        /// The player who collected this gem. Although currently not used, this parameter would be
        /// useful for creating special power-up gems. For example, a gem could make the player invincible.
        /// </param>
        public void OnCollected(Player collectedBy)
        {
            try
            {
                collectedSound.Play();
            }
            catch { }
        }

        /// <summary>
        /// Draws a gem in the appropriate color.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, Position, null, Color, 0.0f, origin, 1.0f, SpriteEffects.None, 0.0f);
        }
    }
}