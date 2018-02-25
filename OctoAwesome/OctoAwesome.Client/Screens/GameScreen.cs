﻿using MonoGameUi;
using OctoAwesome.Client.Controls;
using OctoAwesome.Client.Components;
using System;
using engenious;
using engenious.Input;
using engenious.Helper;

namespace OctoAwesome.Client.Screens
{
    internal sealed class GameScreen : Screen
    {
        private const float mouseSpeed = 0.2f;

        private new ScreenComponent Manager { get; set; }

        DebugControl debug;
        SceneControl scene;
        CompassControl compass;
        ToolbarControl toolbar;
        MinimapControl minimap;
        CrosshairControl crosshair;
        HealthBarControl healthbar;

        public GameScreen(ScreenComponent manager) : base(manager)
        {
            DefaultMouseMode = MouseMode.Captured;

            Manager = manager;
            Padding = Border.All(0);

            scene = new SceneControl(manager);
            scene.HorizontalAlignment = HorizontalAlignment.Stretch;
            scene.VerticalAlignment = VerticalAlignment.Stretch;
            Controls.Add(scene);

            debug = new DebugControl(manager);
            debug.HorizontalAlignment = HorizontalAlignment.Stretch;
            debug.VerticalAlignment = VerticalAlignment.Stretch;
            debug.Visible = false;
            Controls.Add(debug);

            compass = new CompassControl(manager);
            compass.HorizontalAlignment = HorizontalAlignment.Center;
            compass.VerticalAlignment = VerticalAlignment.Top;
            compass.Margin = Border.All(10);
            compass.Width = 300;
            compass.Height = 50;
            Controls.Add(compass);

            //TODO: dynamische einbindugn aus extensions
            toolbar = new ToolbarControl(manager);
            toolbar.HorizontalAlignment = HorizontalAlignment.Stretch;
            toolbar.VerticalAlignment = VerticalAlignment.Bottom;
            toolbar.Height = 100;
            Controls.Add(toolbar);

            minimap = new MinimapControl(manager, scene);
            minimap.HorizontalAlignment = HorizontalAlignment.Right;
            minimap.VerticalAlignment = VerticalAlignment.Bottom;
            minimap.Width = 128;
            minimap.Height = 128;
            minimap.Margin = Border.All(5);
            Controls.Add(minimap);

            healthbar = new HealthBarControl(manager);
            healthbar.HorizontalAlignment = HorizontalAlignment.Left;
            healthbar.VerticalAlignment = VerticalAlignment.Bottom;
            healthbar.Width = 240;
            healthbar.Height = 78;
            healthbar.Maximum = 100;
            healthbar.Value = 40;
            healthbar.Margin = Border.All(20, 30);
            Controls.Add(healthbar);

            crosshair = new CrosshairControl(manager);
            crosshair.HorizontalAlignment = HorizontalAlignment.Center;
            crosshair.VerticalAlignment = VerticalAlignment.Center;
            crosshair.Width = 8;
            crosshair.Height = 8;
            Controls.Add(crosshair);

            Title = Languages.OctoClient.Game;

            RegisterKeyActions();
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            Vector2 move = Vector2.Zero;
            if (pressedMoveUp) move += new Vector2(0f, 1f);
            if (pressedMoveLeft) move += new Vector2(-1f, 0f);
            if (pressedMoveDown) move += new Vector2(0f, -1f);
            if (pressedMoveRight) move += new Vector2(1f, 0f);
            Manager.Player.MoveValue = move;

            Vector2 head = Vector2.Zero;
            if (pressedHeadUp) head += new Vector2(0f, 1f);
            if (pressedHeadDown) head += new Vector2(0f, -1f);
            if (pressedHeadLeft) head += new Vector2(-1f, 0f);
            if (pressedHeadRight) head += new Vector2(1f, 0f);
            Manager.Player.HeadValue = head;
            
            HandleGamePad();

            base.OnUpdate(gameTime);
        }

        #region Mouse Input

        protected override void OnLeftMouseDown(MouseEventArgs args)
        {
            if (!IsActiveScreen) return;

            Manager.Player.ApplyInput.Set(true);
            args.Handled = true;
        }

