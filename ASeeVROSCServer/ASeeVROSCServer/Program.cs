using ASeeVROSCServer.ASeeVRInterface;
using Pimax.EyeTracking;
using System;
using System.Threading;

namespace ASeeVROSCServer
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var sender = new SharpOSC.UDPSender("127.0.0.1", 9000);

            Console.WriteLine(System.IO.Directory.GetCurrentDirectory());

            var eyeTracker = new EyeTracker();
            eyeTracker.Start();

            while(!eyeTracker.Active)
            { }

            var dataHandler = new ASeeVRDataHandler(eyeTracker, sender, 10);
        }
    }
}
