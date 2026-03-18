/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 4/16/2015
 * Time: 1:05 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System.IO;
using System;
using FDEMCore.Contact.FailureTheories;
using System.Linq;
using System.Collections.Generic;

namespace FDEMCore
{
	/// <summary>
	/// Parameters for the output of an analysis: dir name for a file, whether all of the output (all positions and contacts)
	/// should be outputted, whether the kinetic energy should be plotted, and whether a plot should be made at analysis time
	/// for the entire analysis event.
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class OutputParameters
	{
		#region Public Member
		public string DirName;
		public string FileName;
		public string FileIndex;
		public string FileEnding;
		public bool OutputAll;
		public bool OutputStressStrain;
		public bool OutputAllOnFirst;
		public bool PlotKE;
		public string EndingForAll = "_All";
		
		public string TotalFileName{
			get { return Path.Combine(DirName, FileName + FileIndex + FileEnding); }
		}
		public string AllDataFileName{
			get {return Path.Combine(DirName, FileName + FileIndex + EndingForAll + FileEnding);}
		}
		#endregion
		
		public OutputParameters(string inDirName, bool outputAll, bool outputFirstOnly, bool outputStressStrain, bool plotKE)
		{
			DirName = inDirName;
			FileName = "EmptyFile";
			FileIndex = "_1";
			FileEnding = ".csv";
			OutputAll = outputAll;
			OutputAllOnFirst = outputFirstOnly;
			OutputStressStrain = outputStressStrain;
			PlotKE = plotKE;
		}
	}

	/// <summary>
	/// Parameters needed for contact: coefficients of friction, contact damping, and stiffness ratios
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class ContactParameters
	{
		#region Public Members
		public double SCOF;
		public double DCOF;
		public double ContactDamping;
		public double KnOverKt;
		#endregion
		public ContactParameters(double StaticCoeffOfFriction, double DynamicCoeffOfFriction,
		                         double DampingCoeff, double inKnOverKt)
		{
			SCOF = StaticCoeffOfFriction;
			DCOF = DynamicCoeffOfFriction;
			ContactDamping = DampingCoeff;
			KnOverKt = inKnOverKt;
		}
	}

	
	public interface INonContactSpringParameters{
		bool OverrideContactSearch{get;}
		int NAnalysisToCreateSprings{get;}
	}
	
	/// <summary>
	/// Parameters needed for sizing
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class SizingParameters : INonContactSpringParameters
	{
		#region Public Members
		public double E;
		public double Nu;
		public double MaxDist;
		public double MaxStress;
		public double DampCoeff;
		protected int nFirstAnalysis;
		public double Probability;
		//Constants for the iNonContactSpringParameters
		public int NAnalysisToCreateSprings{get {return nFirstAnalysis;}}
		public bool OverrideContactSearch{get {return false;}}
		#endregion
		public SizingParameters(double inE, double inNu, double inMaxDist, double inMaxStress, double inProbability, double inDampCoeff, int inNFirstAnalysis)
		{
			MaxDist = inMaxDist;
			E = inE;
			Nu = inNu;
			MaxStress = inMaxStress;
			DampCoeff = inDampCoeff;
			nFirstAnalysis = inNFirstAnalysis;
			Probability = inProbability;
		}
	}
	
	/// <summary>
	/// Parameters needed for sizing
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class MatrixAssemblyParameters : INonContactSpringParameters
	{
		#region Public Members
		public double Ep;
		public double G;
		public double E;
		public double Nu;
		public IFailureCriteria FailureTheory;
		public double DampCoeff;
		public double CharDist;
		protected int nFirstAnalysis;
		public string ModelName;
		public string modelConstants;
		//just for debugging....
		public bool dontMakeProjections = false;
		//Constants for the iNonContactSpringParameters
		public int NAnalysisToCreateSprings{get {return nFirstAnalysis;}}
		public bool OverrideContactSearch{get {return true;}}
		#endregion
		
		/// <summary>
		/// For model name: options are: "MatrixContinuumElasticFibers", "MatrixContinuum"
		/// </summary>
		public MatrixAssemblyParameters(double inE, double inNu, double inDampCoeff, int inNFirstAnalysis, double inCharDist, 
			string inModelName, string modelConstants, string FailureCriteriaName, string failureConstants)
		{
			E = inE;
			Nu = inNu;
			this.modelConstants = modelConstants;
			failureConstants += "/" + inE.ToString();
			FailureTheory = CreateFailureCriteria.CreateFailureCriteriaFromInput(FailureCriteriaName, failureConstants);
			DampCoeff = inDampCoeff;
			nFirstAnalysis = inNFirstAnalysis;
			CharDist = inCharDist;
			Ep = E/((1 + Nu)*(1 - 2*Nu));
			G = E/(2*(1 + Nu));
			ModelName = inModelName;
		}
	}
	
