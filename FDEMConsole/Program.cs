using System;
using System.Threading;
using System.Globalization; //to set the language
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using FDEMCore;

namespace FDEMConsole
{
    class Program
    {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		private static void Main(string[] args)
		{
			//This is to get rid of issues I had in germany...
			Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

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
			/*
			//now leave the window open until someone hits enter
			Console.WriteLine("Finished!  Press enter to close console");
			Console.Out.Flush();
			var dummyInput = Console.ReadLine();
			Environment.Exit(0);
			*/
		}

		private static void RunArguments(string[] args)
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

					//Decide how many cpus to use up (set at 75%)
					//var opts = new ParallelOptions { MaxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 1.0) * 1.0)) };
					//Console.WriteLine(opts.ToString());
					//Console.WriteLine(opts.MaxDegreeOfParallelism);
					//Console.WriteLine("numberOfFilesFound" + paths.Length);
					//Run each of the files in paralell...
					//Parallel.For(0, paths.Length, opts, i => ReadFilePath(paths[i]));
					Parallel.For(0, paths.Length, i => ReadFilePath(paths[i]));

				}
				else
				{
					Console.WriteLine($"{path} is not a valid file or directory.");

				}
			}
		}
		private static void ReadFilePath(string path)
		{
			
			string filename = Path.GetFileName(path);
			string dirname = Path.GetDirectoryName(path);
			Stopwatch stopWatch = new Stopwatch();
			stopWatch.Start();
			InputFile myInputFile = new InputFile(filename, dirname);


			try
			{
				
				Console.WriteLine($"Found this file: {filename}");

				myInputFile.Initiate();

				stopWatch.Stop();
				TimeSpan ts = stopWatch.Elapsed;
				// Format and display the TimeSpan value.
				string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
					ts.Hours, ts.Minutes, ts.Seconds,
					ts.Milliseconds / 10);

				Console.WriteLine($"Ran file: {filename} in {elapsedTime}. I hope it was successful.");
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());

				//First, write out the Stress/Strain
				OutputParameters myOutput = new OutputParameters(dirname, false, false, true, false);
				myOutput.FileName = System.IO.Path.GetFileNameWithoutExtension(filename);
				myOutput.FileIndex = myInputFile.outParams.FileIndex;
				myInputFile.myAnalysis.GenerateOutput(myOutput, 1);

				//Now, try to generate all of the data (this usually doesn't work...)
				OutputParameters myErrorOutputParams = new OutputParameters(dirname, true, false, false, false);
				myErrorOutputParams.FileName = System.IO.Path.GetFileNameWithoutExtension(filename);
				myErrorOutputParams.FileIndex = myInputFile.outParams.FileIndex;
				myErrorOutputParams.EndingForAll = "_ErrorAll";
				myInputFile.myAnalysis.GenerateOutput(myErrorOutputParams, 1);
			}
		}


	}
}