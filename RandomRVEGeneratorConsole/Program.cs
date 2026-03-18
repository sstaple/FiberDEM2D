using System;
using System.Threading;
using System.Globalization; //to set the language
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using FDEMCore;
using RandomMath;

namespace RandomRVEGeneratorConsole
{
    class Program
    {
        static void Main(string[] args)
        {
			//If no arguments are passed...
			if (args.Length == 0)
			{
				Console.WriteLine("Please enter an input file(s) name or directory(s) containing input files.  If there are multiple, separate them with a space.");
				Console.Out.Flush();
				var input = Console.ReadLine();
				args = input.Split(' ');
				RunArguments(args);
			}
			//If arguments are given when the .exe is called
			else
			{
				RunArguments(args);
			}

			//now leave the window open until someone hits enter
			Console.WriteLine("Finished!  Press enter to close console");
			Console.Out.Flush();
			var dummyInput = Console.ReadLine();
			Environment.Exit(0);
		}

		public static void RunArguments(string[] args)
		{
			int l = args.Length;

			foreach (string path in args)
			{
				//If the input argument is a filename....
				if (File.Exists(path))
				{
					//no parallel stuff: just run it!
					ReadFilePath(path);
				}
				//If the input argument is a directory name,
				//find all of the .txt files and try to run them!
				else if (Directory.Exists(path))
				{
					string[] paths = Directory.GetFiles(path, "*.txt");
					Console.WriteLine($"Found this directory: {path}");

					Parallel.For(0, paths.Length, i => ReadFilePath(paths[i]));

				}
				else
				{
					Console.WriteLine($"{path} is not a valid file or directory.");

				}
			}
		}
		public static void ReadFilePath(string path)
		{

			string fileName = Path.GetFileName(path);
			string dirName = Path.GetDirectoryName(path);
			Stopwatch stopWatch = new Stopwatch();
			stopWatch.Start();
			RandomRVEGeneratorInputFile myInputFile = new RandomRVEGeneratorInputFile(fileName, dirName);


			try
			{

				Console.WriteLine($"Found this file: {fileName}");

				//Generate the RVE and write the output file.
				myInputFile.Initiate();

				stopWatch.Stop();
				TimeSpan ts = stopWatch.Elapsed;
				// Format and display the TimeSpan value.
				string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
					ts.Hours, ts.Minutes, ts.Seconds,
					ts.Milliseconds / 10);

				Console.WriteLine($"Ran file: {fileName} in {elapsedTime}. I hope it was successful.");
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				//Write an error file just to make it clear.
				string errorFileName = Path.Combine(dirName, fileName + "_error.txt");
				StreamWriter dataWrite = new StreamWriter(errorFileName);
				dataWrite.WriteLine(ex.ToString());
				dataWrite.Close();
			}
		}

	}
}
