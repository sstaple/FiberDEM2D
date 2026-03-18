/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 2/7/2013
 * Time: 8:41 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using RandomMath;
using FDEMCore.Contact;

namespace FDEMCore
{
	/// <summary>
	/// Analysis performs an analysis for a set number of steps, time steps, and strain.  Most basic, and is inherited by all of the analysis objects
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	abstract public class Analysis
	{
		#region Private Members
		protected List<Fiber> lFibers;
		protected List<FToFRelation> lSprings;
		protected CellBoundary cBoundary;
		protected Grid localGrid;
		protected ContactParameters contactParams;
		protected double dT;
		protected double initialVolume;
		protected List<double[,]> homogenizedStressForOutput;
		protected List<double[,]> homogenizedStrainForOutput;
		//private double [] kineticEnergy;
		protected List<SolidObject> lSolidObjects;
		protected CreateAndUpdateInteractions interactions; //needed this for some output, otherwise should be protected...
		protected int counter; //Just to count the number of finished contacts
		protected List<double> lKinEnergy;
		protected string sType;
		protected string sAnalysisDetails = "";
		
		protected INonContactSpringParameters nonContactSpringParams;
		protected bool hasNonContactSprings=false;
		
		protected DateTime startTime;
		public TimeSpan duration;
		//output
		protected bool bOutputAll;
		protected bool bOutputSS;
		protected bool bResumedAnalysis = false;
		
		
		#endregion
		
		#region Public Members
		public List<double[,]> HomogenizedStrain {
			get { return homogenizedStrainForOutput; }
		}
		public List<double[,]> HomogenizedStress {
			get { return homogenizedStressForOutput; }
		}
		public string SType{
			get {return sType;}
		}
		public List<FToFRelation> LSprings{
			get{return lSprings;}
		}
		#endregion
		
		#region Constructors
		protected Analysis(ContactParameters inContactParameters)
		{
			contactParams = inContactParameters;
		}
		#endregion
		
		#region Public Methods
		
		protected void CreateNewAnalysis(List<Fiber> inlFibers,  CellBoundary inCBoundary, Grid myGrid){
			
			startTime = DateTime.Now;
			
			lFibers = inlFibers;
			cBoundary = inCBoundary;
			initialVolume = cBoundary.OVolume;
			lSolidObjects = new List<SolidObject> { cBoundary};
			lSolidObjects.AddRange(lFibers);
			
			lKinEnergy = new List<double>();

			homogenizedStressForOutput = new List<double[,]> { new double[3, 3] };
			homogenizedStrainForOutput = new List<double[,]> { new double[3, 3] };

			if (!bResumedAnalysis) {
				counter = 1;
			}
			
			//this call keeps the counter from being reset with nonContactSprings change the contact definition
			if(!bResumedAnalysis || hasNonContactSprings){
				
				//Create a new "createAndUpdateInteractions" to override the contact search if there is a matrix
				//Do a temp contact in order to make forces and projections initially
				if (hasNonContactSprings && nonContactSpringParams.OverrideContactSearch) {

					//To get projected fibers on the 1st iteration
					CreateAndUpdateContact tempInteractions = new CreateAndUpdateContact(lFibers, myGrid, cBoundary, contactParams);
					tempInteractions.AssignBoundaryToGridAndAddProjectedFibers();
					foreach (SolidObject so in lSolidObjects)
					{
						so.SaveTimeStep(0);
						so.UpdateTimeStep(dT); //reset the fiber positions
					}/**/
					//Newly added
                    foreach (Fiber fiber in lFibers)
                    {
						fiber.ClearProjectedFibers();
                    }
					
					interactions = new CreateAndUpdateSprings(lFibers, myGrid, cBoundary, contactParams, nonContactSpringParams);
					
				}
				
				//Create a new "createAndUpdateInteractions" for contact search otherwise
				else{
					interactions = new CreateAndUpdateContact(lFibers, myGrid, cBoundary, contactParams);
					if (hasNonContactSprings && !nonContactSpringParams.OverrideContactSearch) {
						
						//Add sizing, which can only be done on a "CreateAndUpdateContact" object
						CreateAndUpdateContact tempCon = interactions as CreateAndUpdateContact;
						tempCon.AddSizing((SizingParameters)nonContactSpringParams);
						//Work on putting something here
					}
					//To get projections on the first iteration...
					interactions.UpdateGrid(0);
					interactions.UpdateContacts(0, dT);
					foreach (SolidObject so in lSolidObjects)
					{
						so.SaveTimeStep(0);
						so.UpdateTimeStep(dT); //reset the fiber positions
					}/**/
				}
			}
			
			localGrid = myGrid;
			
		}

		//This is for the multi-analysis stuff
		public void ResumeAnalysis(Analysis lastAnalysis){
			
			bResumedAnalysis = true;
			interactions = lastAnalysis.interactions;
			counter = lastAnalysis.counter;
		}
		public void CopyOutput(ref List<double[,]> inHStressForOutput, ref List<double[,]> inHStrainForOutput,
		                       ref List<double> inKE){
			//Remove the 1st state because it is just the added 0
			homogenizedStrainForOutput.RemoveAt(0);
			homogenizedStressForOutput.RemoveAt(0);
			
			inHStrainForOutput.AddRange(homogenizedStrainForOutput);
			inHStressForOutput.AddRange(homogenizedStressForOutput);
			inKE.AddRange(lKinEnergy);
		}
		
		abstract public void Analyze(List<Fiber> inlFibers,  CellBoundary inCBoundary, Grid myGrid);
		
		abstract public void SetTimeSteps(int nTimeS);
		/// <summary>
		/// Adds sizing on the specified step
		/// </summary>
		public void AddNonContactSprings(INonContactSpringParameters inNonContactSpringParams){
			hasNonContactSprings = true;
			nonContactSpringParams = inNonContactSpringParams;
		}
		
		public static void CriticalDamping(double M, double L, double E, double nu, ref double contactDamp, ref double globalDamp, ref double dt){
			double MStar= M * M / (2.0 * M);
			double Estar = 1d / ( (1d - nu * nu) / E + (1d - nu * nu) / E);
			double k = Math.PI* Estar * L / 4d;
			contactDamp =  Math.Sqrt(MStar*k) / MStar; //for critical damping
			globalDamp =  Math.Sqrt(M*k) / M; //for critical damping (Global)
			
			//double temp = k / M - (damp/(M)) * (damp/(M) / 4);
			double koM = k / MStar;
			dt = Math.PI / Math.Pow(koM - (contactDamp) * (contactDamp) / 4, 0.5) * 0.01; //0.001 doesn't have to be there, it should just be magnitudes smaller than the natural frequency: Added 0.5 to damping to make dt non-singular
		}

		public static double MaxDT(double minM, double maxK, double dCoeff){
			double MStar= minM * minM / (2.0 * minM);
			double Damp =  dCoeff * Math.Sqrt(MStar*maxK);
			
			double koM = maxK / MStar;
			double dt = Math.PI / Math.Pow(koM - (Damp) * (Damp) / (4.0 * MStar*MStar), 0.5) * 0.01; //0.01 doesn't have to be there, it should just be magnitudes smaller than the natural frequency
			return dt;
		}
		
		public static double Max(List<double[,]> inD){
			double max = 0;
			foreach (double[,] dArray in inD) {
				foreach (double d in dArray) {
					if (Math.Abs(d) > max) {
						max = Math.Abs(d);
					}
				}
			}
			return max;
		}
		/// <summary>
		/// Generates output
		/// </summary>
		/// <param name="outputParams"></param>
		/// <param name="i">This is the specimen number, for writing on first only</param>
		public void GenerateOutput(OutputParameters outputParams, int i){
			
			bOutputAll = outputParams.OutputAll;
			bOutputSS = outputParams.OutputStressStrain;
			
			//This just outputs everything if it is the first one and the option was marked
			if (outputParams.OutputAllOnFirst) {
				if (i == 0) {
					bOutputAll = true;
				}
				else{ bOutputAll = false;}
			}
			
			if (bOutputAll) {
				OutputFile myOutput = new OutputFile(this, outputParams.AllDataFileName, false);
			}
			if (bOutputSS) {
				OutputFile ssOutput = new OutputFile(this, outputParams.TotalFileName, true);
			}
			if (outputParams.PlotKE) {
				//PlotKinEnergy(); //Put something in here: maybe write a file with the kinetic energy as an output
			}
			
		}
		
		/// <summary>
		/// This is only to be used by the OutputFile method.  Use GenerateOutput to generate a file
		/// </summary>
		public virtual void WriteOutput(StreamWriter dataWrite, string sComment, string sCommand){

			#region Now write out Iteration Information and StressStrain Data
			if (bOutputAll) {
				//Write the general output (Homogenized Stress/Strain)

				OutputFile.WriteHeader("GeneralOutput", sComment, sCommand, dataWrite);
				dataWrite.WriteLine(sComment + sAnalysisDetails);
				dataWrite.WriteLine(sComment + duration.ToString());
				dataWrite.WriteLine(sComment);
				dataWrite.WriteLine(sComment + "Iteration, E11, E22, E33, E23, E13, E12, S11, S22, S33, S23, S13, S12");

				for (int i = 0; i < homogenizedStrainForOutput.Count; i++)
				{
					double[] tempStrain = MatrixMath.TensorToVoigtVector(homogenizedStrainForOutput[i]);
					double[] tempStress = MatrixMath.TensorToVoigtVector(homogenizedStressForOutput[i]);
					dataWrite.WriteLine(i + "," + tempStrain[0] + "," + tempStrain[1] + "," + tempStrain[2]
										+ "," + tempStrain[3] + "," + tempStrain[4] + "," + tempStrain[5]
										+ "," + tempStress[0] + "," + tempStress[1] + "," + tempStress[2]
										+ "," + tempStress[3] + "," + tempStress[4] + "," + tempStress[5]);
				}

				for (int i = 0; i < homogenizedStressForOutput.Count; i++) {
					OutputFile.WriteHeader("Iteration", sComment, sCommand, dataWrite);
					dataWrite.WriteLine(i);
					
					#region Now write out Fiber Information
					//TODO: but the headers into the types themselves.....
					dataWrite.WriteLine(sCommand + "Fibers");
					dataWrite.WriteLine(sComment + "Fiber Index, isProjected?, CenterX, CenterY, CenterZ, ZRot, Radius, Length");
					int j = 0;
					
					foreach (Fiber f in lFibers) {
						dataWrite.Write(j);
						f.WriteOutput(i, dataWrite);
						j++;
					}
					#endregion
					
					#region Now write out Contact Information
					dataWrite.WriteLine(sCommand + "Contacts");
					dataWrite.WriteLine(sComment + "p1X, p1Y, pyZ, p2X, P2Y, p2Z, Force/Stress, Type, Broken?");
					foreach (FToFRelation s in lSprings) {
						s.WriteOutput(i, dataWrite);
					}
					
					
					#endregion
					
					#region Boundary Information
					dataWrite.WriteLine(sCommand + "Boundaries");
					dataWrite.WriteLine(sComment + "CenterX, CenterY, CenterZ, NormX, NormY, NormZ");
					cBoundary.WriteOutput(i, dataWrite);
					#endregion
				}
			}
			
			#endregion
		}
		public void WriteStressStrain(StreamWriter dataWrite, string sComment, string sCommand){

			//Write the general output (Homogenized Stress/Strain)
			if (bOutputSS) {
				OutputFile.WriteHeader("GeneralOutput", sComment, sCommand, dataWrite);
				dataWrite.WriteLine(sComment + sAnalysisDetails);
				dataWrite.WriteLine(sComment + duration.ToString());
				dataWrite.WriteLine(sComment);
				dataWrite.WriteLine(sComment + "Iteration, E11, E22, E33, E23, E13, E12, S11, S22, S33, S23, S13, S12");

				for (int i = 0; i < homogenizedStrainForOutput.Count; i++)
				{
					double[] tempStrain = MatrixMath.TensorToVoigtVector(homogenizedStrainForOutput[i]);
					double[] tempStress = MatrixMath.TensorToVoigtVector(homogenizedStressForOutput[i]);

					dataWrite.WriteLine(i + "," + tempStrain[0] + "," + tempStrain[1] + "," + tempStrain[2]
										+ "," + tempStrain[3] + "," + tempStrain[4] + "," + tempStrain[5]
										+ "," + tempStress[0] + "," + tempStress[1] + "," + tempStress[2]
										+ "," + tempStress[3] + "," + tempStress[4] + "," + tempStress[5]);

				}
			}
		}

		/*Again, sizing is decommissioned.  Get this going again later....
		 * public void SaveSizingKDistribution(double Vf, int i, string dirName){

			SizingParameters mySizingParameters = nonContactSpringParams as SizingParameters;
			CreateAndUpdateContact myCon =  interactions as CreateAndUpdateContact;
			string sVf = Vf.ToString("F2");
			sVf = sVf.Replace(".", "p");
			string sd = mySizingParameters.MaxDist.ToString();
			sd = sd.Replace(".", "p");
			string sp = mySizingParameters.Probability.ToString();
			sp = sp.Replace(".", "p");

			string fn = Path.Combine(dirName, "SizingDist_vf" + sVf + "_dmax" + sd  + "_prob" + sp + "_" + i + ".csv");
			myCon.SaveSizingKDistribution(fn);
		}
		*/
		#endregion
		
		#region Private Methods
		
		protected void SingleStep(int i, int nStepsSkipped, int nStepsSkipContact, double dt){
			
			//Fist, update positions
			foreach (SolidObject so in lSolidObjects) {
				so.UpdateTimeStep(dt);
				so.UpdatePosition();
				so.UpdateRotPosition();
			}
			//Check for contacts and create contacts
			double j = ((i - 1) / (double)nStepsSkipContact);
			if ((j % 1) == 0) {
				interactions.UpdateGrid(i);
			}
			//Now, update Loads
			interactions.UpdateContacts(i, dt);
			foreach (SolidObject so in lSolidObjects) {
				so.UpdateAcceleration();
				so.UpdateRotAcceleration();
			}
			//Record all Values if it's one of the steps to be recorded
			//Store if the number is an interger
			double j1 = (i / (double)nStepsSkipped);
			if ((j1 % 1) == 0) {
				//int k = (int)j1;
				SaveTimeStep(ref counter); //try this with the current rather than the saved time step
			}
		}
		
		protected void SaveTimeStep(ref int iSaved){
			//Save Results at the time step
			
			foreach (SolidObject so in lSolidObjects) {
				so.SaveTimeStep(iSaved);
			}
			interactions.SaveTimeStep(iSaved); //Need to do the boundary first, then the contacts
			SaveCurrentStress();
			iSaved++;
			
		}

		protected void SaveCurrentStress(){
			//Save Stress/Strain at the time step
			double[,] intaractionStress = interactions.CurrentContactStress();
			double[,] fiberStress = new double[3, 3];
            foreach (Fiber fiber in lFibers)
            {
				fiberStress = MatrixMath.Add(fiber.AverageHomogenizedOutOfPlaneStress(), fiberStress);
			}

			double[,] totalHomogenizedStress = MatrixMath.Add(fiberStress, intaractionStress);
			totalHomogenizedStress = MatrixMath.ScalarMultiply(1.0 / initialVolume, totalHomogenizedStress);

			homogenizedStressForOutput.Add(totalHomogenizedStress);
			double [] str = cBoundary.CalculteStrain();
			homogenizedStrainForOutput.Add( MatrixMath.VoigtVectorToTensor(str));
		}

		protected double FindKineticEnergy(){
			double ke = 0;
			foreach (Fiber f in lFibers) {
				double temp = 0.5 * f.Mass * VectorMath.Dot(f.CurrentVelocity, f.CurrentVelocity);
				ke += temp;
			}
			return ke;
		}
		
		protected void RunLoadStep(int nTimeSteps, double dT, int inStepsToSkip, int nStepsToSkipContact)
		{
			for (int i = 1; i < nTimeSteps + 1; i++) {
				SingleStep(i, inStepsToSkip, nStepsToSkipContact, dT);
				lKinEnergy.Add(FindKineticEnergy());
			}
			lSprings = interactions.LSprings;
		}
		
		protected int RunRelaxationStep(double dT, int nMaxSteps, int nStepsPerRecording, int nStepsWithoutDamping, double dCoeff, double perKETol, double perKEBreak=0.0)
		{
			int i = 1;
			bool stopFlag = false;
			double maxKE = 0;
			double perKE;// = 1;
			double Estar = 1d / ( 2.0 * (1d - lFibers [0].Nu12 * lFibers [0].Nu12) / lFibers [0].Modulus2);
			double tempk = Math.PI* Estar * lFibers [0].OLength / 4d;
			//Save the original damping factors
			double [] GlobDampingFactors = new double[lFibers.Count];
			//Set the global damping of each fiber to 0
			for (int j = 0; j < lFibers.Count; j++) {
				GlobDampingFactors[j] = lFibers[j].GlobalDampingCoeff;
				lFibers[j].GlobalDampingCoeff = 0.0; 
			}
			
			while (!stopFlag) {
				//Run a step
				SingleStep(i, nStepsPerRecording, 1, dT);
				
				//Get kinetic energy
				lKinEnergy.Add(FindKineticEnergy());
				
				//Find the max kinetic energy
				if (lKinEnergy[lKinEnergy.Count - 1] > maxKE) {
					maxKE = lKinEnergy[lKinEnergy.Count - 1];
				}
				//Find the percent of the max KE for stopping criteria
				perKE = lKinEnergy[lKinEnergy.Count - 1] / maxKE;

				//Let interactions break if the KE is below the given threshhold (perKEBreak)
				interactions.CanSizingBreak = (perKE <= perKEBreak);

				//If past the number of steps without damping, increase the damping until it reaches the global damping
				if (i > nStepsWithoutDamping) {
					double incrementalDampingCoeff = 1.0 / dCoeff * (i - nStepsWithoutDamping);
					double maxDTForIncrementalDamping = MaxDT (lFibers [0].Mass, tempk, incrementalDampingCoeff);
					
					if (dT > maxDTForIncrementalDamping) {  //This makes sure that the incremental damping doesn't go above the original global damping
						foreach (Fiber f in lFibers) {
							f.GlobalDampingCoeff += incrementalDampingCoeff; // / lFibers[0].Mass; ///If this goes wrong, get rid of +=!!!!!!
						}
					} else {
						//dampingCoeff = 0;
					} //Just for debugging
				}

				if ((i >= nMaxSteps) || (perKE < perKETol) || maxKE.Equals(0.0)) {
					stopFlag = true;
				}
				i++;
			}
			//Reset the global damping back to the original value
			for (int j = 0; j < lFibers.Count; j++) {
				lFibers[j].GlobalDampingCoeff = GlobDampingFactors[j];
				lFibers[j].StopObject();
			}
			
			lSprings = interactions.LSprings;
			return (i - 1);
		}
		
		protected int RunRelaxationStep(double dT, int nMaxSteps,int nStepsPerRecording, double perKETol, double perKEBreak = 0.0)
		{
			int i = 1;
			bool stopFlag = false;
			double maxKE = 0;
			double perKE;// = 1;
			
			while (!stopFlag) {
				//Run a step
				SingleStep(i, nStepsPerRecording, 1, dT);
				
				//Get kinetic energy
				lKinEnergy.Add(FindKineticEnergy());
				
				//Find the max kinetic energy
				if (lKinEnergy[lKinEnergy.Count - 1] > maxKE) {
					maxKE = lKinEnergy[lKinEnergy.Count - 1];
				}
				//Find the percent of the max KE for stopping criteria
				perKE = lKinEnergy[lKinEnergy.Count - 1] / maxKE;

				//Let interactions break if the KE is below the given threshhold (perKEBreak)
				interactions.CanSizingBreak = (perKE <= perKEBreak);

				//Stopping criteria
				if ((i >= nMaxSteps) || (perKE < perKETol)) {
					stopFlag = true;
				}
				i++;
			}
			//stop the fibers
			for (int j = 0; j < lFibers.Count; j++) {
				lFibers[j].StopObject();
			}
			
			//Not sure what this is for, but I'm sure I had my reasons...
			lSprings = interactions.LSprings;
			return (i - 1);
		}
		
		public static double [] GetStrainWithConstantVolume (double strainComponent, int nstrainComponent, double Vratio)
		{
			double [] strain = new double[6];

			switch (nstrainComponent) {
					
				case 2:
					#region E33 Given
					double d3ol3 = -1d + Math.Sqrt (1d + 2.0 * strainComponent);
					double d2ol2 = Vratio / (1d + d3ol3) - 1d;
					double E22_E33 = (Math.Pow (d2ol2 + 1d, 2d) - 1d) / 2d;
					
					strain [1] = E22_E33;
					strain [2] = strainComponent;
					break;
					#endregion
				case 3:
					#region E12
					double E22_E12 = 2.0 * strainComponent * strainComponent;
					strain [1] = 1.0 * E22_E12;
					strain [5] = strainComponent;
					break;
					#endregion
					
				case 4:
					#region E13
					double E33_E13 = 2.0 * strainComponent * strainComponent;
					strain [2] = 1.0 * E33_E13;
					strain [4] = strainComponent;
					break;
					#endregion
					
				case 5:
					#region E23
					double E33_E23 = 2.0 * strainComponent * strainComponent;
					strain [2] = 1.0 * E33_E23;
					strain [3] = strainComponent;
					break;
					#endregion
					
				default:
					#region
					
					break;
					#endregion
			}
			return strain;

		}
		
		protected void SingleContVolFracLoad (int nTimeSteps, double dT, int inStepsToSkip, int nStepsToSkipContact, double startStrainComp, double totStrainComp, int nStrainComp, double Vratio)
		{
			double EIncrement = 1d / (nTimeSteps) * (totStrainComp - startStrainComp);
			//double EstrainStep = EIncrement / dT;
			double currentEcomp = startStrainComp;

			double [] prevEtot = GetStrainWithConstantVolume (currentEcomp, nStrainComp, Vratio);
			double [] currEtot;
			double [] currEstep;

			for (int i = 1; i < nTimeSteps + 1; i++) {
				//Set the strain increments
				currentEcomp += EIncrement;
				currEtot = GetStrainWithConstantVolume (currentEcomp, nStrainComp, Vratio);
				currEstep = VectorMath.Subtract (currEtot, prevEtot);

				cBoundary.StrainStep = currEstep;
				cBoundary.StrainRate = VectorMath.ScalarMultiply (1.0 / dT, currEstep);
				
				prevEtot = currEtot;

				//Load it up!
				SingleStep (i, inStepsToSkip, nStepsToSkipContact, dT);
			}
		}
		
		/* Plotting functions taken out: this sort of thing can be inserted somewhere else, maybe in a windows wrapper....
		protected void PlotKinEnergy(){
			//Plot Kinetic Energy
			List<string> labels = new List<string> { "Kinetic Energy" };
			List<double[]> lX = new List<double[]>();
			List<double[]> lY = new List<double[]>();
			double [] index = new double[lKinEnergy.Count-1];
			for (int j = 0; j < lKinEnergy.Count-1; j++) {
				index[j] = j;
			}
			lX.Add(index);
			lY.Add(lKinEnergy.ToArray());
			
			SinglePlot.SinglePlotForm S11Plot = new SinglePlot.SinglePlotForm("Kinetic energy during relaxation", "Iteration", "Kinetic Energy ()", labels, lX, lY);
			
		}

		public void PlotSpringForce (int i)
		{
			//Plot Spring Force
			if (lSprings.Count > i) {

				List<int> myIndexes = lSprings [i].LTimeSteps;

				if (myIndexes.Count > 1) {
					List<string> labels = new List<string> ();
					FToFRelation mySpring = lSprings [i];
					labels.Add ("FNorm");
					labels.Add ("FTan");
					List<double[]> lX = new List<double[]> ();
					List<double[]> lY = new List<double[]> ();
					double [] index = new double[mySpring.LNormForce.Count];
					double [] Fn = new double[mySpring.LNormForce.Count];
					double [] Ft = new double[mySpring.LNormForce.Count];
					for (int j = 0; j < mySpring.LNormForce.Count; j++) {
						index [j] = myIndexes [j];
						Fn [j] = mySpring.LNormForce [j];
						Ft [j] = mySpring.LTanForce [j];
					}
					lX.Add (index);
					lX.Add (index);
					lY.Add (Fn);
					lY.Add (Ft);
					SinglePlot.SinglePlotForm S11Plot = new SinglePlot.SinglePlotForm ("Spring Force between fibers "
					                                                                   + mySpring.Nf1 + " and "
					                                                                   + mySpring.Nf2,
					                                                                   "Iteration", "Spring Force ()", labels, lX, lY);
				}
			}
		}
		*/
		#endregion
	}
	#region Inherit directly from Analysis
	/// <summary>
	/// Performs an analysis where it is simply loaded
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class LoadStepAnalysis:Analysis
	{
		private int nTimeSteps;
		private int nStepsToSkip;
		private int nStepsToSkipContact;
		private double [] strain;
		
		public LoadStepAnalysis(double [] totStrain, int inTimeSteps, double dt, int inStepsToSkip, int inStepsToSkipContact,
		                        ContactParameters inContactParameters):base(inContactParameters){
			strain = totStrain;
			nTimeSteps = inTimeSteps;
			dT = dt;
			nStepsToSkip = inStepsToSkip;
			nStepsToSkipContact = inStepsToSkipContact;
			sType = "LoadStepAnalysis";
			sAnalysisDetails =sType + ", dt = " + dt + ", # Time Steps = " + inTimeSteps;
		}
		
		override public void Analyze(List<Fiber> inlFibers,  CellBoundary inCBoundary, Grid myGrid){
			
			double [] dE = VectorMath.ScalarMultiply(1.0 / nTimeSteps, strain);
			inCBoundary.StrainStep = dE;
			inCBoundary.StrainRate = VectorMath.ScalarMultiply (1.0 / dT, dE);
			
			base.CreateNewAnalysis(inlFibers,  inCBoundary, myGrid);
			base.RunLoadStep(nTimeSteps, dT, nStepsToSkip,  nStepsToSkipContact);
			DateTime stopTime = DateTime.Now;
			duration = stopTime - startTime;
		}
		
		override public void SetTimeSteps(int nTimeS){
			int nStepsRecorded = (int)(nTimeSteps / nStepsToSkip);
			nTimeSteps = nTimeS;
			double nss = Math.Ceiling ((double)nTimeSteps / nStepsRecorded);
			nStepsToSkip = (int)(nss);
		}
	}