	/// <summary>
	/// Parameters needed for contact: coefficients of friction, contact damping, and stiffness ratios
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class FiberParameters
	{
		#region Public Members

		public virtual double R
        {
			get { return r; }
			set { r = value; }
        }
		public virtual double MaxR
		{
			get { return r; }
		}
		public virtual double MinR
		{
			get { return r; }
		}

		protected double r;
		public double m;
		public double l;
		public double E1;
		public double E2;
		public double nu12;
		public double nu23;
		public double G12;
		public double rho;
		public double globalD;
		#endregion
		public FiberParameters(FiberParameters fiberParamsToCopy)
		{
			r = fiberParamsToCopy.r;
			l = fiberParamsToCopy.l;
			rho = fiberParamsToCopy.rho;
			m = fiberParamsToCopy.m;
			E1 = fiberParamsToCopy.E1;
			E2 = fiberParamsToCopy.E2;
			nu12 = fiberParamsToCopy.nu12;
			G12 = fiberParamsToCopy.G12;
			globalD = fiberParamsToCopy.globalD;
			nu23 = fiberParamsToCopy.nu23;
		}

		public FiberParameters(double radius, double linearDensity, double length, double AxialModulus,
		                       double TransverseModulus, double PoissonsRatio, double globalDamping)
		{
			r = radius;
			l = length;
			rho = linearDensity;
			m = l * linearDensity;
			E1 = AxialModulus;
			E2 = TransverseModulus;
			nu12 = PoissonsRatio;
			G12 = E1 / (2.0 * (1.0 + nu12));
			globalD = globalDamping;
			nu12 = PoissonsRatio;
		}

		public FiberParameters(double radius, double linearDensity, double length, double AxialModulus,
							   double TransverseModulus, double PoissonsRatio12, double PoissonsRatio23, double ShearModulus12, double globalDamping)
		{
			r = radius;
			l = length;
			rho = linearDensity;
			m = l * linearDensity;
			E1 = AxialModulus;
			E2 = TransverseModulus;
			nu12 = PoissonsRatio12;
			nu23 = PoissonsRatio23;
			G12 = ShearModulus12;
			globalD = globalDamping;
		}

		public virtual void GetRVEBoundaryDimensions(out double h, out double w, int nFibers, double fiberVolumeFraction, double RVEHoW = 1.0)
        {
			double totalArea = nFibers * (Math.PI * r * r) / fiberVolumeFraction;
			h = Math.Sqrt(totalArea * RVEHoW);
			w = h / RVEHoW;
           // return Math.Sqrt(nFibers * (Math.PI * r * r) / fiberVolumeFraction); //NRows * 2 * r * 2;
		}
		public void GetAndCheckRVEBoundaryDimension(out double h, out double w, List<Fiber> lFibers, double fiberVolumeFraction, double hoverw = 1.0)
        {
			double fiberArea = 0;
            foreach (Fiber f in lFibers)
            {
				fiberArea += f.Radius * f.Radius * Math.PI;
            }
            double totalArea = fiberArea / fiberVolumeFraction;
            h = Math.Sqrt(totalArea * hoverw);
            w = h / hoverw;
        }
	}

	/// <summary>
	/// Parameters needed for contact: coefficients of friction, contact damping, and stiffness ratios
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class FiberMultipleRadiiParameters: FiberParameters
	{
		#region Public Members
		protected double[] radii;
		protected double[] percentRadii;
        Random myRanNumber;

		public override double R {
            get { return GetNextRadius(); }
        }
		public override double MaxR
        {
			get { return radii.Max(); }
        }
		public override double MinR
		{
			get { return radii.Min(); }
		}

		#endregion
		public FiberMultipleRadiiParameters(double[] fiberRadii, double[] fiberRadiiPercent, double fiberLinearDensity, double fiberLength, double fiberModulus1,
							   double fiberModulus2, double fiberPoissonsRatio, double globalDamping):
			base(fiberRadii[0], fiberLinearDensity, fiberLength, fiberModulus1,
							   fiberModulus2, fiberPoissonsRatio, globalDamping)
		{
			radii = fiberRadii;
			percentRadii = fiberRadiiPercent;
			myRanNumber =  new Random();
		}

		private double GetNextRadius()
        {
			double ranNum = (myRanNumber.NextDouble()) * 100; //get it 0 to 100 to fit with %
			double radius = radii[0];
			double sumPercent = percentRadii[0];

            for (int i = 1; i < radii.Length; i++)
            {
				if(ranNum >= sumPercent && ranNum < (sumPercent + percentRadii[i]))
                {
					radius = radii[i];
                }
				sumPercent += percentRadii[i];
            }
			return radius;
        }

        public override void GetRVEBoundaryDimensions(out double h, out double w, int nFibers, double fiberVolumeFraction, double RVEHoW = 1.0)
        {
			double areaOfFibers = 0;

            for (int i = 0; i < radii.Length; i++)
            {
				areaOfFibers += nFibers * Math.PI * radii[i] * radii[i] * percentRadii[i] / 100.0;
            }

            double totalArea = areaOfFibers / fiberVolumeFraction;
            h = Math.Sqrt(totalArea * RVEHoW);
            w = h / RVEHoW;
		}
	}
}
