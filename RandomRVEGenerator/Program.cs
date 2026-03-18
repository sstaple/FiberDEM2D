
using System;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Globalization; //to set the language
using System.Threading.Tasks;
using FDEMCore;

namespace RandomRVEGenerator
{
    static class Program
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
				//add an open dialogue box
				OpenFileDialog openFldr = new OpenFileDialog()
				{
					Title = "Select File to Read From",
					Filter = "TXT Files (*.txt*)|*.txt*",
					FilterIndex = 2,
					RestoreDirectory = true,
					InitialDirectory = Directory.GetCurrentDirectory()
				};

				if (openFldr.ShowDialog() == DialogResult.OK)
				{
					try
					{
						string fullFileName = openFldr.FileName;
						string dirName = System.IO.Path.GetDirectoryName(openFldr.FileName); ;
						string sFileName = System.IO.Path.GetFileName(openFldr.FileName);
						RandomRVEGeneratorInputFile myInputFile = new RandomRVEGeneratorInputFile(sFileName, dirName);
						myInputFile.Initiate();
						MessageBox.Show("Congratulations, your run is finished.  I hope it was successful.");
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.ToString());
					}
				}
			}
			//If arguments are given when the .exe is called
			else
			{
				int l = args.Length;

				//For debugging
				/*for (int i = 0; i < args.Length; i++)
				{
					MessageBox.Show("Found {0}", args[i]);
				}*/


				foreach (string path in args)
				{
					//If the input argument is a filename....
					if (File.Exists(path))
					{
						//For debugging
						//MessageBox.Show($"Found this: {path}");
						ReadFilePath(path);
					}
					//If the input argument is a directory name,
					//find all of the .txt files and try to run them!
					else if (Directory.Exists(path))
					{
						string[] paths = Directory.GetFiles(path, "*.txt");

						//Decide how many cpus to use up (set at 75%)
						var opts = new ParallelOptions { MaxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 1.0)) };

						//Run each of the files in paralell...
						Parallel.For(0, paths.Length, opts, i => ReadFilePath(paths[i]));
						
					}
					else
					{
						MessageBox.Show($"{path} is not a valid file or directory.");
						
					}
				}
			}
		}
		private static void ReadFilePath(string path)
		{
			string filename = Path.GetFileName(path);
			string dirname = Path.GetDirectoryName(path);

			try
			{
				FDEMCore.RandomRVEGeneratorInputFile myInputFile = new FDEMCore.RandomRVEGeneratorInputFile(filename, dirname);

				myInputFile.Initiate();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}


			//Try to run each on it's own thread....
			/*ThreadStart job = new ThreadStart(myInputFile.Initiate);
			Thread thread = new Thread(job);
			thread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
			thread.Start();
			*/
			//Or not....
		}


	}
}