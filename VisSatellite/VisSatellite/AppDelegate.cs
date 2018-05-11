using AppKit;
using Foundation;

namespace VisSatellite
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
		private MainWindowController mainWindowController;

		public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
		{
			return true;
		}
        
        public override void DidFinishLaunching(NSNotification notification)
        {
            // Insert code here to initialize your application
			this.mainWindowController = new MainWindowController();
            this.mainWindowController.Window.MakeKeyAndOrderFront(this);
        }
       
        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
        }
    }
}
