using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using engenious.UI;
using engenious;
using engenious.Graphics;
using OctoAwesome.UI.Components;

namespace OctoAwesome.UI.Controls
{
    public class CrosshairControl : Control
    {
        public Texture2D Texture;
        public float Transparency;
        public Color Color;

        AssetComponent assets;

        public CrosshairControl(BaseScreenComponent manager, AssetComponent asset) : base(manager)
        {
            assets = asset;

            Transparency = 0.5f;
            Color = Color.White;

            Texture = assets.LoadTexture(GetType(), "octocross");
        }

        protected override void OnDrawContent(SpriteBatch batch, Rectangle contentArea, GameTime gameTime, float alpha)
        {
            if (!assets.Ready)
                return;

            batch.Draw(Texture, contentArea, Color * Transparency);
        }
    }
}
