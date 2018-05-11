namespace vissatellite
{
    public class Program
    {
		private static SatelliteUniversum gameWindow;

        public static void Main(string[] args)
        {
			gameWindow = new SatelliteUniversum(1280, 720);
			gameWindow.Run(60.0);
        }
    }

}
