using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Globalization; //to set the language
using RandomMath;


namespace FDEMCore
{
	[SerializableAttribute] //This allows to make a deep copy fast
	public class Experiment
	{
		#region Private Members
		private OutputParameters outParams;
		private Packing pack;
		private Analysis analysis;
		private bool isMultiThreaded=false;
		private string originalFileName;
		private bool noRepitions;
        #endregion

        #region Public Members

        #endregion

        #region Constructors
        public Experiment (Packing myPack, Analysis myAnalysis, OutputParameters myOutputParams, string InputFileName)
		{
			originalFileName = InputFileName;
			outParams = myOutputParams;
			pack = myPack;
			analysis = myAnalysis;

        }
		#endregion

		#region public methods
		public void VaryMinDistance(double[] minDist, int nRuns)
		{
			string fileName = originalFileName;

			RandomPack myran = (RandomPack)pack;

			noRepitions = (nRuns == 1);

			for (int i = 0; i < minDist.Length; i++)
			{
				//Set the packing rows
				string smd = minDist[i].ToString("F3");
				smd = smd.Replace(".", "p");

				myran.minSpacingBetweenFibers = minDist[i];

				outParams.FileName = fileName + "_md" + smd;

				//Run the run
				RunSingleRunWrapper(nRuns);
			}
		}

		public void VaryVolumeFraction(int[] volFrac, int nRuns)
		{
			string fileName = originalFileName;

			noRepitions = (nRuns == 1);

			for (int i = 0; i < volFrac.Length; i++)
			{
				//Set the packing rows
				pack.NRows = volFrac[i];

				string smd = volFrac[i].ToString("F3");
				smd = smd.Replace(".", "p");
				outParams.FileName = fileName + "_vf" + volFrac[i];

				//Run the run
				RunSingleRunWrapper(nRuns);
			}
		}
		
		public void VaryRVESize(int [] nRows, int nRuns, double voFrac)
		{
			string sVf = voFrac.ToString("F3");
			sVf = sVf.Replace(".", "p");
			//string fileName = originalFileName  + "_" +  pack.SType + "_" + analysis.SType + "_vf"  + sVf;
			string fileName = originalFileName;

			noRepitions = (nRuns == 1);

			for (int i = 0; i < nRows.Length; i++) {
				//Set the packing rows
				pack.NRows = nRows[i];

				outParams.FileName = fileName + "_nf" + nRows[i];
				
				//Run the run
				RunSingleRunWrapper(nRuns);
			}
		}

		public void VaryRVESizeAndNTS(int [] nRows, int [] nTimeSteps,int nRuns)
		{
			string fileName = originalFileName  + "_" +  pack.SType + "_" + analysis.SType;

			noRepitions = (nRuns == 1);

			for (int i = 0; i < nRows.Length; i++) {
				//Set the packing rows
				pack.NRows = nRows[i];
				//Set the nTimeSteps
				analysis.SetTimeSteps(nTimeSteps[i]);

				string tempNTS = (nTimeSteps[i] / 1000.0).ToString();
				outParams.FileName = fileName + "_nTS" + tempNTS + "k" + "_nf" + nRows[i]; 

				//Run the run
				RunSingleRunWrapper(nRuns);
			}
		}

		public void VaryProbability(double [] nProb, SizingParameters sP,int nRuns)
		{
			string tempNTS = (pack.FVolFraction).ToString();
			tempNTS=tempNTS.Replace (".", "p");
			string fileName = originalFileName  + "_" +  pack.SType + "_" + analysis.SType + "_vf" + pack.FVolFraction + "_nf" + pack.NRows;
			noRepitions = (nRuns == 1);

			for (int i = 0; i < nProb.Length; i++) {
				//Set the probability and sizing
				sP.Probability = nProb[i];
				analysis.AddNonContactSprings(sP);

				tempNTS = (nProb[i]).ToString();
				tempNTS=tempNTS.Replace (".", "p");
				outParams.FileName = fileName + "_prob" + tempNTS; 

				//Run the run
				RunSingleRunWrapper(nRuns);
			}
		}

		public void VaryDamping(double [] damp, SizingParameters sP, int nRuns)
		{
			string fileName = originalFileName + "_" + pack.SType + "_" + analysis.SType + "_nf" + pack.NRows + "_nTS" + pack.NRows;
			noRepitions = (nRuns == 1);

			for (int i = 0; i < damp.Length; i++) {
				//Set the damping and sizing
				sP.DampCoeff = damp[i];
				analysis.AddNonContactSprings(sP);

				string tempNTS = (damp[i]).ToString();
				tempNTS=tempNTS.Replace (".", "p");
				outParams.FileName = fileName + "_sizD" + tempNTS; 

				//Run the run
				RunSingleRunWrapper(nRuns);
			}
		}

