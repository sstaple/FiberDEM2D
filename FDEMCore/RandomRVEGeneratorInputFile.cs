
using System;

namespace FDEMCore
{
	public class RandomRVEGeneratorInputFile : FDEMCore.InputFile
    {

		#region public members
		public RandomPack Packing;
        #endregion

        #region private members
        #endregion

        #region Constructor

        public RandomRVEGeneratorInputFile(string FileName, string DirName) : base(FileName, DirName)
		{
			//This runs the code from input file that just saves the directories
		}

		#endregion

		#region Public Methods
		protected override void ReadInputFile()
		{
			#region Initial stuff
			FiberParameters fiberParams = new FiberParameters(1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0);
			Packing = new RandomPack(0, 0, fiberParams);
			OutputParameters outParams = new OutputParameters(dirName, false, false, false, false);
			string[] temp;
			double Vf, r;
			int nRows, nRepetitions = -1;
			#endregion

			//Get the first inputs.  These are a must, and must be in order!!
			temp = NextLine();

			r = Convert.ToDouble(temp[1]);
			temp = NextLine();

			Vf = Convert.ToDouble(temp[1]);
			temp = NextLine();

			nRows = Convert.ToInt32(temp[1]);
			temp = NextLine();

			nRepetitions = Convert.ToInt32(temp[1]);
			temp = NextLine();

			//Now make the first random packing
			fiberParams = new FiberParameters(r, 1.0, 1.0, 1.0, 1.0, 0.3, 0.0);
			Packing = new RandomPack(nRows, Vf, fiberParams);

			//Now go through all of the optional arguments
			while (temp[0] != "*END")
			{
				RandomPack.ReadAndSetRandomPackingOptions(temp, Packing);
				temp = NextLine();
			}


			dataRead.Close();

            //Now just run them....
            for (int i = 0; i < nRepetitions; i++)
            {
				//Write out an output file for the individual run
				outParams.FileName = sFileName;
				//This only creates indexes if there are more then 1 repetitions.
				outParams.FileIndex = nRepetitions == 1 ? string.Empty : "_" + (i + 1);

				//set the packing: for the random run, this re-sets the packing;
				Packing.SetPacking(outParams);
			}
		}

		#endregion

		#region Private Methods
		

		#endregion
	}
}