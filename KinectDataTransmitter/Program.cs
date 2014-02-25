using System;
using System.Threading;
using DataConverter;
using Microsoft.Kinect;

namespace KinectDataTransmitter
{
  
    class Program
    {
        private static int _nonAcknoledgedPings;

        static void Main(string[] args)
        {

            Console.WriteLine(Converter.EncodeInfo("Starting up..."));


            if (KinectSensor.KinectSensors.Count == 0)
            {
                Console.WriteLine(Converter.EncodeError("No kinect device was found."));
                return;
            }

            try
            {
                
                var kinect = new KinectDevice();
                kinect.IsTrackingSkeletons = true;
                kinect.IsTrackingHands = true;
                /*
                 * //for the future...
                kinect.IsTrackingFace = false;
                kinect.IsWritingColorStream = true;
                kinect.IsWritingDepthStream = true;
                 * */

                Thread pingThread = new Thread(SendPings);
                pingThread.Start();

                string inputStr = null;
                while ((inputStr = Console.ReadLine()) != null)
                {
                    const string byteOrderMark = "ï»¿";
                    if (inputStr[0] == 0xEF && inputStr[1] == 0xBB && inputStr[2] == 0xBF)
                    {
                        // ignore the bom.
                        continue;
                    }

                    if (Converter.IsPing(inputStr))
                    {
                        _nonAcknoledgedPings--;
                    }
                    else
                    {
                        int i = inputStr.IndexOf('1');
                        Console.WriteLine(inputStr + " " + i + " " + (inputStr[0] == 0xEF) + " " + (inputStr[1] == 0xBB) + " " + (inputStr[2] == 0xBF));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(Converter.EncodeError(e.Message));
            }
        }

        private static void SendPings()
        {
            while (true)
            {
                Console.WriteLine(Converter.EncodePingData());
                _nonAcknoledgedPings++;
                Thread.Sleep(10000);

                if (_nonAcknoledgedPings >= 2)
                {
                    Environment.Exit(-1);
                }
            }
        }
    }
}
