using engenious.UI;
using OctoAwesome.Client.Screens;
using System;
using engenious;
using OctoAwesome.UI.Components;

namespace OctoAwesome.Client.Components
{
    internal sealed class ScreenComponent : BaseScreenComponent, IAssetRelatedComponent
    {
        public new OctoGame Game { get; private set; }

        public PlayerComponent Player { get { return Game.Player; } }

        public CameraComponent Camera => Game.Camera;

        public ScreenComponent(OctoGame game) : base(game)
        {
            Game = game;
            TitlePrefix = "OctoAwesome";
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            ReloadAssets();

            Frame.Background = new BorderBrush(Color.CornflowerBlue);

            NavigateFromTransition = new AlphaTransition(Frame, Transition.Linear, TimeSpan.FromMilliseconds(200), 0f);
            NavigateToTransition = new AlphaTransition(Frame, Transition.Linear, TimeSpan.FromMilliseconds(200), 1f);

            NavigateToScreen(new MainScreen(this));

        }

        public void Exit()
        {
            Game.Exit();
        }

        public void UnloadAssets()
        {
            Skin.Current.ButtonBrush = null;
            Skin.Current.ButtonHoverBrush = null;
            Skin.Current.ButtonPressedBrush = null;
            Skin.Current.ProgressBarBrush = null;
            Skin.Current.HorizontalScrollBackgroundBrush = null;
        }

        public void ReloadAssets()
        {
            Skin.Current.ButtonBrush = NineTileBrush.FromSingleTexture(Game.Assets.LoadTexture("buttonLong_brown"), 15, 15);
            Skin.Current.ButtonHoverBrush = NineTileBrush.FromSingleTexture(Game.Assets.LoadTexture("buttonLong_beige"), 15, 15);
            Skin.Current.ButtonPressedBrush = NineTileBrush.FromSingleTexture(Game.Assets.LoadTexture("buttonLong_beige_pressed"), 15, 15);
            Skin.Current.ButtonDisabledBrush = NineTileBrush.FromSingleTexture(Game.Assets.LoadTexture("buttonLong_brown_disabled"), 15, 15);
            Skin.Current.ProgressBarBrush = NineTileBrush.FromSingleTexture(Game.Assets.LoadTexture("progress_red"), 10, 8);
            Skin.Current.HorizontalScrollBackgroundBrush = NineTileBrush.FromSingleTexture(Game.Assets.LoadTexture("progress_background"), 10, 8);
        }
    }
}
