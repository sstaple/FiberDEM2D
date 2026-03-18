using System;
using System.Collections.Generic;
using DelaunayTriangulator;
using NUnit.Framework;

namespace FDEMTests
{
	/*
	public partial class TestTriangulationPanel
	{
		public List<Vertex> ReadVertices(string file_name)
		{
			string line;
			int ColumnNumber = -1;
			List<Vertex> vertice_list = new List<Vertex>();

			using (System.IO.StreamReader in_file = new System.IO.StreamReader(file_name))
			{
				while ((line = in_file.ReadLine()) != null)
				{
					if (line.Contains("*Fibers"))
					{
						line = in_file.ReadLine();
						var entries = line.Split(',');
						for (int j = 0; j < entries.Length; j++)
						{
							if (entries[j].Contains("CenterY"))
							{
								ColumnNumber = j;
							}
						}

						while ((line = in_file.ReadLine()) != null)
						{
							if (line.Contains("*"))
							{
								break;
							}
							entries = line.Split(',');

							vertice_list.Add(new Vertex((float)Convert.ToDouble(entries[ColumnNumber]), (float)Convert.ToDouble(entries[ColumnNumber + 1])));
						}
						break;
					}
				}
			}
			return vertice_list;
		}

		[Test]
		public void TriangulateSample()
		{
			string filename = "SampleInputFile_Random_LoadStepAnalysis_vf0p400_nf8_All_1.csv";
			List<Vertex> vertices = ReadVertices(filename);
			Triangulator dTrian = new Triangulator();
			dTrian.Triangulation(vertices, true);
			List<int[]> pair = dTrian.Connections;

			using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename.Remove(filename.Length - 4) + "_Connections.txt"))
			{
				file.WriteLine("Point_1_x\tPoint_1_y\tPoint_2_x\tPoint_2_y\n");
				for (int i = 0; i < pair.Count; i += 1)
				{
					
					file.Write(vertices[pair[i][0]].x);
					file.Write("\t");
					file.Write(vertices[pair[i][0]].y);
					file.Write("\t");
					file.Write(vertices[pair[i][1]].x);
					file.Write("\t");
					file.Write(vertices[pair[i][1]].y);
					file.Write(Environment.NewLine);
				}
			}
		}

		[Test]
		public void DrawMatrixWithTriangulation(){
			
			FiberParameters myFP = new FiberParameters(3.5 , 6.1811E-7, 35.0, 24000, 1500,
			                                           0.27, 0.5);
			
			RandomPack myRP = new RandomPack(7, 0.5, myFP) { minSpacingBetweenFibers = 0.5 };
			
			//myRP.nMaxSteps = 50;
			//myRP.squareMargin = 3;
			//AsymmetricHexagonalWithVf myRP = new AsymmetricHexagonalWithVf(0.5, 3, myFP, 7.0);
			myRP.SetPacking();
			
			
			INonContactSpringParameters myMP = new MatrixContinuumParameters(4950.0, 0.37, 1.0, 1, 0.01, "MatrixContinuum", "VonMises", "100/");
			
			ContactParameters myCP = new ContactParameters(0.1, 0.1, 1.0, 1.0);
			
			LoadStepAnalysis mylsa = new LoadStepAnalysis(new double[6], 3, 0.0000001, 1, 1, myCP);			
			mylsa.AddNonContactSprings(myMP);
			
			mylsa.Analyze(myRP.LFibers, myRP.Boundary, myRP.TheGrid);
			
			PlotSingleFrame mySF = new PlotSingleFrame(myRP.LFibers, myRP.Boundary, mylsa.LSprings,
			                                           myRP.TheGrid,3);
			
			PlotSingleFrame mySF2 = new PlotSingleFrame(myRP.LFibers, myRP.Boundary, mylsa.LSprings,
			                                           myRP.TheGrid,3, Color.Black, Color.White, Color.Goldenrod);
		}
	}*/
}
