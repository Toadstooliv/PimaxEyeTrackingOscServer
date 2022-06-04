using ASeeVROSCServer.ASeeVRInterface;
using Pimax.EyeTracking;
using System;
using System.Threading;


namespace ASeeVROSCServer
{
    class Program
    {

        static ASeeVRDataHandler dataHandler;
        static SharpOSC.UDPSender sender;
        static EyeTracker eyeTracker;
        static bool runThread;
        static OSCEyeTracker ConfigData;

        static void Main(string[] args)
        {
            sender = new SharpOSC.UDPSender("127.0.0.1", 9000);

            Console.WriteLine(System.IO.Directory.GetCurrentDirectory());
            ConfigData = new OSCEyeTracker();
            ConfigData.InitializeTrackingParams("Config.json");


            eyeTracker = new EyeTracker();
            eyeTracker.Start();

            runThread = true;

            Thread thr = new(new ThreadStart(Runner));
            thr.Start();

            Thread.Sleep(500);
            Console.Write("Press any key to stop...\n");

            Console.ReadKey();
            runThread = false;
            eyeTracker.Stop();
        }

        private static void Runner()
        {
            dataHandler = new ASeeVRDataHandler(eyeTracker, sender, ConfigData);
            while (true)
            {
                if (!runThread) break;
                Thread.Sleep(1000);
            }
            Console.WriteLine("Stopped");
        }
    }
}
