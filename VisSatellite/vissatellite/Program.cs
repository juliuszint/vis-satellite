using OpenTK;
using OpenTK.Graphics;

namespace vissatellite
{
    public class Program
    {
		private static VisUniversumGameWindow gameWindow;

        public static void Main(string[] args)
        {
			gameWindow = new VisUniversumGameWindow(1280, 720);
			gameWindow.Run(60.0);
        }
    }

	public class VisUniversumGameWindow : GameWindow
	{
		public VisUniversumGameWindow(int width, int height)
			: base(width, height, GraphicsMode.Default, "Satellite Universum", 0, DisplayDevice.Default, 3, 3, GraphicsContextFlags.ForwardCompatible)
		{
			
		}
	}
}