	/// <summary>
	/// Performs an analysis where it is simply loaded
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class ConstantVolFractionLoadAnalysis:Analysis
	{
		private int nTimeSteps;
		private int nStepsToSkip;
		private int nStepsToSkipContact;
		private double totalStrainComponent;
		private double Vratio;
		private int nStrComponent; //2 is E33, 3 is E12
		
		public ConstantVolFractionLoadAnalysis(int inTimeSteps, double dt, int inStepsToSkip, int inStepsToSkipContact, double totStrainComponent,
		                                       int strainComponent, double volRatio,
		                                       ContactParameters inContactParameters):base(inContactParameters){
			nTimeSteps = inTimeSteps;
			dT = dt;
			nStepsToSkip = inStepsToSkip;
			nStepsToSkipContact = inStepsToSkipContact;
			totalStrainComponent = totStrainComponent;
			Vratio = volRatio;
			nStrComponent = strainComponent;
			sType = "ConstantVolFractionLoadAnalysis";
		}
		
		override public void Analyze(List<Fiber> inlFibers,  CellBoundary inCBoundary, Grid myGrid){
			
			base.CreateNewAnalysis(inlFibers,  inCBoundary, myGrid);
			double startStrain = inCBoundary.currStrain[nStrComponent]; //Was 0
			
			base.SingleContVolFracLoad(nTimeSteps, dT, nStepsToSkip, nStepsToSkipContact, startStrain, totalStrainComponent, nStrComponent, Vratio);
			lSprings = interactions.LSprings;
			DateTime stopTime = DateTime.Now;
			duration = stopTime - startTime;
		}
		
		override public void SetTimeSteps(int nTimeS){
			int nStepsRecorded = (int)(nTimeSteps / nStepsToSkip);
			nTimeSteps = nTimeS;
			nStepsToSkip = (int)(nTimeSteps / nStepsRecorded);
		}
	}

