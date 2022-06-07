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
        static bool runThread, calibrate;
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
            Console.Write("Press S to stop or C to calibrate...\n");

            while (true)
            {
                ConsoleKeyInfo input = Console.ReadKey();
                if (input.KeyChar == char.Parse("s"))
                {
                    runThread = false;
                    eyeTracker.Stop();
                    break;
                }
                else if (input.KeyChar == char.Parse("c"))
                {
                    calibrate = true;
                }
                Thread.Sleep(100);
            }
        }

        private static void Runner()
        {
            dataHandler = new ASeeVRDataHandler(eyeTracker, sender, ConfigData);
            while (true)
            {
                if (!runThread) break;
                if (calibrate)
                {
                    calibrate = false;
                    dataHandler.Calibrate();
                }
                Thread.Sleep(500);
            }
            Console.WriteLine(" Stopped successfully.");
        }
    }
}
