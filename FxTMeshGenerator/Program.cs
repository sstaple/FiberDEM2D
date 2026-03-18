using FDEMCore;
using FxTMeshGenerator.Geometry;
using FxTMeshGenerator.IO;
using FxTMeshGenerator.Meshing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;


namespace FxTMeshGenerator
{
    //I want this to either start by loading an ipnut file or a pack file.  If it's an input file, then I want to run the whole process of generating the mesh.  If it's a pack file, then I just want to load the mesh and write it out in the desired format.
    internal static class Program
    {
        static void Main(string[] args)
        {
            args = new string[] { @"C:\Users\Scott_Stapleton\Downloads\RVE\V0p7YPeriodic.txt" }; //Work computer
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


            try
            {

                Console.WriteLine($"Found this file: {fileName}");

                myInputFile.Initiate();

                Console.WriteLine($"Ran packing for: {fileName}");

                //For debugging:
                Meshing.DebugOptions myDebugOptions = new Meshing.DebugOptions
                {
                    Debug = true,
                    Directory = dirName,
                    FileName = fileName
                };

                // Step 1: Generate Delaunay triangulation, and pass the debug options to enable debug output during triangulation
                var triangulator = new DelaunayTriangulator();
                var triangulation = triangulator.GenerateTriangulation(myInputFile.Packing.Boundary, myInputFile.Packing.LFibers, myDebugOptions);

                // Step 2: Build finite element mesh from triangulation
                var elementBuilder = new ElementBuilder();

                // Use mesh file name as debug output base path
                string vtkMeshFileName = Path.Combine(dirName, Path.GetFileNameWithoutExtension(fileName) + "_mesh.vtk");

                var femesh = elementBuilder.BuildMesh(
                    triangulation, 
                    myInputFile.Packing.LFibers, 
                    myInputFile.Packing.Boundary,
                    ElementConfig.Simple,
                    vtkMeshFileName); // Pass debug output path

                // Write triangulation for debugging
                string vtkTriFileName = Path.Combine(dirName, Path.GetFileNameWithoutExtension(fileName) + "_tri.vtk");
                VtkLegacyWriter.WriteUnstructuredGrid2D(vtkTriFileName, triangulation);

                // Write final mesh
                VtkLegacyWriter.WriteUnstructuredMesh(vtkMeshFileName, femesh);

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
            return myInputFile;
        }

    }
}