	/// <summary>
	/// Performs an analysis where it is loaded, then allowed to relax until kinetic energy is small
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class RelaxationStepAnalysis:Analysis
	{
		private int nMaxSteps;
		private int nStepsPerRecording;
		private int nStepsWithoutDamping;
		private double dCoeff;
		private double perKETol;
		private double [] strain;
		private bool bIncreaseDamping = false;
		
		public RelaxationStepAnalysis(double [] totStrain, double dt, int inMaxSteps, int inStepsPerRecording, int inStepsWithoutDamping, double DampingCoeff, double percentKETotal,
		                              ContactParameters inContactParameters):base(inContactParameters){
			Initialize(dt, inMaxSteps, inStepsWithoutDamping, inStepsPerRecording, percentKETotal, totStrain);
			bIncreaseDamping = true;
			dCoeff = DampingCoeff;
			sAnalysisDetails =sType + ", dt = " + dt + ", Max # steps = " + inMaxSteps + ", # steps without damping = " + inStepsWithoutDamping
				+ ", Damping Coeff = " + dCoeff + ", Strain = " + totStrain.ToString();
		}

		public RelaxationStepAnalysis(double [] totStrain, double dt, int inMaxSteps, int inStepsPerRecording, int inStepsWithoutDamping, double percentKETotal,
		                              ContactParameters inContactParameters):base(inContactParameters){

			Initialize(dt, inMaxSteps, inStepsWithoutDamping, inStepsPerRecording, percentKETotal, totStrain);
			dCoeff = 0.0;
			sAnalysisDetails =sType + ", dt = " + dt + ", Max # steps = " + inMaxSteps + ", # steps without damping = " + inStepsWithoutDamping
				+ ", Strain = " + totStrain.ToString();
		}

		protected void Initialize(double dt, int inMaxSteps, int inStepsWithoutDamping, int inStepsPerRecording, double percentKETotal, double [] totStrain){

			strain = totStrain;
			nMaxSteps = inMaxSteps;
			dT = dt;
			nStepsPerRecording = inStepsPerRecording;
			nStepsWithoutDamping = inStepsWithoutDamping;
			perKETol = percentKETotal;
			sType = "RelaxationStepAnalysis";
		}

		override public void Analyze(List<Fiber> inlFibers,  CellBoundary inCBoundary, Grid myGrid){
			
			double [] dE = VectorMath.ScalarMultiply(1.0 / nStepsWithoutDamping, strain);
			inCBoundary.StrainStep = dE;
			inCBoundary.StrainRate = VectorMath.ScalarMultiply (1.0 / dT, dE);
			
			base.CreateNewAnalysis(inlFibers,  inCBoundary, myGrid);

			int n;
			if (bIncreaseDamping) {
				n = RunRelaxationStep (dT, nMaxSteps, nStepsPerRecording, nStepsWithoutDamping, dCoeff, perKETol);
			} else {
				n = RunRelaxationStep(dT, nMaxSteps, nStepsPerRecording, perKETol);
			}
			DateTime stopTime = DateTime.Now;
			duration = stopTime - startTime;
		}
		
		override public void SetTimeSteps(int nTimeS){
			nStepsWithoutDamping = nTimeS;
		}
	}

