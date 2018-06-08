namespace vissatellite
{
    public class Program
    {
		private static SatelliteUniverse gameWindow;

        public static void Main(string[] args)
        {
			gameWindow = new SatelliteUniverse(1280, 720);
			gameWindow.Run(60.0);
        }
    }

}
