using FDEMCore;
using FDEMCore.FxTMesh.Geometry;
using FDEMCore.FxTMesh.IO;
using FDEMCore.FxTMesh.Meshing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;


namespace FxTMeshGenerator
{
    //I want this to either start by loading an input file or a pack file.  If it's an input file, then I want to run the whole process of generating the mesh.  If it's a pack file, then I just want to load the mesh and write it out in the desired format.
    internal static class Program
    {
        static void Main(string[] args)
        {
            args = new string[] { @"C:\Users\Scott_Stapleton\Downloads\RVE\Test2\V0p7YPeriodic.txt" }; //Work computer
            //args = new string[] { @"C:\Users\scott\Downloads\RVE\V0p7YPeriodic.txt" }; //Laptop computer

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
        public static RandomRVEGeneratorInputFile ReadFilePath(string path)
        {

            string fileName = Path.GetFileName(path); 
            string dirName = Path.GetDirectoryName(path);
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            RandomRVEGeneratorInputFile myInputFile = new RandomRVEGeneratorInputFile(fileName, dirName);


            string fileNameNoExt = Path.GetFileNameWithoutExtension(path);
            //For debugging, make and pass the DebugOptions to the triangulator and element builder.  This will cause them to write out debug files during their processes, which can be helpful for diagnosing issues with the triangulation and meshing.
            FDEMCore.FxTMesh.Meshing.DebugOptions myDebugOptions = new FDEMCore.FxTMesh.Meshing.DebugOptions {Debug = true,Directory = dirName,
                FileName = fileNameNoExt };

            try
            {
                Console.WriteLine($"Found this file: {fileNameNoExt}");

                myInputFile.Initiate();

                Console.WriteLine($"Ran packing for: {fileNameNoExt}");

                // Step 1: Generate Delaunay triangulation, and pass the debug options to enable debug output during triangulation
                var triangulator = new DelaunayTriangulator();
                var triangulation = triangulator.GenerateTriangulation(myInputFile.Packing.Boundary, myInputFile.Packing.LFibers, 
                    myDebugOptions);

                // Step 2: Build finite element mesh from triangulation
                var elementBuilder = new MeshBuilder();
                var feMesh = elementBuilder.BuildMesh(triangulation,myInputFile.Packing.LFibers, myInputFile.Packing.Boundary,
                    FxTElementFamily.Type2, myDebugOptions); // Pass debug output path

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                // Format and display the TimeSpan value.
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);

                Console.WriteLine($"Ran file: {fileNameNoExt} in {elapsedTime}. I hope it was successful.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                //Write an error file just to make it clear.
                string errorFileName = Path.Combine(dirName, fileNameNoExt + "_error.txt");
                StreamWriter dataWrite = new StreamWriter(errorFileName);
                dataWrite.WriteLine(ex.ToString());
                dataWrite.Close();
            }
            return myInputFile;
        }

    }
}