	/// <summary>
	/// Performs an analysis where it is loaded, then allowed to relax until kinetic energy is small
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class QuasiStaticAnalysis:Analysis
	{
		private int nMaxSteps;
		private int nStepsWithoutDamping;
		private int nLSPerRecording;
		private double dCoeff;
		private double perKETol;
		private double perKEBreak;
		private double massScaling;
		private double stiffnessScaling;
		private int nLoadSteps;
		private double [] totalStrain;
		private bool bIncreaseDamping = false;

		/// <summary>
		/// This version Increases the damping as relaxation goes on.  Be careful using this!
		/// </summary>
		public QuasiStaticAnalysis(double inMassScaling, double inStiffnessScaling, double dt, int inLoadSteps, int nLoadStepsPerRecording,
		                           int inMaxSteps, int inStepsWithoutDamping, double DampingCoeff, double percentKETotal, double percentKEAtBreak, double [] totStrain,
		                           ContactParameters inContactParameters):base(inContactParameters){
			Initialize(inMassScaling, inStiffnessScaling, dt, inLoadSteps, nLoadStepsPerRecording, inMaxSteps,
			           inStepsWithoutDamping, percentKETotal, percentKEAtBreak, totStrain);
			bIncreaseDamping = true;
			dCoeff = DampingCoeff;
			sAnalysisDetails =sType + ", dt = " + dt + ", Load Steps = " + nLoadSteps + ", Max # steps = "
				+ inMaxSteps + ", # steps without damping = " + inStepsWithoutDamping + ", Damping Coeff = " + dCoeff +
				", Strain = " + totStrain.ToString();
		}
		/// <summary>
		/// This version doesn't have a damping increase
		/// </summary>
		/// <param name="inMassScaling">In mass scaling.</param>
		/// <param name="inStiffnessScaling">In stiffness scaling.</param>
		/// <param name="dt">time step</param>
		/// <param name="inLoadSteps">number of load steps.</param>
		/// <param name="nLoadStepsPerRecording">How often everything is saved (positions, contacts, et.).  Stress is saved every load step</param>
		/// <param name="inRelaxSteps">Number of steps it will relax for</param>
		/// <param name="inLoadingSteps">number of steps per load step that it will be loaded</param>
		/// <param name="percentKETotal">Percent Kinetic energy at which relaxation is finished</param>
		/// <param name="totStrain">Total strain vector</param>
		/// <param name="inContactParameters">Contact parameters</param>
		public QuasiStaticAnalysis(double inMassScaling, double inStiffnessScaling, double dt, int inLoadSteps,
		                           int nLoadStepsPerRecording, int inRelaxSteps, int inLoadingSteps, double percentKETotal, double percentKEAtBreak,
		                           double [] totStrain, ContactParameters inContactParameters):base(inContactParameters){
			Initialize(inMassScaling, inStiffnessScaling, dt, inLoadSteps, nLoadStepsPerRecording, inRelaxSteps, inLoadingSteps, percentKETotal,
						percentKEAtBreak, totStrain);
			sAnalysisDetails =sType + ", dt = " + dt + ", Load Steps = " + nLoadSteps + ", Max relaxation steps = "
				+ inRelaxSteps + ", # steps during loading = " + inLoadingSteps + ", Strain = "
				+ totStrain.ToString();
		}

		protected void Initialize(double inMassScaling, double inStiffnessScaling, double dt, int inLoadSteps, int nLoadStepsPerRecording,
		                          int inMaxSteps, int inStepsWithoutDamping, double percentKETotal, double percentKEBreak, double [] totStrain){
			nLSPerRecording = nLoadStepsPerRecording;
			massScaling = inMassScaling;
			stiffnessScaling = inStiffnessScaling;
			dT = dt;
			nLoadSteps = inLoadSteps;
			nMaxSteps = inMaxSteps;
			nStepsWithoutDamping = inStepsWithoutDamping;
			perKETol = percentKETotal;
			perKEBreak = percentKEBreak;
			totalStrain = totStrain;
			sType = "QuasiStaticAnalysis";
		}
		override public void Analyze(List<Fiber> inlFibers,  CellBoundary inCBoundary, Grid myGrid){
			
			base.CreateNewAnalysis(inlFibers,  inCBoundary, myGrid);
			
			double [] strainIncrement = VectorMath.ScalarMultiply(1d/(nLoadSteps*nStepsWithoutDamping), totalStrain);
			//double [] strainRate = myMath.VectorMath.ScalarMultiply(1.0 / dT, strainIncrement);
			double ogModulus = lFibers[0].Modulus2;
			double EScaled = stiffnessScaling * ogModulus;
			double oMass = lFibers[0].Mass;
			double MScaled = massScaling * oMass;
			
			for (int i = 1; i < nLoadSteps + 1; i++) {

				//Set the strain increments
				cBoundary.StrainStep = strainIncrement;
				cBoundary.StrainRate = VectorMath.ScalarMultiply(1.0 / dT, strainIncrement);

				//Keep the sizing from permanently breaking during loading/relaxation
				base.interactions.CanSizingBreak = false;
				
				//Load it up!
				base.RunLoadStep(nStepsWithoutDamping, dT, (int)(nStepsWithoutDamping*nStepsWithoutDamping*200), 1);
				//set strain increment to 0
				cBoundary.StrainStep = new double[6];
				cBoundary.StrainRate = new double[6];
				//Let it relax!!!!
				//Set the scaled E
				foreach (Fiber f in lFibers) {
					f.Modulus2 = EScaled;
					f.Mass = MScaled;
				}/**/
				if (bIncreaseDamping) {
					base.RunRelaxationStep(dT, nMaxSteps, (int)(nMaxSteps*nStepsWithoutDamping*10), nStepsWithoutDamping, dCoeff, perKETol, perKEBreak);
				}
				else{
					base.RunRelaxationStep(dT, nMaxSteps, (int)(nMaxSteps*nStepsWithoutDamping*10), perKETol, perKEBreak);
				}
				
				//Now one load step without scaled K and M to get correct results
				foreach (Fiber f in lFibers) {
					//f.StopObject(); //Get rid of any velocity left over //THIS RESULTS IN A LOWER LOAD: SOMEHOW WRONG!!! DON'T USE IT!
					f.Modulus2 = ogModulus;
					f.Mass = oMass;
				}

				//Now let the sizing break
				base.interactions.CanSizingBreak = true;
				//This is to get the effects of mass scaling out and to set the current iteration to 2
				base.RunLoadStep(2, dT, 10, 1);

				//Record the results just at certain intervals
				double j1 = (i / (double)nLSPerRecording);
				if ((j1 % 1).Equals(0.0)) {

					base.SaveTimeStep(ref counter);
				}
			}
			lSprings = interactions.LSprings;
			DateTime stopTime = DateTime.Now;
			duration = stopTime - startTime;
		}
		
		override public void SetTimeSteps(int nTimeS){
			nStepsWithoutDamping = nTimeS;
		}
	}

