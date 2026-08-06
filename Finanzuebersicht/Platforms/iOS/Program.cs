using Finanzuebersicht.Services;
using ObjCRuntime;
using UIKit;

namespace Finanzuebersicht;

public class Program
{
	// This is the main entry point of the application.
	static void Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += (_, e) =>
			CrashLog.Write("UnhandledException", e.ExceptionObject as Exception);

		TaskScheduler.UnobservedTaskException += (_, e) =>
		{
			CrashLog.Write("UnobservedTaskException", e.Exception);
			e.SetObserved();
		};

		Runtime.MarshalManagedException += (_, args) =>
		{
			CrashLog.Write("MarshalManagedException", args.Exception);
		};

		UIApplication.Main(args, null, typeof(AppDelegate));
	}
}
