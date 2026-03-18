/*
 * Created by SharpDevelop.
 * User: sstaplet
 * Date: 8/14/2009
 * Time: 11:36 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using FDEMCore.Contact;

namespace FDEMCore
{
	/// <summary>
	/// This class is responsible for the input file.  You can read the input
	/// file to start an analysis.  File should be tab-separated
	/// </summary>
	public class InputFile
	{
		#region private members
		protected int nCount = 0;
		#endregion
		
		#region public members
		public string fullFileName;
		public string sFileName;
		public string dirName;
		public StreamReader dataRead;
		public Analysis myAnalysis;
		public OutputParameters outParams;
		public int Count{
			get{
				return nCount;
			}
		}
		#endregion
		
		#region Constructor
		
		public InputFile(string FileName, string DirName)
		{
			fullFileName = Path.Combine(DirName, FileName);
			dirName = DirName;
			sFileName = System.IO.Path.GetFileNameWithoutExtension(fullFileName);
		}
		
		public void Initiate(){
			try {
				// Open a new instance of the streamreader
				dataRead = new StreamReader(fullFileName);
				ReadInputFile();
			} catch (Exception ex) {

				if (File.Exists(fullFileName))
				{
					dataRead.Close();
				}
				throw new Exception("Error:" + ex.ToString() + " at line " + nCount);
			}
		}
		#endregion
		
		#region Public Methods
		protected virtual void ReadInputFile()
		{
			#region Initial stuff
			//Tons of variables needed
			double dt = 1.0;
			double Escale = 1.0;
			double Mscale = 1.0;
			bool hasSizing = false;
			bool hasMatrix = false;
			List <string> laType = new List<string>();
			List <string []> laCons = new List<string []>();
			List <double []> ldStrain = new List<double []>();
			List <Analysis> lAnalysis = new List<Analysis>();
			string expType = "";
			string expCons = "";
			double [] maxStrain = new double[6];
            //Have to initiate these since the initiation is done in a switch loop
            INonContactSpringParameters nCSpringParams = new SizingParameters(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1);
			FiberParameters fiberParams = new FiberParameters(1.0, 1.0, 1.0, 1.0, 1.0, 1.0,0.0);
			ContactParameters conParams = new ContactParameters(1.0, 1.0, 1.0, 1.0);
			outParams = new OutputParameters(" ", false, false, false, false);
			Packing myPacking = new Packing(fiberParams);
			myAnalysis = new LoadStepAnalysis(maxStrain, 0, 0.0, 0, 0, conParams);
			Experiment myExperiment = new Experiment(myPacking, myAnalysis, outParams, sFileName);
			
			string[] temp;
			int nEndFlag = 0;
			string sSection = "None";
			double Vf = 0.0;
			int nRepetitions = 0;			
			#endregion
			
			while (nEndFlag == 0) {

				temp = NextLine();
				
				#region If the line is section header...
				if (temp[0].Contains("*")== true) { //
					sSection = temp[0];
					
					if (sSection == "*END") { //ends it if the section is "end"
						nEndFlag = 1;
					}
				}
				#endregion
				
				#region If the line is a data member...
				else { //For Data Members
					
					switch (sSection) {
							
						case "*ScaleFactors":
							#region
							Escale = Convert.ToDouble(temp[1]);
							temp = NextLine();
							Mscale = Convert.ToDouble(temp[1]);
							break;
							#endregion
						case "*Fiber":
							#region
							double E1 = Convert.ToDouble(temp[1]);
							temp = NextLine();
							double E2 = Convert.ToDouble(temp[1]);
							temp = NextLine();
							double nu12 = Convert.ToDouble(temp[1]);
							temp = NextLine();

							double nu23, G23;
							if (temp[0].Equals("nu23"))
							{
								nu23 = Convert.ToDouble(temp[1]);
								temp = NextLine();
								G23 = Convert.ToDouble(temp[1]);
								temp = NextLine();
							}
							else
							{
								nu23 = nu12;
								G23 = E2 / (2.0 * (1 + nu12));
							}
							double r = Convert.ToDouble(temp[1]);
							temp = NextLine();
							double l = Convert.ToDouble(temp[1]);
							temp = NextLine();
							double rho = Convert.ToDouble(temp[1]);
							temp = NextLine();
							double dGlobal = Convert.ToDouble(temp[1]);
							fiberParams = new FiberParameters(r, rho, l, E1, E2, nu12, nu23, G23, dGlobal);
							break;
							#endregion
						case "*Contact":
							#region
							double dCOF = Convert.ToDouble(temp[1]);
							temp = NextLine();
							double sCOF = Convert.ToDouble(temp[1]);
							temp = NextLine();
							double dCon = Convert.ToDouble(temp[1]);
							temp = NextLine();
							double KnoKt = Convert.ToDouble(temp[1]);
							conParams = new ContactParameters(sCOF, dCOF, dCon, KnoKt);
							break;
							#endregion
						case "*Sizing":
							#region
							double dMaxSiz = Convert.ToDouble(temp[1]);
							temp = NextLine();
							double maxStressSiz = Convert.ToDouble(temp[1]);
							temp = NextLine();
							double ESiz = Convert.ToDouble(temp[1]);
							temp = NextLine();
							double NuSiz = Convert.ToDouble(temp[1]);
							temp = NextLine();
							double dSiz = Convert.ToDouble(temp[1]);
							temp = NextLine();
							int nAnalysSiz = Convert.ToInt32(temp[1]);
							temp = NextLine();
							double probSiz = Convert.ToDouble(temp[1]);
							nCSpringParams = new SizingParameters(ESiz, NuSiz, dMaxSiz, maxStressSiz, probSiz, dSiz, nAnalysSiz);
							hasSizing = true;
							break;
							#endregion
						case "*Matrix":
							#region
							double EMat = Convert.ToDouble(temp[1]);
							temp = NextLine();
							double NuMat = Convert.ToDouble(temp[1]);
							temp = NextLine();
							double dMat = Convert.ToDouble(temp[1]);
							temp = NextLine();
							int nAnalysisMat = Convert.ToInt32(temp[1]);
							temp = NextLine();
							double charDistance = Convert.ToDouble(temp[1]);
							temp = NextLine();
							string modelName = temp[1];
							temp = NextLine();
							string modelConstants = temp[1];
							temp = NextLine();
							string failureCriteriaName = temp[1];
							temp = NextLine();
							string failureConstants = temp[1];
							nCSpringParams = new MatrixAssemblyParameters(EMat, NuMat, dMat, nAnalysisMat, charDistance, modelName, modelConstants,
								failureCriteriaName, failureConstants);
							hasMatrix = true;
							break;
							#endregion
						case "*RVE":
							#region
							Vf = Convert.ToDouble(temp[1]);
							
							temp = NextLine();
							string packingType = temp[1];
							
							temp = NextLine();
							int nRows = Convert.ToInt32(temp[1]);
							
							temp = NextLine();
							nRepetitions = Convert.ToInt32(temp[1]);
							
							//First, set the cell size (make it bigger if there is sizing, but sizing length is 0 default)
							//double cellSize = sizParams.MaxDist + 2.0*fiberParams.r;
							//TODO implement this somewhere else
							
							//Packing Type is last because I need to use the strain from analysis to make the packing
							switch (packingType) {
								case "Hex":
									myPacking = new AsymmetricHexagonal(nRows, fiberParams);
									break;
								case "HexVf":
									myPacking = new AsymmetricHexagonalWithVf(Vf, nRows, fiberParams);
									break;
								case "SingleRow":
									myPacking = new SingleRow(nRows, fiberParams);
									break;
                                case "SingleRowWithVf":
                                    myPacking = new SingleRowWithVf(Vf, nRows, fiberParams);
                                    break;
                                case "SymmHex":
									myPacking = new SymmetricHexagonal(nRows, fiberParams);
									break;
								case "Random":
									myPacking = new RandomPack(nRows, Vf, fiberParams);
									break;
                                case "Square":
                                    myPacking = new SquareWithVf(Vf, nRows, fiberParams);
                                    break;
								case "OutputFile":
									myPacking = new PackingFromFile(fiberParams);
									break;
								case "PackingOutputFile":
									myPacking = new PackingOutputFilePacking(fiberParams);
									break;
								default:
									throw new Exception("Incorrect Packing Type");
							}

							//Now, loop through the extra options, though they should only be good for random packing
							if (packingType == "OutputFile" || packingType == "PackingOutputFile")
							{
								temp = NextLineEvenComments();
								PackingFromFile myOutFilePacking = (PackingFromFile)myPacking;
								myOutFilePacking.filename = Path.Combine(dirName, temp[1]);

                                temp = NextLineEvenComments();
								if(temp[0].Contains("BoundaryTypes"))
								{
                                    string[] temp2 = temp[1].Split('/');
                                    myOutFilePacking.BoundaryTypes[1] = temp2[0] == "s" ? BoundaryType.Solid : BoundaryType.Periodic;
                                    myOutFilePacking.BoundaryTypes[2] = temp2[1] == "s" ? BoundaryType.Solid : BoundaryType.Periodic;
                                }
                            }
							if (packingType == "Random")
                            {
                                temp = NextLineEvenComments();
                                RandomPack myRanPack = (RandomPack)myPacking;
                                
								while (!temp[0].Contains("**"))
								{
									RandomPack.ReadAndSetRandomPackingOptions(temp, myRanPack);
									temp = NextLineEvenComments();
								}
							}


                            break;
							#endregion
						case "*Analysis":
							#region
							int nAnalysis = Convert.ToInt32(temp[1]);
							
							for (int i = 0; i < nAnalysis; i++) {
								temp = NextLine();
								laType.Add(temp[1]);
								temp = NextLine();
								laCons.Add(temp[1].Split(','));
								temp = NextLine();
								string [] stemp = temp[1].Split(',');
								ldStrain.Add(new double[6]{Convert.ToDouble(stemp[0]),
								             	Convert.ToDouble(stemp[1]),
								             	Convert.ToDouble(stemp[2]),
								             	Convert.ToDouble(stemp[3]),
								             	Convert.ToDouble(stemp[4]),
								             	Convert.ToDouble(stemp[5])});
							}
							
							break;
							#endregion
						case "*Output":
                            #region
							//This is to catch options that have been discontinued.
                            if (temp[0] == "bPlot")
							{
								throw new Exception("Please remove the 'bplot' option from the outputs: it is from an old version");
							}
							bool bAll = Convert.ToBoolean(temp[1]);
							temp = NextLine();
							bool b1st = Convert.ToBoolean(temp[1]);
							temp = NextLine();
							bool bSS = Convert.ToBoolean(temp[1]);
							temp = NextLine();
							bool bPlotKE = Convert.ToBoolean(temp[1]);
							outParams = new OutputParameters(dirName, bAll, b1st, bSS, bPlotKE);
							break;
							#endregion
						case "*Experiment":
							#region
							expType = temp[1];
							temp = NextLine();
							expCons = temp[1];
							
							break;
							#endregion
					}
				}
				#endregion
			}
			
			#region make everything and run the experiment
			double [] currStrain = new double[6];


			#region Make the analysis

			#region find max time step
			double Estar = 1d / ( 2.0 *(1d - fiberParams.nu12 * fiberParams.nu12 ) / (fiberParams.E2 * Escale) );
			double maxK = Math.PI* Estar * fiberParams.l / 4d;
			double minM = fiberParams.m * Mscale;
			double dtmin = Analysis.MaxDT(minM, maxK, conParams.ContactDamping);

			/* reinstate this when sizing is reinstated
			//Now check sizing:
			if (hasSizing) {
				double kSiz = 0.0;
				double kT = 0.0;
				double kr = 0.0;
				double Fmax=0.0;
				double area=0.0;
				//TODO This assumes all fibers are the same size.  Would have to find what the max k is for fibers not of the same size (max and min methods)
				SizingParameters sizParams = nCSpringParams as SizingParameters;
				
				Contact.FToFSizingSpring_EqArea.CalculateSizingKF(ref kSiz, ref kT, ref kr, ref Fmax, ref area, fiberParams.R * 2.0, sizParams.E, sizParams.Nu,
				                                           sizParams.MaxStress, fiberParams.R, fiberParams.R, fiberParams.l);
				double tempdt = Analysis.MaxDT(minM, kSiz, sizParams.DampCoeff);
				if (tempdt < dtmin) {
					dtmin = tempdt;
				}
			}
			*/
			//Now check matrix time steps:
			if (hasMatrix) {
				MatrixAssemblyParameters matrixParams = nCSpringParams as MatrixAssemblyParameters;
				double kSiz = 0.0;

				//Decide which model to use...
				kSiz = FToFWithMatrix.K_x(fiberParams.R, fiberParams.l, matrixParams,
					new Fiber(new double[3], fiberParams, new CellBoundary(new double[3])), new Fiber(new double[3], fiberParams, new CellBoundary(new double[3])));

				double tempdt = Analysis.MaxDT(minM, kSiz, matrixParams.DampCoeff);
				if (tempdt < dtmin) {
					dtmin = tempdt;
				}
			}
			dt = dtmin;
			#endregion

			for (int i = 0; i < laType.Count; i++) {
				
				//First get the strain in the right form
				for (int j = 0; j < 6; j++) {
					
					//this tracks the strain for multiple loading
					currStrain[j] += ldStrain[i][j];
					//this gets the max strain to make a grid over all of it.
					if (j < 3 && currStrain[j] > maxStrain[j]) {
						maxStrain[j] = currStrain[j];
					}
					if (j >= 3 && Math.Abs(currStrain[j]) > Math.Abs(maxStrain[j])) {
						maxStrain[j] = currStrain[j];
					}
				}
				
				switch (laType[i]) {
					case "LoadStepAnalysis":
						lAnalysis.Add(new LoadStepAnalysis(ldStrain[i], Convert.ToInt32(laCons[i][0]), dt,
						                                   Convert.ToInt32(laCons[i][1]),
						                                   Convert.ToInt32(laCons[i][2]), conParams));
						break;
					case "ConstantVolFractionLoadAnalysis":
						lAnalysis.Add(new ConstantVolFractionLoadAnalysis(Convert.ToInt32(laCons[i][0]),dt,
						                                                  Convert.ToInt32(laCons[i][1]),
						                                                  Convert.ToInt32(laCons[i][2]),
						                                                  Convert.ToDouble(laCons[i][3]),
						                                                  Convert.ToInt32(laCons[i][4]),
						                                                  Convert.ToDouble(laCons[i][5]),
						                                                  conParams));
						break;
					case "RelaxationStepAnalysis":
						lAnalysis.Add(new RelaxationStepAnalysis(ldStrain[i], dt,
						                                         Convert.ToInt32(laCons[i][0]),
						                                         Convert.ToInt32(laCons[i][1]),
						                                         Convert.ToInt32(laCons[i][2]),
						                                         Convert.ToDouble(laCons[i][3]),
						                                         Convert.ToDouble(laCons[i][4]), conParams));
						/*RelaxationStepAnalysis(double [] totStrain, double dt, int inMaxSteps, int inStepsPerRecording, int inStepsWithoutDamping, double DampingCoeff, double percentKETotal,
		                              ContactParameters inContactParameters)*/
			if (laCons[i].Length == 5) {
							lAnalysis.Add(new RelaxationStepAnalysis(ldStrain[i], dt,
							                                         Convert.ToInt32(laCons[i][0]),
							                                         Convert.ToInt32(laCons[i][1]),
							                                         Convert.ToInt32(laCons[i][2]),
							                                         Convert.ToDouble(laCons[i][3]),
							                                         Convert.ToDouble(laCons[i][4]), conParams));
							/*RelaxationStepAnalysis(double [] totStrain, double dt, int inMaxSteps, int inStepsPerRecording, int inStepsWithoutDamping, double DampingCoeff, double percentKETotal,
		                              ContactParameters inContactParameters)*/
						}
						else{
							lAnalysis.Add(new RelaxationStepAnalysis(ldStrain[i], dt,
							                                         Convert.ToInt32(laCons[i][0]),
							                                         Convert.ToInt32(laCons[i][1]),
							                                         Convert.ToInt32(laCons[i][2]),
							                                         Convert.ToDouble(laCons[i][4]), conParams));
							/*RelaxationStepAnalysis(double [] totStrain, double dt, int inMaxSteps, int inStepsPerRecording, int inStepsWithoutDamping, double percentKETotal,
		                              ContactParameters inContactParameters)*/
						}
						break;
					case "QuasiStaticAnalysis":
						//incremental damping with failure cutoff
						if (laCons[i].Length == 7) {
							lAnalysis.Add(new QuasiStaticAnalysis(Mscale, Escale, dt,
							                                      Convert.ToInt32(laCons[i][0]),
							                                      Convert.ToInt32(laCons[i][1]),
							                                      Convert.ToInt32(laCons[i][2]),
							                                      Convert.ToInt32(laCons[i][3]),
							                                      Convert.ToDouble(laCons[i][4]),
							                                      Convert.ToDouble(laCons[i][5]),
																  Convert.ToDouble(laCons[i][6]),
																  ldStrain[i], conParams));
							//int inLoadSteps, int nLoadStepsPerRecording, int inMaxSteps, int inStepsWithoutDamping, double DampingCoeff, double percentKETotal, double percentKEBreak
						}
						//No incremental damping, failure cutoff
						else if(laCons[i].Length == 6)
						{
							lAnalysis.Add(new QuasiStaticAnalysis(Mscale, Escale, dt,
																  Convert.ToInt32(laCons[i][0]),
																  Convert.ToInt32(laCons[i][1]),
																  Convert.ToInt32(laCons[i][2]),
																  Convert.ToInt32(laCons[i][3]),
																  Convert.ToDouble(laCons[i][4]),
																  Convert.ToDouble(laCons[i][5]),
																  ldStrain[i], conParams));
							//int inLoadSteps, int nLoadStepsPerRecording, int inRelaxSteps, int inLoadingSteps, double percentKETotal, double percentKEBreak
						}
						//No incremental damping, no failure cutoff
						else
						{
							lAnalysis.Add(new QuasiStaticAnalysis(Mscale, Escale, dt,
							                                      Convert.ToInt32(laCons[i][0]),
							                                      Convert.ToInt32(laCons[i][1]),
							                                      Convert.ToInt32(laCons[i][2]),
							                                      Convert.ToInt32(laCons[i][3]),
							                                      Convert.ToDouble(laCons[i][4]),
																  Convert.ToDouble(laCons[i][4]),
																  ldStrain[i], conParams));
							//int inLoadSteps, int nLoadStepsPerRecording, int inRelaxSteps, int inLoadingSteps, double percentKETotal,
						}
						break;
					case "ConstantVolFractionQuasiStaticAnalysis":
						if (laCons[i].Length == 9) {
							lAnalysis.Add(new ConsVolFracQuasiStaticAnalysis(Mscale, Escale, dt,
							                                                 Convert.ToInt32(laCons[i][0]),
							                                                 Convert.ToInt32(laCons[i][1]),
							                                                 Convert.ToInt32(laCons[i][2]),
							                                                 Convert.ToInt32(laCons[i][3]),
							                                                 Convert.ToDouble(laCons[i][4]),
							                                                 Convert.ToDouble(laCons[i][5]),
							                                                 ldStrain[i],
							                                                 Convert.ToDouble(laCons[i][6]),
							                                                 Convert.ToInt32(laCons[i][7]),
							                                                 Convert.ToDouble(laCons[i][8]),conParams));
						}else{
							lAnalysis.Add(new ConsVolFracQuasiStaticAnalysis(Mscale, Escale, dt,
							                                                 Convert.ToInt32(laCons[i][0]),
							                                                 Convert.ToInt32(laCons[i][1]),
							                                                 Convert.ToInt32(laCons[i][2]),
							                                                 Convert.ToInt32(laCons[i][3]),
							                                                 Convert.ToDouble(laCons[i][4]),
							                                                 ldStrain[i],
							                                                 Convert.ToDouble(laCons[i][5]),
							                                                 Convert.ToInt32(laCons[i][6]),
							                                                 Convert.ToDouble(laCons[i][7]),conParams));
						}
						break;
					case "ImplicitNumericalTangentAnalysis":

						lAnalysis.Add(new ImplicitAnalysis_NumericalTangent(Convert.ToInt32(laCons[i][0]),
																			 Convert.ToInt32(laCons[i][1]),
																			 Convert.ToDouble(laCons[i][2]),
																			 Convert.ToInt32(laCons[i][3]),
																			 ldStrain[i], conParams,
																		 Convert.ToDouble(laCons[i][4])));
						break;
					case "ImplicitNumericalTangent1stLSAnalysis":
						
						lAnalysis.Add(new ImplicitAnalysis_NumericalTangentAtLoadStep(Convert.ToInt32(laCons[i][0]),
																			 Convert.ToInt32(laCons[i][1]),
																			 Convert.ToDouble(laCons[i][2]),
																			 Convert.ToInt32(laCons[i][3]),
																			 ldStrain[i], conParams,
																			 Convert.ToDouble(laCons[i][4])));
						break;
					case "ImplicitInitialNumericalTangentAnalysis":

						lAnalysis.Add(new ImplicitAnalysis_InitialNumericalTangent(Convert.ToInt32(laCons[i][0]),
																			 Convert.ToInt32(laCons[i][1]),
																			 Convert.ToDouble(laCons[i][2]),
																			 Convert.ToInt32(laCons[i][3]),
																			 ldStrain[i], conParams,
																			 Convert.ToDouble(laCons[i][4])));
						break;
					default:
						throw new Exception("Incorrect Analysis Name");
				}
			}
			//This either makes the first analysis THE analysis, or makes a multiAnalysis
			if (lAnalysis.Count > 1) {
				myAnalysis = new MultiAnalysis(lAnalysis, conParams);
			}
			else{
				myAnalysis = lAnalysis[0];
			}
			//Set the strain in the set packing
			myPacking.Strain = maxStrain;
			#endregion

			#region Make the sizing or matrix 
			//Matke the sizing
			if(hasSizing || hasMatrix){
				myAnalysis.AddNonContactSprings(nCSpringParams);
			}
			
			#endregion

			#region setExperiment
			myExperiment = new Experiment(myPacking, myAnalysis, outParams, sFileName);
			
			switch (expType) {
				case "MinSpacingBetweenFibers":
					string[] tempMinSpac = expCons.Split(',');
					double[] minSpac = new double[tempMinSpac.Length];
					for (int i = 0; i < tempMinSpac.Length; i++)
					{
						minSpac[i] = Convert.ToDouble(tempMinSpac[i]);
					}
					myExperiment.VaryMinDistance(minSpac, nRepetitions);
					break;

				case "RVESize":
					string [] tempNRow = expCons.Split(',');
					int [] nRowArray = new int[tempNRow.Length];
					for (int i = 0; i < tempNRow.Length; i++) {
						nRowArray[i] = Convert.ToInt32(tempNRow[i]);
					}
					myExperiment.VaryRVESize(nRowArray, nRepetitions, Vf);
					break;

				case "NTimeSteps":
					string [] tempNTS = expCons.Split(',');
					int [] nTS = new int[tempNTS.Length];
					for (int i = 0; i < tempNTS.Length; i++) {
						nTS[i] = Convert.ToInt32(tempNTS[i]);
					}
					myExperiment.VaryNTimeSteps(nTS, nRepetitions);
					break;
				case "SizProb":
					if (!hasSizing) {throw new Exception("No Sizing: Change experiment");}
					string [] tempNP = expCons.Split(',');
					double [] nP = new double[tempNP.Length];
					for (int i = 0; i < tempNP.Length; i++) {
						nP[i] = Convert.ToDouble(tempNP[i]);
					}
					myExperiment.VaryProbability(nP, nCSpringParams as SizingParameters,nRepetitions);
					break;
				case "SizDamp":
					if (!hasSizing) {throw new Exception("No Sizing: Change experiment");}
					string [] tempD = expCons.Split(',');
					double [] nD = new double[tempD.Length];
					for (int i = 0; i < tempD.Length; i++) {
						nD[i] = Convert.ToDouble(tempD[i]);
					}
					myExperiment.VaryDamping(nD, nCSpringParams as SizingParameters,nRepetitions);
					break;
				case "RVESizeAndNTS":
					string [] tempNTS2 = expCons.Split(',');
					int [] nTS1 = new int[tempNTS2.Length/2];
					int [] nRowArray2 = new int[tempNTS2.Length/2];
					for (int i = 0; i < tempNTS2.Length/2; i++) {
						nRowArray2[i] = Convert.ToInt32(tempNTS2[2*i]);
						nTS1[i] = Convert.ToInt32(tempNTS2[2*i+1]);
					}
					myExperiment.VaryRVESizeAndNTS(nRowArray2, nTS1, nRepetitions);
					break;
				case "SingleRun":
					myExperiment.SingleRun();
					break;
				default:
					throw new Exception("Incorrect Experiment Type");
					//break;
			}
			#endregion
			
			dataRead.Close();
		}

        
        #endregion

        #endregion

        #region Private Methods
        protected string [] NextLine(){
			bool bComment = true;
			string [] temp = new string[1];
			//Keep reading lines until it is not a comment line
			while (bComment) {
				
				temp = NextLineEvenComments();
				
				if (!temp[0].Contains("**")) {
					bComment = false;
				}
			}
			return temp;
		}

		protected string [] NextLineEvenComments(){
			
			char[] charsToTrim = {',', '.', ' '};
			
			string tempLine = dataRead.ReadLine();
			tempLine = tempLine.Replace(" ", ""); //Get rid of empty spaces
			
			tempLine = tempLine.TrimEnd(charsToTrim);
			string[] temp = tempLine.Split('\t');
			
			return temp;
		}
		
		#endregion
	}
}
