using System;
using System.Threading;
using System.IO;
using System.Collections;
using System.Text;
using CortexAccess;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace BandPowerLogger
{
    class Program
    {
        static void Main(string[] args)
        {
            // Start up message
            Console.WriteLine("BAND POWER LOGGER - Modified");

            // Ask user for specific headset
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[OPTIONAL] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" Enter the desired headset ID (Example: EPOCX-71D833AC): ");
            string WantedHeadsetId = Console.ReadLine();

            // Create a data stream
            DataStreamExample dse = new DataStreamExample();
            dse.AddStreams("pow");
            dse.OnSubscribed += SubscribedOK;
            dse.OnBandPowerDataReceived += OnBandPowerOK;
            dse.Start("", false, WantedHeadsetId);

            // Allow the program to run
            Console.WriteLine("\nPress Esc to end program and exit\n");
            while (Console.ReadKey().Key != ConsoleKey.Escape) { }

            // Unsubcribe from the stream
            dse.UnSubscribe();
            Thread.Sleep(5000);

            // Close the session
            dse.CloseSession();
            Thread.Sleep(5000);
        }

        private static void SubscribedOK(object sender, Dictionary<string, JArray> e)
        {
            foreach (string key in e.Keys)
            {
                if (key == "pow")
                {
                    // print header
                    ArrayList header = e[key].ToObject<ArrayList>();
                    //add timeStamp to header
                    header.Insert(0, "Timestamp");
                    WriteDataToConsole(header);
                }
            }
        }

        // Write Header and Data to Console
        private static void WriteDataToConsole(ArrayList data)
        {
            int i = 0;
            for (; i < data.Count - 1; i++)
            {
                Console.Write(data[i] + ", ");
            }
            // Last element
            Console.WriteLine(data[i]);
        }

        private static void OnBandPowerOK(object sender, ArrayList eegData)
        {
            WriteDataToConsole(eegData);
        }
    }
}