        protected override void OnRightMouseDown(MouseEventArgs args)
        {
            if (!IsActiveScreen) return;

            Manager.Player.InteractInput.Set(true);
            args.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs args)
        {
            if (!IsActiveScreen) return;

            if (args.MouseMode == MouseMode.Captured && IsActiveScreen)
            {
                Manager.Player.HeadValue = args.GlobalPosition.ToVector2() * mouseSpeed * new Vector2(1f, -1f);
                args.Handled = true;
            }
        }

        protected override void OnMouseScroll(MouseScrollEventArgs args)
        {
            if (!IsActiveScreen) return;

            Manager.Player.SlotLeftInput = args.Steps > 0;
            Manager.Player.SlotRightInput = args.Steps < 0;
            args.Handled = true;
        }

        #endregion

        #region Keyboard Input

        private bool pressedMoveUp = false;
        private bool pressedMoveLeft = false;
        private bool pressedMoveDown = false;
        private bool pressedMoveRight = false;
        private bool pressedHeadUp = false;
        private bool pressedHeadDown = false;
        private bool pressedHeadLeft = false;
        private bool pressedHeadRight = false;

        private void RegisterKeyActions()
        {
            Manager.Game.KeyMapper.AddAction("octoawesome:forward", type =>
            {
                if (!IsActiveScreen) return;
                if (type == KeyMapper.KeyType.Down) pressedMoveUp = true;
                else if (type == KeyMapper.KeyType.Up) pressedMoveUp = false;
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:left", type =>
            {
                if (!IsActiveScreen) return;
                if (type == KeyMapper.KeyType.Down) pressedMoveLeft = true;
                else if (type == KeyMapper.KeyType.Up) pressedMoveLeft = false;
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:backward", type =>
            {
                if (!IsActiveScreen) return;
                if (type == KeyMapper.KeyType.Down) pressedMoveDown = true;
                else if (type == KeyMapper.KeyType.Up) pressedMoveDown = false;
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:right", type =>
            {
                if (!IsActiveScreen) return;
                if (type == KeyMapper.KeyType.Down) pressedMoveRight = true;
                else if (type == KeyMapper.KeyType.Up) pressedMoveRight = false;
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:headup", type =>
            {
                if (!IsActiveScreen) return;
                if (type == KeyMapper.KeyType.Down) pressedHeadUp = true;
                else if (type == KeyMapper.KeyType.Up) pressedHeadUp = false;
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:headdown", type =>
            {
                if (!IsActiveScreen) return;
                if (type == KeyMapper.KeyType.Down) pressedHeadDown = true;
                else if (type == KeyMapper.KeyType.Up) pressedHeadDown = false;
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:headleft", type =>
            {
                if (!IsActiveScreen) return;
                if (type == KeyMapper.KeyType.Down) pressedHeadLeft = true;
                else if (type == KeyMapper.KeyType.Up) pressedHeadLeft = false;
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:headright", type =>
            {
                if (!IsActiveScreen) return;
                if (type == KeyMapper.KeyType.Down) pressedHeadRight = true;
                else if (type == KeyMapper.KeyType.Up) pressedHeadRight = false;
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:interact", type =>
            {
                if (!IsActiveScreen || type != KeyMapper.KeyType.Down) return;
                Manager.Player.InteractInput.Set(true);
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:apply", type =>
            {
                if (!IsActiveScreen || type != KeyMapper.KeyType.Down) return;
                Manager.Player.ApplyInput.Set(true);
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:jump", type =>
            {
                if (!IsActiveScreen || type != KeyMapper.KeyType.Down) return;
                Manager.Player.JumpInput.Set(true);
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:flymode", type =>
            {
                if (!IsActiveScreen || type != KeyMapper.KeyType.Down) return;
                Manager.Player.FlymodeInput = true;
            });
            for (int i = 0; i < 10; i++)
            {
                int tmp = i; // Nicht löschen. Benötigt, um aktuellen Wert zu fangen.
                Manager.Game.KeyMapper.AddAction("octoawesome:slot" + tmp, type =>
                {
                    if (!IsActiveScreen || type != KeyMapper.KeyType.Down) return;
                    Manager.Player.SlotInput[tmp] = true;
                });
            }
            Manager.Game.KeyMapper.AddAction("octoawesome:debug.allblocks", type =>
            {
                if (!IsActiveScreen || type != KeyMapper.KeyType.Down) return;
                Manager.Player.AllBlocksDebug();
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:debug.control", type =>
            {
                if (!IsActiveScreen || type != KeyMapper.KeyType.Down) return;
                debug.Visible = !debug.Visible;
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:inventory", type =>
            {
                if (!IsActiveScreen || type != KeyMapper.KeyType.Down) return;
                Manager.NavigateToScreen(new InventoryScreen(Manager));
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:hidecontrols", type =>
            {
                if (!IsActiveScreen || type != KeyMapper.KeyType.Down) return;
                compass.Visible = !compass.Visible;
                toolbar.Visible = !toolbar.Visible;
                minimap.Visible = !minimap.Visible;
                crosshair.Visible = !crosshair.Visible;
                debug.Visible = !debug.Visible;
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:exit", type =>
            {
                if (!IsActiveScreen || type != KeyMapper.KeyType.Down) return;
                Manager.NavigateToScreen(new PauseScreen(Manager));
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:freemouse", type =>
            {
                if (!IsActiveScreen || type != KeyMapper.KeyType.Down) return;
                if (Manager.MouseMode == MouseMode.Captured)
                    Manager.FreeMouse();
                else
                    Manager.CaptureMouse();
            });
            Manager.Game.KeyMapper.AddAction("octoawesome:teleport", type =>
            {
                if (!IsActiveScreen || type != KeyMapper.KeyType.Down) return;
                Manager.NavigateToScreen(new TargetScreen(Manager, (x, y) => 
                {
                    Manager.Game.Player.CurrentEntity.SetPosition(new Coordinate(0, new Index3(x, y, 300), new Vector3()));
                    Manager.NavigateBack();
                }, 
                    Manager.Game.Player.CurrentEntity.Position.GlobalBlockIndex.X, Manager.Game.Player.CurrentEntity.Position.GlobalBlockIndex.Y));
            });
        }

        #endregion

        #region GamePad Input

        private bool pressedGamepadInventory = false;
        private bool pressedGamepadInteract = false;
        private bool pressedGamepadApply = false;
        private bool pressedGamepadJump = false;
        private bool pressedGamepadFlymode = false;
        private bool pressedGamepadSlotLeft = false;
        private bool pressedGamepadSlotRight = false;

        private void HandleGamePad()
        {
            if (!IsActiveScreen) return;

            bool succeeded = false;
            GamePadState gamePadState = new GamePadState();
            try
            {
                gamePadState = GamePad.GetState(0);
                succeeded = true;
            }
            catch (Exception) { }

            if (succeeded)
            {
                Manager.Player.MoveValue += gamePadState.ThumbSticks.Left;
                Manager.Player.HeadValue += gamePadState.ThumbSticks.Right;

                if (gamePadState.Buttons.X == ButtonState.Pressed && !pressedGamepadInteract)
                    Manager.Player.InteractInput.Set(true);
                pressedGamepadInteract = gamePadState.Buttons.X == ButtonState.Pressed;

                if (gamePadState.Buttons.A == ButtonState.Pressed && !pressedGamepadApply)
                    Manager.Player.ApplyInput.Set(true);
                pressedGamepadApply = gamePadState.Buttons.A == ButtonState.Pressed;

                if (gamePadState.Buttons.Y == ButtonState.Pressed && !pressedGamepadJump)
                    Manager.Player.JumpInput.Set(true);
                pressedGamepadJump = gamePadState.Buttons.Y == ButtonState.Pressed;

                if (gamePadState.Buttons.LeftStick == ButtonState.Pressed && !pressedGamepadFlymode)
                    Manager.Player.FlymodeInput = true;
                pressedGamepadFlymode = gamePadState.Buttons.LeftStick == ButtonState.Pressed;

                if (gamePadState.Buttons.LeftShoulder == ButtonState.Pressed && !pressedGamepadSlotLeft)
                    Manager.Player.SlotLeftInput = true;
                pressedGamepadSlotLeft = gamePadState.Buttons.LeftShoulder == ButtonState.Pressed;

                if (gamePadState.Buttons.RightShoulder == ButtonState.Pressed && !pressedGamepadSlotRight)
                    Manager.Player.SlotRightInput = true;
                pressedGamepadSlotRight = gamePadState.Buttons.RightShoulder == ButtonState.Pressed;

                if (gamePadState.Buttons.Back == ButtonState.Pressed && !pressedGamepadInventory)
                    Manager.NavigateToScreen(new InventoryScreen(Manager));
                pressedGamepadInventory = gamePadState.Buttons.Back == ButtonState.Pressed;
            }
        }

        #endregion
    }
}
