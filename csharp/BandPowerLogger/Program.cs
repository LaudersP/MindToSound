using System;
using System.Threading;
using System.Collections;
using System.Text;
using CortexAccess;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace BandPowerLogger
{
    class Program
    {
        private static CortexAccess.Utils _utilities = new CortexAccess.Utils();
        private static OSC _osc;
        private static string _ipAddress = "127.0.0.1";        
        private static int _wantedPortNumber = 55555;

        private static bool _saveData = false;
        private static string _csvFilename;
        private static FileStream _outFileStream;
        
        static void Main(string[] args)
        {
            // Start up message
            PrintProgramTitle();

            // Ask user for specific headset
            _utilities.SendColoredMessage("OPTIONAL", ConsoleColor.Yellow, "Enter the desired headset ID (Example: EPOCX-71D833AC): ", false);
            string wantedHeadsetId = Console.ReadLine();

            // Ask the user for specific port to host the OSC channel on
            while (true)
            {
                _utilities.SendColoredMessage("OPTIONAL", ConsoleColor.Yellow, "Enter the desired port number (Default: 55555, acceptable ports 49152-65535): ", false);
                string userInput = Console.ReadLine();

                // Check if the port should be left default
                if (userInput == "")
                    break;

                // Ensure the port is valid
                int intInput = Convert.ToInt32(userInput);
                if(intInput < 49152 || intInput > 65535) // Registerd ports
                {
                    continue;
                }

                // Set the port
                _wantedPortNumber = intInput;
                _utilities.SendSuccessMessage("Set port to \'" + _wantedPortNumber + "\'");
                Console.WriteLine();
                break;
            }

            // Ask the user if they would like to save the data into a CSV file
            while (true)
            {
                _utilities.SendColoredMessage("OPTIONAL", ConsoleColor.Yellow, "Would you like to save the session data? (Y/N): ", false);
                string userInput = Console.ReadLine();
                if (userInput.ToUpper() == "Y")
                {
                    _saveData = true;
                    break;
                }
                else if(userInput.ToUpper() == "N" || userInput == "")
                {
                    break;
                }
            }

            if(_saveData)
            {
                // Get subject name
                Console.Write("    Subject Name: ");
                _csvFilename = Console.ReadLine() + " ";

                // Get the date/time
                _csvFilename += DateTime.Now + ".csv";
                
                // Final formatting of the filename
                _csvFilename = _csvFilename.Replace(' ', '_');
                _csvFilename = _csvFilename.Replace(':', '_');
                _csvFilename = _csvFilename.Replace('/', '_');

                _utilities.SendColoredMessage("SAVING", ConsoleColor.Magenta, "Output path: \'" + _csvFilename + "\'", true);

                string saveDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "saved_sessions");
                if (!Directory.Exists(saveDirectory))
                    Directory.CreateDirectory(saveDirectory);

                _outFileStream = new FileStream(Path.Combine(saveDirectory, _csvFilename), FileMode.Append, FileAccess.Write);
            }
            Console.WriteLine();

            // Create the OSC instance
            _osc = new OSC(_ipAddress, _wantedPortNumber);

            // Create a data stream
            DataStreamExample dse = new DataStreamExample();
            dse.AddStreams("pow");
            dse.OnSubscribed += SubscribedOK;
            dse.OnBandPowerDataReceived += OnBandPowerOK;
            dse.Start("", false, wantedHeadsetId);

            // Block while the program is running
            while (Console.ReadKey().Key != ConsoleKey.Escape);

            // Send shutdown message
            Console.Write("     ");
            Thread.Sleep(500);
            _utilities.SendWarningMessage("Attempting to closing the program, please wait...");

            // Unsubcribe from the stream
            dse.UnSubscribe();
            Thread.Sleep(5000);

            // Close the session
            dse.CloseSession();
            Thread.Sleep(5000);

            // Close the file if needed
            if(_saveData)
            {
                _outFileStream.Dispose();
                _utilities.SendColoredMessage("SAVING", ConsoleColor.Magenta, "Data save completed.", true);
            }

            // End of program
            _utilities.SendSuccessMessage("Program terminated.");
        }

        private static void PrintProgramTitle()
        {
            string[] programTitle =
            {
                "\t /$$      /$$ /$$                 /$$ /$$$$$$$$         /$$$$$$                                      /$$",
                "\t| $$$    /$$$|__/                | $$|__  $$__/        /$$__  $$                                    | $$",
                "\t| $$$$  /$$$$ /$$ /$$$$$$$   /$$$$$$$   | $$  /$$$$$$ | $$  \\__/  /$$$$$$  /$$   /$$ /$$$$$$$   /$$$$$$$",
                "\t| $$ $$/$$ $$| $$| $$__  $$ /$$__  $$   | $$ /$$__  $$|  $$$$$$  /$$__  $$| $$  | $$| $$__  $$ /$$__  $$",
                "\t| $$  $$$| $$| $$| $$  \\ $$| $$  | $$   | $$| $$  \\ $$ \\____  $$| $$  \\ $$| $$  | $$| $$  \\ $$| $$  | $$",
                "\t| $$\\  $ | $$| $$| $$  | $$| $$  | $$   | $$| $$  | $$ /$$  \\ $$| $$  | $$| $$  | $$| $$  | $$| $$  | $$",
                "\t| $$ \\/  | $$| $$| $$  | $$|  $$$$$$$   | $$|  $$$$$$/|  $$$$$$/|  $$$$$$/|  $$$$$$/| $$  | $$|  $$$$$$$",
                "\t|__/     |__/|__/|__/  |__/ \\_______/   |__/ \\______/  \\______/  \\______/  \\______/ |__/  |__/ \\_______/",
                "\n"
            };

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            foreach(string line in programTitle)
            {
                Console.WriteLine(line);
            }
            Console.ForegroundColor= ConsoleColor.White;
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

                    if(_saveData)
                        WriteDataToFile(header);
                }
            }
        }

        // Write data to OSC
        private static void WriteDataToOSC(ArrayList data)
        {
            // Check for a valid array of data
            if (data.Count == 0)
            { 
                _utilities.SendErrorMessage("No data received.");
                return;
            }

            string[] bands = { "theta", "alpha", "betaL", "betaH", "gamma" };
            string[] sensors = { "AF3", "F7", "F3", "FC5", "T7", "P7", "O1", "O2", "P8", "T8", "FC6", "F4", "F8", "AF4" };

            // Output data in OSC protocol
            // ... /{band}/{sensor} {value}
            int i = 1;
            for (; i < data.Count - 1; i++)
            {
                // Determine the band
                string band = bands[(i - 1) % 5];

                // Assign the sensor
                string sensor = sensors[(i - 1) / 5];

                // Construct the OSC address string
                string OscAddressString = $"/{band}/{sensor}";

                // Construct the arguments
                float floatValue = Convert.ToSingle(data[i]);
                object[] args = { floatValue };

                // Send the OSC message
                _osc.SendMessage(OscAddressString, args);
            }
        }

        // Write Header and Data to File
        private static void WriteDataToFile(ArrayList data)
        {
            int i = 0;
            for (; i < data.Count - 1; i++)
            {
                byte[] val = Encoding.UTF8.GetBytes(data[i].ToString() + ", ");

                if (_outFileStream != null)
                    _outFileStream.Write(val, 0, val.Length);
                else
                    break;
            }
            // Last element
            byte[] lastVal = Encoding.UTF8.GetBytes(data[i].ToString() + "\n");
            if (_outFileStream != null)
                _outFileStream.Write(lastVal, 0, lastVal.Length);
        }

        private static void OnBandPowerOK(object sender, ArrayList eegData)
        {
            if(_saveData)
                WriteDataToFile(eegData);

            WriteDataToOSC(eegData);
        }
    }
}