	/// <summary>
	/// Performs an multiple analysis at once.
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class MultiAnalysis:Analysis
	{
		private List <Analysis> lAnalysis;
		
		public MultiAnalysis(List <Analysis> inlAnalysis, ContactParameters inContactParameters):base(inContactParameters){
			lAnalysis = inlAnalysis;
			sType = "MultiAnalysis";
		}
		
		override public void Analyze(List<Fiber> inlFibers,  CellBoundary inCBoundary, Grid myGrid){
			
			//Add the noncontact springs in the appropriate analysis
			if (hasNonContactSprings) {
				lAnalysis[nonContactSpringParams.NAnalysisToCreateSprings-1].AddNonContactSprings(nonContactSpringParams);
			}
			
			CreateNewAnalysis(inlFibers, inCBoundary, myGrid);
			
			for (int i = 0; i < lAnalysis.Count; i++) {
				
				if (i != 0) {
					lAnalysis[i].ResumeAnalysis(lAnalysis[i-1]);
				}
				
				lAnalysis[i].Analyze(inlFibers, inCBoundary, myGrid);
			}
			
			//Now save stuff for the output
			lSprings = lAnalysis[lAnalysis.Count-1].LSprings;
			foreach (Analysis a in lAnalysis) {
				
				a.CopyOutput(ref homogenizedStressForOutput, ref homogenizedStrainForOutput, ref lKinEnergy);
			}
			
			DateTime stopTime = DateTime.Now;
			duration = stopTime - startTime;
		}
		
		override public void SetTimeSteps(int nTimeS){
			foreach (Analysis a in lAnalysis) {
				a.SetTimeSteps(nTimeS);
			}
		}
	}

