﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace OctoAwesome
{
    public partial class RenderControl : UserControl
    {
        private int SPRITE_WIDTH = 57;
        private int SPRITE_HEIGHT = 64;

        private Stopwatch watch = new Stopwatch();

        public Game Game { get; set; }

        private Image grass;
        private Image sprite;

        public RenderControl()
        {
            InitializeComponent();

            grass = Image.FromFile("Assets/grass.png");
            sprite = Image.FromFile("Assets/sprite.png");

            watch.Start();
        }

        protected override void OnResize(EventArgs e)
        {
            if (Game != null)
            {
                Game.PlaygroundSize = new Point(ClientSize.Width, ClientSize.Height);
            }
            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.CornflowerBlue);

            for (int x = 0; x < ClientRectangle.Width; x += grass.Width)
            {
                for (int y = 0; y < ClientRectangle.Height; y += grass.Height)
                {
                    e.Graphics.DrawImage(grass, new Point(x, y));
                }
            }

            if (Game == null)
                return;

            using (Brush brush = new SolidBrush(Color.White))
            {
                int frame = (int)((watch.ElapsedMilliseconds / 250) % 8);

                e.Graphics.DrawImage(sprite,
                    new Rectangle(Game.Position.X, Game.Position.Y, SPRITE_WIDTH, SPRITE_HEIGHT), 
                    new Rectangle(frame * SPRITE_WIDTH, 0, SPRITE_WIDTH, SPRITE_HEIGHT), 
                    GraphicsUnit.Pixel);

                // e.Graphics.FillEllipse(brush, new Rectangle(Game.Position.X, Game.Position.Y, 100, 100));
            }
        }
    }
}