		public void VaryNTimeSteps(int [] nTimeSteps, int nRuns)
		{
			string fileName = originalFileName + "_" + pack.SType + "_" + analysis.SType + "_nf" + pack.NRows;
			noRepitions = (nRuns == 1);

			for (int i = 0; i < nTimeSteps.Length; i++) {
				//Set the nTimeSteps
				analysis.SetTimeSteps(nTimeSteps[i]);
				string tempNTS = (nTimeSteps[i] / 1000.0).ToString();

				outParams.FileName = fileName + "_nTS" + tempNTS + "k"; 
				
				//Run the run
				RunSingleRunWrapper(nRuns);
			}
		}

		public void SingleRun()
		{
			string fileName = originalFileName;
			noRepitions = true;
			outParams.FileName = fileName;
			RunSingleRunWrapper(1);
		}

		public static object DeepClone(object obj) 
		{
			object objResult = null;
			using (MemoryStream  ms = new MemoryStream())
			{
				BinaryFormatter  bf =   new BinaryFormatter();
				bf.Serialize(ms, obj);

				ms.Position = 0;
				objResult = bf.Deserialize(ms);
			}
			return objResult;
		}

		#endregion

		#region private methods

		private void RunSingleRunWrapper(int nRuns){

			if (isMultiThreaded) {
				RunSingleRun (nRuns, isMultiThreaded);
			} else {
				RunSingleRun (nRuns);
			}
		}
		private void RunSingleRun(int nRuns)
		{
			for (int i = 0; i < nRuns; i++) {

				//Write out an output file for the individual run.  Leaves file index blank if there are no repetitions
				outParams.FileIndex = noRepitions ? "" : "_" + (i + 1);

				//set the packing: for the random run, this re-sets the packing;
				pack.SetPacking(outParams);

				//Now run the analysis
				analysis.Analyze(pack.LFibers, pack.Boundary, pack.TheGrid);

				analysis.GenerateOutput(outParams, i);

			}
		}

		private void RunSingleRun(int nRuns, bool bIsMultiThreaded)
		{
			for (int i = 0; i < nRuns; i++) {
				Packing tempPack = (Packing)DeepClone (pack);
				Analysis tempAnalysis = (Analysis)DeepClone (analysis);
				OutputParameters tempoutParams = (OutputParameters)DeepClone(outParams);

				//This stops the addition of _1 if there are no repetitions.
				RunSingleThread myThread = noRepitions ? new RunSingleThread(tempPack, tempAnalysis, tempoutParams) : new RunSingleThread (i, tempPack, tempAnalysis, tempoutParams);

				ThreadStart job = new ThreadStart(myThread.Run);
				Thread thread = new Thread(job);
				thread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
				thread.Start();
			}
		}

		private void WriteOutput(List<double []> lData, string [] labels, string fileName)
		{
			StreamWriter dataWrite = new StreamWriter(fileName);

			for (int i = 0; i < labels.Length; i++) {
				dataWrite.Write(labels[i] + ",");
			}
			dataWrite.WriteLine();

			for (int i = 0; i < lData[0].Length; i++) {

				foreach(double [] dA in lData){

					dataWrite.Write(dA[i] + ",");
				}
				dataWrite.WriteLine();
			}
			dataWrite.Close();
		}
		private double CalcAverage(double [] d)
		{
			double ave = 0;
			for (int i = 0; i < d.Length; i++) {
				ave += d[i];
			}

			return ave / (d.Length);
		}
		private double CalcStDev(double ave, double [] dArray)
		{
			double sumOfDerivation = 0;
			for (int i = 0; i < dArray.Length; i++) {

				sumOfDerivation += (dArray[i]) * (dArray[i]);
			}
			double sumOfDerivationAverage = sumOfDerivation / (dArray.Length);
			return Math.Sqrt(sumOfDerivationAverage - (ave*ave));
		}

		#endregion
	}

	public class RunSingleThread
	{
		private OutputParameters outParams;
		private Packing pack;
		private Analysis analysis;
		private int i;
		private bool noRepitions;

        public RunSingleThread (int it, Packing myPack, Analysis myAnalysis, OutputParameters myOutputParams)
		{
            outParams = myOutputParams;
			pack = myPack;
			analysis = myAnalysis;
			i = it;
			noRepitions = false;
		}
		public RunSingleThread(Packing myPack, Analysis myAnalysis, OutputParameters myOutputParams)
        {
            outParams = myOutputParams;
			pack = myPack;
			analysis = myAnalysis;
			i = 0;
			noRepitions = true;
		}

		public void Run()
		{
			outParams.FileIndex = noRepitions ? "" : "_" + (i+1);
			pack.SetPacking(outParams);
			analysis.Analyze(pack.LFibers, pack.Boundary, pack.TheGrid);
			analysis.GenerateOutput(outParams, i);

			//To save the distribution of stiffnesses
			//analysis.SaveSizingKDistribution (pack.FVolFraction, i + 1, outParams.DirName);
		}
	}
}
