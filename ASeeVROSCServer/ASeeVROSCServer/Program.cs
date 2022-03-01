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

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            sender = new SharpOSC.UDPSender("127.0.0.1", 9000);

            Console.WriteLine(System.IO.Directory.GetCurrentDirectory());

            eyeTracker = new EyeTracker();
            eyeTracker.Start();

            runThread = true;

            Thread thr = new Thread(new ThreadStart(Program.Runner));
            thr.Start();

            Thread.Sleep(500);
            Console.Write("Press any key to stop...");

            Console.ReadKey();
            runThread = false;
            eyeTracker.Stop();
            
        }

        private static void Runner()
        {
            dataHandler = new ASeeVRDataHandler(eyeTracker, sender, 6);
            while (runThread) ;
        }
    }
}
