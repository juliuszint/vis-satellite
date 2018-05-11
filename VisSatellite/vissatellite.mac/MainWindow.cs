using System;

using Foundation;
using AppKit;

using OpenTK.Graphics.OpenGL;
using OpenTK.Platform.MacOS;
using System.Drawing;

using vissatellite.shared;

namespace vissatellite.mac
{
    public partial class MainWindow : NSWindow
    {
		public MonoMacGameView Game { get; set; }
		public VisSatteliteGame GameLogic { get; set; }

        public MainWindow(IntPtr handle) : base(handle)
        {
        }

        [Export("initWithCoder:")]
        public MainWindow(NSCoder coder) : base(coder)
        {
        }

		public override void AwakeFromNib()
        {
            base.AwakeFromNib();

			// Create new Game View and replace the window content with it
            this.Game = new MonoMacGameView(ContentView.Frame);
			GameLogic = new VisSatteliteGame();
            ContentView = Game;

            // Wire-up any required Game events
            Game.Load += (sender, e) => {
				// Initialize settings, load textures and sounds here
				GameLogic.Load();
            };

            // Adjust the GL view to be the same size as the window
            Game.Resize += (sender, e) => GL.Viewport(0, 0, Game.Size.Width, Game.Size.Height);

            Game.UpdateFrame += (sender, e) => {
                // Add any game logic or physics
            };

            Game.RenderFrame += (sender, e) => {
				GameLogic.Render();

            };

            // Run the game at 60 updates per second
            Game.Run(60.0);
        }
    }
}