	/// <summary>
	/// Performs an analysis where it is loaded, then allowed to relax until kinetic energy is small
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class ConsVolFracQuasiStaticAnalysis:Analysis
	{
		private int nMaxSteps;
		private int nStepsWithoutDamping;
		private int nLSPerRecording;
		private double dCoeff;
		private double perKETol;
		private double massScaling;
		private double stiffnessScaling;
		private int nLoadSteps;
		private double [] totalStrain;
		private double totalStrainComponent;
		private double Vratio;
		private int nStrComponent; //2 is E33, 3 is E12
		private bool bIncreaseDamping = true;
		
		public ConsVolFracQuasiStaticAnalysis(double inMassScaling, double inStiffnessScaling, double dt, int inLoadSteps, int nLoadStepsPerRecording,
		                                      int inMaxSteps, int inStepsWithoutDamping,
		                                      double DampingCoeff, double percentKETotal, double [] totStrain, double totStrainComponent, int strainComponent,
		                                      double volRatio, ContactParameters inContactParameters):base(inContactParameters){
			nLSPerRecording = nLoadStepsPerRecording;
			massScaling = inMassScaling;
			stiffnessScaling = inStiffnessScaling;
			dT = dt;
			nLoadSteps = inLoadSteps;
			nMaxSteps = inMaxSteps;
			nStepsWithoutDamping = inStepsWithoutDamping;
			dCoeff = DampingCoeff;
			perKETol = percentKETotal;
			totalStrain = totStrain;
			totalStrainComponent = totStrainComponent;
			Vratio = volRatio;
			nStrComponent = strainComponent;
			sType = "ConsVolFracQuasiStaticAnalysis";
			sAnalysisDetails =sType + ", dt = " + dt + ", Load Steps = " + nLoadSteps + ", Max # steps = " + inMaxSteps + ", # steps without damping = " + inStepsWithoutDamping + "Strain = " + totStrainComponent + "Strain # = " + strainComponent;
			
		}

		public ConsVolFracQuasiStaticAnalysis(double inMassScaling, double inStiffnessScaling, double dt, int inLoadSteps,
		                                      int nLoadStepsPerRecording, int inRelaxSteps, int inLoadingSteps, double percentKETotal,
		                                      double [] totStrain, double totStrainComponent, int strainComponent,
		                                      double volRatio, ContactParameters inContactParameters):
			this(inMassScaling, inStiffnessScaling, dt, inLoadSteps, nLoadStepsPerRecording,
			     inRelaxSteps, inLoadingSteps, 1000.0, percentKETotal, totStrain, totStrainComponent, strainComponent,
			     volRatio, inContactParameters){
			bIncreaseDamping = false;

		}

		override public void Analyze(List<Fiber> inlFibers,  CellBoundary inCBoundary, Grid myGrid){
			
			base.CreateNewAnalysis(inlFibers,  inCBoundary, myGrid);
			
			double currentE = inCBoundary.currStrain[nStrComponent]; //Was 0
			double EIncrement = 1d/(nLoadSteps) * (totalStrainComponent);
			double ogModulus = lFibers[0].Modulus2;
			double EScaled = stiffnessScaling * ogModulus;
			double oMass = lFibers[0].Mass;
			double MScaled = massScaling * oMass;
			
			for (int i = 1; i < nLoadSteps + 1; i++) {
				//Set the strain increments

				//Keep the sizing from permanently breaking during loading/relaxation
				base.interactions.CanSizingBreak = false;

				//Load it up!
				base.SingleContVolFracLoad(nStepsWithoutDamping, dT, (int)(nStepsWithoutDamping*10), 1,
				                           currentE, currentE + EIncrement, nStrComponent, Vratio);
				currentE += EIncrement;
				
				//set strain increment to 0
				cBoundary.StrainStep = new double[6];
				cBoundary.StrainRate = new double[6];
				
				foreach (Fiber f in lFibers) {
					f.Modulus2 = EScaled;
					f.Mass = MScaled;
				}/**/
				
				//Let it relax!!!!
				if (bIncreaseDamping) {
					base.RunRelaxationStep(dT, nMaxSteps, (int)(nMaxSteps*nStepsWithoutDamping*10), nStepsWithoutDamping, dCoeff, perKETol);
				}
				else{
					base.RunRelaxationStep(dT, nMaxSteps, (int)(nMaxSteps*nStepsWithoutDamping*10), perKETol);
				}
				
				//Now one load step without scaled K and M to get correct results and allowing to break
				foreach (Fiber f in lFibers) {
					//f.StopObject(); //Get rid of any velocity left over //THIS RESULTS IN A LOWER LOAD: SOMEHOW WRONG!!! DON'T USE IT!
					f.Modulus2 = ogModulus;
					f.Mass = oMass;
				}
				//Now let the sizing break
				base.interactions.CanSizingBreak = true;

				base.RunLoadStep(2, dT, 5, 1);

				//Record the results just at certain intervals
				double j1 = (i / (double)nLSPerRecording);
				if ((j1 % 1).Equals(0.0)) {

					base.SaveTimeStep(ref counter);
				}
			}
			lSprings = interactions.LSprings;
			DateTime stopTime = DateTime.Now;
			duration = stopTime - startTime;
		}
		
		override public void SetTimeSteps(int nTimeS){
			nStepsWithoutDamping = nTimeS;
		}
	}

	#endregion
}