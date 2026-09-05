using System;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO.Enumeration;

namespace MicroCluster
{
    class Program
    {
        // Creates instance of class that controls output options
        private static OutputOptions? outputOptions;

        // To ensure single thread operations
        [STAThread]
        
        // Main executable
        private static void Main(string[] args)
        {
            //Run input Arguments 
            RunArguments(args);

            // Exit Messgae 
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        // Main function to start program running
        private static void RunArguments(string[] args)
        {
            DisplayMessage();

            Console.WriteLine("Please enter the path to your CSV file or folder containing CSV files:");
            string? filePath = Console.ReadLine();

            if (filePath == null)
            {
                Console.WriteLine("No file path provided.");
                return;
            }

            // string of outputs object
            string? optionsPath = null;
            // string of output csv file
            string? outputCsvPath = null;

            if (File.Exists(filePath))
            {
                string directory = Path.GetDirectoryName(filePath) ?? ".";
                optionsPath = Path.Combine(directory, "OutputOptions.txt");
            }
            else if (Directory.Exists(filePath))
            {
                optionsPath = Path.Combine(filePath, "OutputOptions.txt");
            }
            OutputOptions outputOptions = OutputOptions.Load(optionsPath);



            if (File.Exists(filePath))
            {
                // Single file mode
                Console.WriteLine("Single file detected.");

                string directory = Path.GetDirectoryName(filePath) ?? ".";
                outputCsvPath = Path.Combine(directory, "summary_output.csv");
                
                // Create CSV with header
                File.WriteAllText(outputCsvPath, "FileName,Vf_Median,Vf_IQR,FC_area_density,MRC_area_density,FC_cluster_density,MRC_cluster_density\n");

                // Process just one file
                RunFilePath(filePath, outputCsvPath, outputOptions);
            }
            else if (Directory.Exists(filePath))
            {
                // Directory mode
                Console.WriteLine("Directory detected. Processing all CSV files.");

                string[] paths = Directory.GetFiles(filePath, "*.csv");

                if (paths.Length == 0)
                {
                    Console.WriteLine("No CSV files found in the directory.");
                    return;
                }

                outputCsvPath = Path.Combine(filePath, "summary_output.csv");
                
                // Create CSV with header
                File.WriteAllText(outputCsvPath, "FileName,Vf_Median,Vf_IQR,FC_area_density,MRC_area_density,FC_cluster_density,MRC_cluster_density\n");

                // Parallel processing
                Parallel.For(0, paths.Length, i =>
                {
                    RunFilePath(paths[i], outputCsvPath, outputOptions);
                });
            }
            else
            {
                Console.WriteLine("Invalid file or directory.");
            }
        }

        // Displays logo on launch
        private static void DisplayMessage()
        {
            string asciiArt = @"

             _____ _           _            
            / ____| |         | |           
           | |    | |_   _ ___| |_ ___ _ __ 
     __   _| |    | | | | / __| __/ _ \ '__|
    / /  / | |____| | |_| \__ \ ||  __/ |   
   / |__/ / \_____|_|\__,_|___/\__\___|_|   
  /  __/ /   Fiber and Matrix-Rich Cluster Characterization Algorithm
 / /   \_\    
/_/
";

            Console.WriteLine(asciiArt);
        }

        // Runs user input
        private static void RunFilePath(string filePath, string outputCsvPath, OutputOptions outputOptions)
        {
            // Extract important stuff from file 
            string fileName = Path.GetFileName(filePath);
            string? saveDir = Path.GetDirectoryName(filePath);

            // Create new input file instance
            PackFile packFile = new PackFile(filePath, saveDir);

            // Start a stopwatch 
            Stopwatch stopWatch = new Stopwatch(); 
            stopWatch.Start();

            
            // using try and catch to run input file
            try
            {
                // run processing message
                Console.WriteLine($"Starting to process {fileName}");

                // Start program running
                packFile.Initiate(outputOptions);

                Microstructure? microstructure = packFile.Microstructure;


                // Append results
                lock (_csvLock)
                {
                    File.AppendAllText(outputCsvPath,
                        $"{fileName},{microstructure.VfMdn}, {microstructure.VfIqr}, {microstructure.FCDensity},{microstructure.MRCDensity},{microstructure.FCNumDensity},{microstructure.MRCNumDensity}\n");
                }

                // Elapsed time output
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}:{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

                // Output message
                Console.WriteLine($"Ran {fileName} in {elapsedTime}");
            }
            catch (Exception ex)
            {
                // throw exception error
                Console.WriteLine(ex.ToString());
            }

        }

        // CSV lock prevents overwriting in parallelization
        private static readonly object _csvLock = new object();

  

    }
}