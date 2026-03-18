/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 2/7/2013
 * Time: 8:45 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using DelaunatorSharp;
using FDEMCore.Contact;
using FDEMCore.Contact.Meshing;
using MathNet.Numerics.Statistics;
using RandomMath;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;


namespace FDEMCore
{
    /// <summary>
    /// Description of SetPacking.
    /// </summary>
    [SerializableAttribute] //This allows to make a deep copy fast
	public class Packing
	{
		#region Private Members

		protected List<Fiber> lFibers;
		protected FiberParameters fiberParams;
		protected CellBoundary boundary;
		protected double[] strain;
		protected Grid theGrid;
		protected string sType;
		protected double fVolFraction;
		protected int nRows;
		protected double height;
		protected double width;
		protected double[] bottomLeftBackCorner;
		protected double cellSize;
		//Save this to save input files
		protected OutputParameters outputParams;
		protected BoundaryType[] boundaryTypes = new BoundaryType[3] { BoundaryType.LinearDeformations, BoundaryType.Periodic, BoundaryType.Periodic };
        #endregion

        #region Public Members

        public CellBoundary Boundary {
			get { return boundary; }
			set { boundary = value; }
		}
		public List<Fiber> LFibers {
			get { return lFibers; }
			set { lFibers = value; }
		}
		public FiberParameters FiberParams
        {
			get { return fiberParams; }
			set { fiberParams = value; }
        }
		public double Height {
			get { return height; }
		}
		public double Width {
			get { return width; }
		}
		public Grid TheGrid {
			get { return theGrid; }
		}
		public string SType {
			get { return sType; }
		}
		public double FVolFraction {
			get { return fVolFraction; }
		}
		public int NRows {
			get { return nRows; }
			set { nRows = value;
				SetNRows(value); }
		}
		public double[] Strain {
			get { return strain; }
			set { strain = value; }
		}
		public BoundaryType[] BoundaryTypes
		{
			get { return boundaryTypes; }
			set { boundaryTypes = value; }
        }
        #endregion

        #region Constructors
        public Packing(FiberParameters inFiberParams)
		{
			fiberParams = inFiberParams;
			strain = new double[6];
			bottomLeftBackCorner = new double[3]; //Set the bottom left back corner of the boundary to 0
		}
		#endregion

		#region Public Methods
		public bool SetPacking()
        {
            
            boundary = new CellBoundary(new double[3] { fiberParams.l, width, height }, bottomLeftBackCorner, new double[6], new double[6], boundaryTypes);

			SetFibers(fiberParams, ref boundary);

			theGrid = SetGrid(2.0 * fiberParams.MaxR, fiberParams.MaxR, boundary, strain);

			double fVol = 0;
			foreach (Fiber f in lFibers) {
				fVol += Math.PI * f.Radius * f.Radius;
			}
			fVolFraction = fVol / (width * height);
			
			return true;
		}

		public bool SetPacking(OutputParameters outputParams)
        {
			this.outputParams = outputParams;
			return SetPacking();
		}
		#endregion
		
		#region Private Methods
		protected virtual void SetFibers(FiberParameters fParams, ref CellBoundary cb){
			
			lFibers = new List <Fiber>();
		}
		
		protected virtual void SetNRows(int inNRows){
			nRows = inNRows;
		}
		
		protected Grid SetGrid(double margin, double maxRadius, CellBoundary cb, double [] strain){
			//Now create the grid
			double minX = 0;
			double minY = 0;
			double maxX = 0;
			double maxY = 0;

            //Find the maximum and minimum y and x for the deformed configuration and undeformed configuration
            PointF[] defCorners = cb.FindCorners(strain);
            PointF [] origCorners = cb.FindCorners(new double [6]{0,0,0,0,0,0});
			
			foreach (PointF p in origCorners) {
				if (p.X < minX) {minX = p.X;}
				if (p.Y < minY) {minY = p.Y;}
				if (p.X > maxX) {maxX = p.X;}
				if (p.Y > maxY) {maxY = p.Y;}
			}
			foreach (PointF p in defCorners) {
				if (p.X < minX) {minX = p.X;}
				if (p.Y < minY) {minY = p.Y;}
				if (p.X > maxX) {maxX = p.X;}
				if (p.Y > maxY) {maxY = p.Y;}
			}
			//Set a margin
			minX -= margin;
			minY -= margin;
			maxX += margin;
			maxY += margin;
			
			Grid grid = new Grid(minX, minY, maxY - minY, maxX - minX, 2.0 * maxRadius);
			
			return grid;
		}
		#endregion
	}

	[SerializableAttribute] //This allows to make a deep copy fast
	public class AsymmetricHexagonal: Packing
	{
		#region Private Members
		
		private int nCols;
		private double hBetweenCenters;
		#endregion
		
		#region Constructors
		public AsymmetricHexagonal(int inRows, FiberParameters inFiberParams)
			:base(inFiberParams)
		{
			NRows = inRows; //this runs this.SetNRows(inRows);
			sType = "Hex";
		}
		//I commented this out because it needs to implement this.SetNRows to really be useful, and I didn't so it is dangerous to use it.

		//This is for a square RVE with a certain Volume Fraction
		/*public AsymmetricHexagonal(int NRows, double fiberRadius, double fiberMass, double fiberLength, double fiberModulus1, double fiberModulus2, double fiberPoissonsRatio, double FiberVolumeFraction)
			:base(fiberRadius, fiberMass, fiberLength, fiberModulus1, fiberModulus2, fiberPoissonsRatio)
		{
			nRows = NRows;
			hBetweenCenters = 2.0 * r * Math.Sin(Math.PI / 3.0);
			height = (nRows) * hBetweenCenters;
			
			nCols = (int)Math.Floor(height / (r * 2) );
			width = nCols * 2.0 * r;
			//Now set the fiber volume fraction by setting the RVE size
			int nFibersTotal = nCols * nRows;
			height = Math.Sqrt(Math.PI*r*r*nFibersTotal / FiberVolumeFraction);
			width = height;
			
		}*/
		
		#endregion
		
		#region Public Methods
		protected override void SetFibers(FiberParameters fParams, ref CellBoundary cb)
		{
			lFibers = new List<Fiber>();

			//Set Fiber Spacing
			for (int i = 0; i < nRows; i++) {
				
				double y = i * hBetweenCenters;
				
				for (int j = 0; j < nCols; j++) {
					
					//Even numbers start on the far left, odd on the far right
					double x = 2.0 * fiberParams.R * j;
					if(i % 2 != 0){
						x += fiberParams.R;
					}
					double[] location = new double[3]{fiberParams.l/2.0, x, y};
					lFibers.Add(new Fiber(location, fParams, cb));
				}
			}
		}
		
		protected override void SetNRows(int inNRows){
			nRows = inNRows;
			hBetweenCenters = 2.0 * fiberParams.R * Math.Sin(Math.PI / 3.0);
			height = (nRows) * hBetweenCenters;
			
			nCols = (int)Math.Floor(height / (fiberParams.R  * 2) );
			width = nCols * 2.0 * fiberParams.R ;
		}
		#endregion
	}

    [SerializableAttribute] //This allows to make a deep copy fast
	public class AsymmetricHexagonalWithVf: Packing
	{
		#region Private Members
		
		private int nCols;
		private double verticalDistanceBetweenCenters;
		private double horizontalDistanceBetweenCenters;
		private double Vf;
		#endregion
		
		#region Constructors
		public AsymmetricHexagonalWithVf(double inVf, int inRows, FiberParameters inFiberParams):
			base(inFiberParams)
		{
			Vf = inVf;
			NRows = inRows; //this runs this.SetNRows(inRows);
			sType = "HexVf";
		}

		#endregion

		#region Public Methods
		protected override void SetFibers(FiberParameters fParams, ref CellBoundary cb)
		{
			lFibers = new List<Fiber>();

			//Set Fiber Spacing
			for (int i = 0; i < nRows; i++) {
				
				double y = i * verticalDistanceBetweenCenters;
				
				for (int j = 0; j < nCols; j++) {
					
					//Even numbers start on the far left, odd on the far right (move a bit on the right to make sure that all rows are touching the boundary
					double x = horizontalDistanceBetweenCenters * j + fiberParams.R / 3.0;
					if(i % 2 != 0){
						x += horizontalDistanceBetweenCenters/2.0;
					}
					double[] location = new double[3] { fiberParams.l / 2.0, x, y };
					lFibers.Add(new Fiber(location, fParams, cb));
				}
			}
		}
		
		protected override void SetNRows(int inNRows){
			nRows = inNRows;
			verticalDistanceBetweenCenters = Math.Sqrt(Math.PI * fiberParams.R * fiberParams.R * Math.Sin(Math.PI / 3.0) / Vf);
			horizontalDistanceBetweenCenters = verticalDistanceBetweenCenters / Math.Sin(Math.PI / 3.0);
			
			height = (nRows) * verticalDistanceBetweenCenters;
			
			nCols = nRows;
			
			width = nCols * horizontalDistanceBetweenCenters;
			
		}
		#endregion
	}

    public class SquareWithVf : Packing
    {
        #region Private Members

        private int nCols;
        private double Vf;
        private double hBetweenCenters;
        #endregion

        #region Constructors
        public SquareWithVf(double inVf, int inRows, FiberParameters inFiberParams) :
            base(inFiberParams)
        {
            Vf = inVf;
            NRows = inRows; //this runs this.SetNRows(inRows);
            sType = "SquareVf";
        }

		#endregion

		#region Public Methods
		protected override void SetFibers(FiberParameters fParams, ref CellBoundary cb)
		{
			lFibers = new List<Fiber>();

			//Set Fiber Spacing
			for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
					double[] location = new double[3] { fParams.l / 2.0, i * hBetweenCenters, j * hBetweenCenters };
					lFibers.Add(new Fiber(location, fParams, cb));
				}
            }
        }

        protected override void SetNRows(int inNRows)
        {
            nRows = inNRows;
            hBetweenCenters = Math.Sqrt(Math.PI * fiberParams.R * fiberParams.R / Vf);

            height = (nRows) * hBetweenCenters;

            nCols = nRows;

            width = nCols * hBetweenCenters;

        }
        #endregion
    }

    [SerializableAttribute] //This allows to make a deep copy fast
	public class SingleRow: Packing
	{
		#region Private Members
		
		private int nCols;
		private double hBetweenCenters;
		#endregion
		
		#region Constructors
		public SingleRow(int nFibers, FiberParameters inFiberParams):
			base(inFiberParams)
		{
			NRows = nFibers; //this runs this.SetNRows(inRows);
			sType = "SingleRow";
		}
		#endregion

		#region Public Methods
		protected override void SetFibers(FiberParameters fParams, ref CellBoundary cb)
		{
			lFibers = new List<Fiber>();

			//Set Fiber Spacing

			for (int j = 0; j < nCols; j++) {

				double x = 2.0 * fiberParams.R * j;
				
				double[] location = new double[3] { fParams.l / 2.0, x, fParams.R };
				lFibers.Add(new Fiber(location, fParams, cb));
			}
		}
		
		protected override void SetNRows(int inNRows){
			nRows = 1;
			hBetweenCenters = 2.0 * fiberParams.R * Math.Sin(Math.PI / 3.0);
			height = (6) * hBetweenCenters;
			
			nCols = inNRows;
			width = nCols * 2.0 * fiberParams.R;
		}
		#endregion
	}

    [SerializableAttribute] //This allows to make a deep copy fast
    public class SingleRowWithVf : Packing
    {
        #region Private Members

        private int nCols;
        private double hBetweenCenters;
        private double Vf;
        #endregion

        #region Constructors
        public SingleRowWithVf(double inVf, int nFibers,  FiberParameters inFiberParams) :
            base(inFiberParams)
        {
            Vf = inVf;
            NRows = nFibers; //this runs this.SetNRows(inRows);
            sType = "SingleRowWifhVf";
        }
		#endregion

		#region Public Methods
		protected override void SetFibers(FiberParameters fParams, ref CellBoundary cb)
		{
			lFibers = new List<Fiber>();

			//Set Fiber Spacing

			for (int j = 0; j < nCols; j++)
            {   
				double[] location = new double[3] { fParams.l / 2.0, j * hBetweenCenters, fParams.R * 2.0 };
				lFibers.Add(new Fiber(location, fParams, cb));
			}
        }

        protected override void SetNRows(int inNRows)
        {
            nRows = 1;

            hBetweenCenters = Math.Sqrt(Math.PI * fiberParams.R * fiberParams.R / Vf);

            height = fiberParams.R * 4;

            nCols = inNRows;

            width = nCols * hBetweenCenters;

        }
        #endregion
    }
    [SerializableAttribute] //This allows to make a deep copy fast
	
    public class SymmetricHexagonal: Packing
	{
		#region Private Members
		private int nCols;
		private double hBetweenCenters;
		#endregion
		
		#region Constructors
		public SymmetricHexagonal(int inRows, FiberParameters inFiberParams):
			base(inFiberParams)
		{
			NRows = inRows; //this runs this.SetNRows(inRows);
			sType = "SymmHex";
		}
		#endregion

		#region Public Methods
		protected override void SetFibers(FiberParameters fParams, ref CellBoundary cb)
		{
			lFibers = new List<Fiber>();

			//Set Fiber Spacing
			for (int i = 0; i < nRows; i++) {
				
				double y = fiberParams.R + i * hBetweenCenters;
				
				int tempNCol = nCols;
				if(i % 2 != 0){tempNCol -= 1;}
				
				for (int j = 0; j < tempNCol; j++) {
					
					//Even numbers start on the far left, odd on the far right
					double x = 2.0 * fiberParams.R * j + fiberParams.R;
					if(i % 2 != 0){
						x += fiberParams.R;
					}
					double[] location = new double[3] { fParams.l / 2.0, x, y };
					lFibers.Add(new Fiber(location, fParams, cb));
				}
			}
		}
		
		protected override void SetNRows(int inNRows){
			nRows = inNRows;
			hBetweenCenters = 2.0 * fiberParams.R * Math.Sin(Math.PI / 3.0);
			height = 2.0 * fiberParams.R + (nRows - 1.0) * hBetweenCenters;
			nCols = nRows;
			width = nCols * 2.0 * fiberParams.R;
		}
		#endregion
	}

	[SerializableAttribute] //This allows to make a deep copy fast
	public class RandomPack: Packing
	{
		#region Private Members
		protected int nFibers;
		protected List <Fiber> lRanFibers;
		protected double fDensity;
		#endregion
		
		#region Public Members
		public double minSpacingBetweenFibers;
		public int nFibersPerSquare = 1;
		public double squareMargin;
		public bool saveResults = false;
		public bool saveFinalPositions = false;
		public bool saveFinalPositionsWithoutProjections = false;
        public double RVEHOverW = 1.0;
        public double contactDampingCoeff = 0.1;
		public double globalDampingCoeff = 1.0;
		public double increasingDampingCoeff = 0.001;
		public double perKETol = 0.01;
		public int nMaxSteps = 3000;
		public int nUndampedSteps = 500;
		public bool isNRowsActuallyNFibers = false;
		public bool saveVfStatistics = false;
		public bool doNotAllowOverlaps = false;
		public bool saveConnectionPlot = false;
        public double minSpacingBetweenFiberAndSolidBoundary = 0.0;


        #endregion

        #region Constructors
        public RandomPack(int inRows, double fiberVolumeFraction, FiberParameters inFiberParams):
			base(inFiberParams)
		{
			fVolFraction = fiberVolumeFraction;
			NRows = inRows; //this runs this.SetNRows(inRows);
			sType = "Random";
			squareMargin = 0.75;
		}

		#endregion

		#region Public Methods
		protected override void SetFibers(FiberParameters fParams, ref CellBoundary cb)
		{
			lFibers = new List<Fiber>();

			int nRelaxationSteps = 1;
			int nDiv = nFibersPerSquare < Math.Sqrt(nFibers) ?  (int)(Math.Ceiling(Math.Sqrt(nFibers))/nFibersPerSquare) : 1;
			double [] z = new double[6];

            //reset boundary
            fiberParams.GetRVEBoundaryDimensions(out height, out width, nFibers, fVolFraction, RVEHOverW);

            lFibers = SetRandomFibers(nDiv, cb, nFibers, height, width, squareMargin, fiberParams, out double marginSize);

			//re-calculate the initial size of the boundary, then re-make the boundary.  Do this because when multiple radii are in play, the boundary needs to be adjusted a little
			//To make the fiber volume fraction correct.
			fParams.GetAndCheckRVEBoundaryDimension(out height, out width, lFibers, fVolFraction, RVEHOverW);
			
			cb = new CellBoundary(new double[3] { fiberParams.l, width, height }, bottomLeftBackCorner, boundaryTypes : boundaryTypes);

			Grid tempGridForRelaxation = base.SetGrid(2 * fParams.MaxR, fParams.MaxR, cb, z);

			for (int i = 0; i < nRelaxationSteps; i++) {
				RelaxationStep(ref lFibers, tempGridForRelaxation, cb);
			}
			
		}

		protected override void SetNRows(int inNRows){

			nFibers = isNRowsActuallyNFibers ? inNRows : inNRows * inNRows;
			fiberParams.GetRVEBoundaryDimensions(out height, out width, nFibers, fVolFraction, RVEHOverW);
			
		}
		//Run Relaxation Analysis
		//Assign lStoppedFibers postions to RanFibers
		private void RelaxationStep(ref List<Fiber> inlRandFibers, Grid inGrid, CellBoundary inCellBoundary)
		{
			int nSS = (int)(nMaxSteps/100);
			double EoM = 1.0;
			
			double M = 1;
			double En2 = M*EoM;
			double inNu = 0.3;
			double DTscaled;
			double Estar = 1d / ( 2.0 *(1d - inNu * inNu ) / M*EoM );
			double maxK = Math.PI* Estar * this.fiberParams.l / 4d;
			DTscaled = Analysis.MaxDT(M, maxK, globalDampingCoeff);

			ContactParameters cParams = new ContactParameters(0, 0, contactDampingCoeff,1.0);
			
			List <Fiber> lStoppedFibers = new List<Fiber>();

			//Assign RanFibers postions to lStoppedFibers and make cell boundary copy
			//Do this so that the packing properties are not the actual properties
			
            CellBoundary cb = new CellBoundary(VectorMath.DeepCopy(inCellBoundary.ODimensions), boundaryTypes:boundaryTypes, 
				minSpacingBetweenBoundary:minSpacingBetweenFiberAndSolidBoundary);

            //Bring in boundaries if they are solid boundaries


            //.This is where we set the min spacing between fibers too.
            foreach (Fiber f in inlRandFibers) {
				lStoppedFibers.Add(new Fiber(VectorMath.DeepCopy(f.CurrentPosition), f.Radius + minSpacingBetweenFibers / 2.0,
					En2, En2, inNu, inNu, En2 / (2.0 * (1 + inNu)), f.OLength, M, globalDampingCoeff, cb));
			}
			
			//Now, create a new analysis to relax the fibers then run it
			RelaxationStepAnalysis preLoadAnalysis = new RelaxationStepAnalysis(new double[6], DTscaled, nMaxSteps, nSS, nUndampedSteps,
			                                                                    increasingDampingCoeff, perKETol, cParams);
			preLoadAnalysis.Analyze(lStoppedFibers, cb, inGrid);

            //Check that there are no overlaps if that is one of the options
            if (doNotAllowOverlaps)
            {
				//Check if there are overlaps
				int nInitialOverlaps = 0;
                foreach (FToFRelation spring in preLoadAnalysis.LSprings)
                {
					//Only makes this "true" if there is a current contact
					nInitialOverlaps = spring.CurrentlyInContact(minSpacingBetweenFibers) ? nInitialOverlaps + 1 : nInitialOverlaps;
					
                }

				if (nInitialOverlaps > 0)
                {
                    //Stop the fibers
                    foreach (Fiber fiber in lStoppedFibers)
                    {
						fiber.StopObject();
                    }
					//Run another relaxation with same parameters
					preLoadAnalysis.Analyze(lStoppedFibers, cb, inGrid);

					//Check again: if there are still overlaps, return an error
					//Count # of overlaps
					int nOverlaps = 0;
					foreach (FToFRelation spring in preLoadAnalysis.LSprings)
					{
						nOverlaps = spring.CurrentlyInContact(minSpacingBetweenFibers) ? nOverlaps + 1 : nOverlaps;
					}
                    if (nOverlaps > 0)
                    {
						throw new Exception($"Had {nInitialOverlaps} overlap(s) initially, and still has {nOverlaps} overlap(s) after re-run of relaxation analysis");
					}
				}
				
            }


			//now set the position of the original fibers, which do not have the initial overlap
			for (int i = 0; i < inlRandFibers.Count; i++)
			{
				double[] currPosition = VectorMath.DeepCopy(lStoppedFibers[i].CurrentPosition);
				inlRandFibers[i].CurrentPosition = currPosition;
				//don't forget to set the initial position
				inlRandFibers[i].Position[0] = currPosition;
				//And add the projected fibers for output
				inlRandFibers[i].ProjectedFibers = lStoppedFibers[i].ProjectedFibers;
				inlRandFibers[i].HasProjectedFibers = lStoppedFibers[i].HasProjectedFibers;

			}

			if (saveResults || saveFinalPositions || saveFinalPositionsWithoutProjections || saveVfStatistics)
			{
                OutputParameters myOutput = new OutputParameters(outputParams.DirName, saveResults, false, false, false)
                {
                    FileIndex = outputParams.FileIndex,

                    //string fileIndex = String.Copy(outputParams.FileIndex);
                    //int fileInd = int.Parse(fileIndex.Remove(0,1));

                    FileName = outputParams.FileName + "_Pack"
                };
                preLoadAnalysis.GenerateOutput(myOutput, 0);
				TimeSpan duration = preLoadAnalysis.duration;

				if (saveFinalPositions || saveFinalPositionsWithoutProjections || saveVfStatistics)
				{
					//write out the final fibers becasue we want the original r.
					WriteFinalFiberPositions(myOutput.TotalFileName, inlRandFibers, boundary, saveFinalPositions, saveVfStatistics, saveConnectionPlot, duration);
				}
			}

			//Now erase the projected fibers
			foreach (Fiber f in inlRandFibers)
			{
				f.UpdateTimeStep(0.001); //reset the fiber positions
			}/**/
			//preLoadAnalysis.GenerateOutput(new OutputParameters("E:\\Google Drive\\IFAM\\Projects\\FDEM\\CompositeModel\\RandomFiberGeneration\\test\\ranResults", true, false, false, showResults, showResults), 1);
			//preLoadAnalysis.GenerateOutput(new OutputParameters("E:\\Google Drive\\IFAM\\Projects\\FDEM\\CompositeModel\\SagarComparison\\Margin\\13f_0.55V_0.75M", true, true, false, false, false), 1);
			//PlotStresses((int)(nUndampedSteps/nSS), DTscaled, preLoadAnalysis);
			//preLoadAnalysis.PlotKE();

		}

		public static void ReadAndSetRandomPackingOptions(string[] temp, RandomPack myRanPack)
		{
				switch (temp[0])
				{
                case "MinSpacing":
                    myRanPack.minSpacingBetweenFibers = Convert.ToDouble(temp[1]);
                    break;
                case "FPerSquare":
                    myRanPack.nFibersPerSquare = Convert.ToInt32(temp[1]);
                    break;
                case "Margin":
                    myRanPack.squareMargin = Convert.ToDouble(temp[1]);
                    break;
                case "SaveResults":
                    myRanPack.saveResults = Convert.ToBoolean(temp[1]);
                    break;
                case "SaveFinalPositionsWithoutProjections":
                    myRanPack.saveFinalPositionsWithoutProjections = Convert.ToBoolean(temp[1]);
                    break;
                case "SaveFinalPositions":
                    myRanPack.saveFinalPositions = Convert.ToBoolean(temp[1]);
                    break;
				case "SaveVfStatistics":
					myRanPack.saveVfStatistics = Convert.ToBoolean(temp[1]);
					break;
				case "ConDamp":
                    myRanPack.contactDampingCoeff = Convert.ToDouble(temp[1]);
                    break;
                case "GlobDamp":
                    myRanPack.globalDampingCoeff = Convert.ToDouble(temp[1]);
                    break;
                case "IncrDamp":
                    myRanPack.increasingDampingCoeff = Convert.ToDouble(temp[1]);
                    break;
				case "RVEHoverW":
					myRanPack.RVEHOverW = Convert.ToDouble(temp[1]);
                    myRanPack.SetNRows(myRanPack.NRows);
                    break;
				case "MaxSteps":
                    myRanPack.nMaxSteps = Convert.ToInt32(temp[1]);
                    break;
                case "UndampedSteps":
                    myRanPack.nUndampedSteps = Convert.ToInt32(temp[1]);
                    break;
                case "IsNRowsActuallyNFibers":
                    myRanPack.isNRowsActuallyNFibers = Convert.ToBoolean(temp[1]);
                    myRanPack.SetNRows(myRanPack.NRows);
                    break;
				case "MultipleRadii":
					temp = temp[1].Split(',');
					double[] radii = Array.ConvertAll(temp[0].Split('/'), Double.Parse);
					double[] percent = Array.ConvertAll(temp[1].Split('/'), Double.Parse);
					myRanPack.fiberParams = new FiberMultipleRadiiParameters(radii, percent, myRanPack.fiberParams.rho, myRanPack.fiberParams.l,
						myRanPack.fiberParams.E1, myRanPack.fiberParams.E2, myRanPack.fiberParams.nu12, myRanPack.fiberParams.globalD);
					myRanPack.SetNRows(myRanPack.NRows);

					break;
				case "DoNotAllowOverlaps":
					myRanPack.doNotAllowOverlaps = Convert.ToBoolean(temp[1]);
					break;
				case "SaveConnectionPlot":
					myRanPack.saveConnectionPlot = Convert.ToBoolean(temp[1]);
					break;
                case "perKECutoff":
                    myRanPack.perKETol = Convert.ToDouble(temp[1]);
                    break;
                case "BoundaryTypes":
					string[] temp2 = temp[1].Split('/');
					myRanPack.boundaryTypes[1] =  temp2[0] == "s" ? BoundaryType.Solid : BoundaryType.Periodic;
					myRanPack.boundaryTypes[2] = temp2[1] == "s" ? BoundaryType.Solid : BoundaryType.Periodic; 
                    break;
                case "MinBoundarySpacing":
                    myRanPack.minSpacingBetweenFiberAndSolidBoundary = Convert.ToDouble(temp[1]);
                    break;
                default:
                    
					throw new Exception("Could not read: " + temp[0]);
			}
		}
		
		//This just seeds the fibers, and is used for the RVEGenerator, to populate the GUI
		public static List<Fiber> SeedFibers(double nDiv, double squareMargin, int nFibers, FiberParameters fiberParams, double volumeFraction, out CellBoundary cBoundary, 
			out double marginSize)
        {
			fiberParams.GetRVEBoundaryDimensions(out double initialH, out double initialW, nFibers, volumeFraction);
			cBoundary = new CellBoundary(new double[3], new double[] { fiberParams.l, initialW, initialH });
			List<Fiber> fibers = SetRandomFibers(nDiv, cBoundary, nFibers, initialH, initialW, squareMargin, fiberParams, out marginSize);
			//re-calculate the initial size of the boundary, then re-make the boundary.  Do this because when multiple radii are in play, the boundary needs to be adjusted a little
			//To make the fiber volume fraction correct.
			fiberParams.GetAndCheckRVEBoundaryDimension(out initialH, out initialW, fibers, volumeFraction);
			cBoundary = new CellBoundary(new double[3], new double[] { fiberParams.l, initialW, initialH });
			return fibers;

		}
		#endregion

		#region Private Methods
		private static List<Fiber> SetRandomFibers(double nDiv, CellBoundary cb, int nFibers, double initialH, double initialW, double squareMargin, FiberParameters fiberParams,
			out double marginSize)
		{
			
			List<Fiber> lInitialFibers = new List<Fiber>();
			//int nDiv = (int)Math.Sqrt(nFibers) / 2; //This makes it 4 fibers per box, which keeps it from being so uniform
			int nFperD = (int)(nFibers / (nDiv*nDiv));
			double xMin = 0;//r;
			double xMax = initialW;// + xMin;
			double yMin = 0;
			double yMax = initialH;
            double dx = (xMax - xMin) / nDiv;
			double dy = (yMax - yMin) / nDiv;
            System.Random myRanNumber = new System.Random();

			marginSize = squareMargin * fiberParams.MaxR;
			
			
			for (int i = 0; i < nDiv; i++) {
				for (int j = 0; j < nDiv; j++) {
					
					lInitialFibers.AddRange(SetSquare(i*dx + marginSize, (i+1)*dx - marginSize, j*dy + marginSize, (j+1)*dy - marginSize, nFperD, myRanNumber, cb, fiberParams));
				}
			}
			//This is for any leftout fibers????
			if (nFperD * (nDiv*nDiv)  < nFibers) {
				lInitialFibers.AddRange(SetSquare(xMin, xMax, yMin, yMax, (int)(nFibers - nFperD * (nDiv*nDiv)), myRanNumber, cb, fiberParams));
			}
			return lInitialFibers;
		}
		
		private static List<Fiber> SetSquare(double xLeft, double xRight,double yBottom,double yTop, int nFib, Random myRanNumber, CellBoundary cb, FiberParameters fiberParams)
		{
			
			List<Fiber> lInitialFibers = new List<Fiber>();
			
			for (int i = 0; i < nFib; i++) {
				double ranx = xLeft + myRanNumber.NextDouble() * (xRight - xLeft);
				double rany = yBottom + myRanNumber.NextDouble() * (yTop - yBottom);
				//This is where we set the fibers!
				FiberParameters tempFP = new FiberParameters(fiberParams.R, 2.0*fiberParams.rho, fiberParams.l,
				                                             fiberParams.E1/2.0, fiberParams.E2/2.0,
				                                             fiberParams.nu12, fiberParams.globalD);
				lInitialFibers.Add(new Fiber(new double[3]{fiberParams.l/2.0, ranx, rany}, tempFP, cb));
			}
			return lInitialFibers;
		}
		
		public static void WriteFinalFiberPositions(string fileName, List <Fiber> fibers, CellBoundary boundary, bool includeProjections, bool includeVfStatistics, bool saveConnectionPlot, TimeSpan duration)
		{
			StreamWriter dataWrite = new StreamWriter(fileName);
			dataWrite.WriteLine("Y, Z, Radius");
			foreach (Fiber f in fibers)
			{
				dataWrite.WriteLine(f.CurrentPosition[1] + "," + f.CurrentPosition[2] + "," + f.Radius);
			}
            if (includeProjections)
            {

				foreach (Fiber f in fibers)
				{
                    if (f.HasProjectedFibers)
                    {
                        foreach (ProjectedFiber projectedFiber in f.ProjectedFibers)
						{
							dataWrite.WriteLine(projectedFiber.CurrentPosition[1] + "," + projectedFiber.CurrentPosition[2] + "," + f.Radius);
						}
                    }
				}
			}
			//now write the boundary...
			dataWrite.WriteLine();

			dataWrite.WriteLine("Boundary: Bottom/Left Corner X, Y, Z, length X, length Y, length Z");

			boundary.WriteBackLeftCornerAndDimensions(dataWrite);

			dataWrite.WriteLine();
			dataWrite.WriteLine("*Runtime (Sec)");
			dataWrite.WriteLine(duration.TotalSeconds.ToString());

            if (includeVfStatistics)
			{
				dataWrite.WriteLine();
				CalculateLocalFiberVolumeFraction(fibers, boundary, out double median, out double mean, out double IQR, out double kurtosis, saveConnectionPlot, fileName);

				dataWrite.WriteLine("Local Fiber Volume Fraction:, Median, Mean,IQR, Kurtosis");
				dataWrite.WriteLine("*VfStatistics, {0}, {1}, {2}", median, mean, IQR, kurtosis);
			}

			dataWrite.Close();
		}
		
		public static void CalculateLocalFiberVolumeFraction(List<Fiber> fibers, CellBoundary cb, out double median, out double mean, out double IQR, out double kurtosis, bool saveConnectionPlot, string fileName)
        {
			//Put fiber centers and projection into an array of points

			MyPoint[] myPts = FDEMMatrixMeshing.AddAllFiberProjectionsToPoints(fibers, cb.Walls);

			//Now do the triangulation, and extract all of the pairs
			var triangulation = new Delaunator(myPts);

            #region Save a connection plot if needed
            if (saveConnectionPlot) { 
			List<int[]> pairs = FDEMMatrixMeshing.ExtractIndicesOfPairs(triangulation);
			string fileNameWOExtension = Path.GetFileNameWithoutExtension(fileName);
                //FToFWithMatrix.OutputAllTriangulation(myPts, pairs, outputParams.DirName + "//" + outputParams.FileName + "_Connections" + outputParams.FileIndex + ".csv");
                FDEMMatrixMeshing.OutputAllTriangulation(myPts, pairs, fileNameWOExtension + "_Connections.csv");
			}
			#endregion


			//Initiate local fiber volume fractions (assuming constant areas!)
			List<double> normalizedAreas = new List<double>();
			double fiberArea = Math.PI * fibers[0].Radius * fibers[0].Radius / 2.0;

			for (int i = 0; i < triangulation.Triangles.Length; i+=3)
            {
				//If all fibers are in the volume, then add the volume
                if (triangulation.Triangles[i] < fibers.Count && triangulation.Triangles[i + 1] < fibers.Count && triangulation.Triangles[i + 2] < fibers.Count)
				{
					double[] pf1 = fibers[triangulation.Triangles[i]].CurrentPosition;
					double[] pf2 = fibers[triangulation.Triangles[i + 1]].CurrentPosition;
					double[] pf3 = fibers[triangulation.Triangles[i + 2]].CurrentPosition;

					double area = Math.Abs(0.5 * (pf1[1] * (pf2[2] - pf3[2]) + pf2[1] * (pf3[2] - pf1[2]) + pf3[1] * (pf1[2] - pf2[2])));
					//Can re-write this to figure out the actual volume fraction of each fiber, but TODO Later...
					normalizedAreas.Add(fiberArea / area);
				}
			}
			//Now get the statistics of the packing triangles
			double[] normAreas = normalizedAreas.ToArray();
			IQR = Statistics.InterquartileRange(normAreas);
			median = Statistics.Median(normAreas);
			mean = Statistics.Mean(normAreas);
			kurtosis = Statistics.Kurtosis(normAreas);
		}
		#endregion
	}

	[SerializableAttribute] //This allows to make a deep copy fast
	public class PackingFromFile : Packing
	{
		#region Private Members
		public string filename;
		protected StreamReader dataRead;
		protected int nCount;
		protected List<double[]> lFLoc;
		#endregion

		#region Public Members

		#endregion

		#region Constructors
		public PackingFromFile(FiberParameters inFiberParams) :
			base(inFiberParams)
		{
			sType = "FilePacking";
		}

		#endregion

		#region Public Methods
		protected override void SetFibers(FiberParameters fParams, ref CellBoundary cb)
		{
			try
			{
				// Open a new instance of the streamreader
				dataRead = new StreamReader(filename);
				ReadFileForFiberLocations(fParams, ref cb);
			}
			catch (Exception ex)
			{

				if (File.Exists(filename))
				{
					dataRead.Close();
				}
				throw new Exception("Error:" + ex.ToString() + " at line " + nCount);
			}
			//RandomPack.CalculateLocalFiberVolumeFraction(LFibers, cb, out double median, out double IQR, out double kurtosis);
		}

		protected override void SetNRows(int inNRows)
		{
		}

		#endregion

		#region Private Methods
		protected virtual void ReadFileForFiberLocations(FiberParameters fParams, ref CellBoundary cb)
		{
			//Need to read in the input file with just locations and r. Then, make the boundary at 0,0,0?? and get the fiber volume fraction.  Have this method be virtual
			//and override it for the packing input file.  Also, work on using nu23 in the stiffness, then it can be set to 0 to see what happens.  See how much the stiffness
			//is changed...
			#region Initial stuff
			List<double[]> lFiberPositions = new List<double[]>();
			List<FiberParameters> lFiberParams = new List<FiberParameters>();
			nCount = 1;
			string[] temp;
			int nEndFlag = 0;

			#endregion

			//heading line
			NextLineToArray();

			while (nEndFlag == 0)
			{
				temp = NextLineToArray();

                //check if it's empty
                if (temp.Length < 3)
                {
					nEndFlag = 1;
                }
                else
                {
					lFiberPositions.Add(new double[] { fParams.l/2.0, Convert.ToDouble(temp[0]), Convert.ToDouble(temp[1]) });
					lFiberParams.Add(fParams);
					lFiberParams[lFiberParams.Count - 1].l = Convert.ToDouble(temp[2]);
				}
			}

			//Now make the boundary:
			NextLineToArray(); //One heading line
			temp = NextLineToArray();

			bottomLeftBackCorner = new double[3] { Convert.ToDouble(temp[0]), Convert.ToDouble(temp[1]), Convert.ToDouble(temp[2]) };
			width = Convert.ToDouble(temp[4]);
			height = Convert.ToDouble(temp[5]);
			boundary = new CellBoundary(new double[3] { fiberParams.l, width, height }, bottomLeftBackCorner);
			cb = boundary;

			dataRead.Close();

            //Now remove all of the projected fibers (fibers outside of the boundary).
            //This allows one to use fiber positions with and without projections
            for (int i = 0; i < lFiberPositions.Count; i++)
            {
				if (lFiberPositions[i][1] < bottomLeftBackCorner[1] || lFiberPositions[i][2] < bottomLeftBackCorner[2] 
					|| lFiberPositions[i][1] > (bottomLeftBackCorner[1] + width) || lFiberPositions[i][2] > (bottomLeftBackCorner[2] + height))
                {
					lFiberPositions.RemoveAt(i);
					i--; //do this since the current counter goes down by 1
                }

            }


			//Now, create the fibers (must be after boundary)
			lFibers = new List<Fiber>();
			for (int i = 0; i < lFiberPositions.Count; i++)
			{
				lFibers.Add(new Fiber(lFiberPositions[i], lFiberParams[i], boundary));
			}

		}
		#endregion

		#region Private Methods
		protected string[] NextLineToArray()
		{

			char[] charsToTrim = { ',', '.', ' ' };
			
			string tempLine = dataRead.ReadLine();
			tempLine = tempLine.Replace(" ", ""); //Get rid of empty spaces

			tempLine = tempLine.TrimEnd(charsToTrim);
			string[] temp = tempLine.Split(',');

			nCount++;

			return temp;
		}
		#endregion
	}
	
	[SerializableAttribute] //This allows to make a deep copy fast
	///This reads in a _all file, gets the first fiber positions, and starts from there.
	public class PackingOutputFilePacking : PackingFromFile
	{
		#region Constructors
		public PackingOutputFilePacking(FiberParameters inFiberParams) :
			base(inFiberParams)
		{
		}

		#endregion

		#region Private Methods
		protected override void ReadFileForFiberLocations(FiberParameters fParams, ref CellBoundary cb)
		{
			#region Initial stuff
			List<double[]> lFiberPositions = new List<double[]>();
			List<FiberParameters> lFiberParams = new List<FiberParameters>();
			nCount = 1;
			string[] temp;
			int nEndFlag = 0;
			string sSection = "None";

			#endregion

			while (nEndFlag == 0)
			{

				temp = NextLine();

				#region If the line is a section header...
				if (temp[0].Contains("*") == true)
				{ //
					sSection = temp[0];

					if (sSection == "*END")
					{ //ends it if the section is "end"
						nEndFlag = 1;
					}
				}
				#endregion

				#region If the line is a data member...
				else
				{ //For Data Members

					switch (sSection)
					{
						case "*Fibers":
							#region
							//**Fiber Index	 isProjected?	 CenterX	 CenterY	 CenterZ	 ZRot	 Radius	 Length
							temp = temp[0].Split(',');
							int fiberIndex = Convert.ToInt32(temp[0]);
							if (fiberIndex != -1)
							{
								lFiberPositions.Add(new double[] { Convert.ToDouble(temp[2]), Convert.ToDouble(temp[3]), Convert.ToDouble(temp[4]) });
								lFiberParams.Add(new FiberParameters(Convert.ToDouble(temp[6]), fParams.rho, Convert.ToDouble(temp[7]),
									fParams.E1, fParams.E2, fParams.nu12, fParams.globalD));
							}

							break;
						#endregion

						case "*Boundaries":
							#region
							//Get back corner: x
							temp = temp[0].Split(',');
							bottomLeftBackCorner[0] = Convert.ToDouble(temp[0]);

							//Get back corner: y
							NextLine();
							temp = NextLine();
							temp = temp[0].Split(',');
							bottomLeftBackCorner[1] = Convert.ToDouble(temp[1]);

							//Get the width (y)
							NextLine();
							temp = temp[0].Split(',');
							width = Convert.ToDouble(temp[1]) - bottomLeftBackCorner[1];

							//Get back corner: z
							NextLine();
							temp = temp[0].Split(',');
							bottomLeftBackCorner[2] = Convert.ToDouble(temp[2]);

							//Get the height (z)
							NextLine();
							temp = temp[0].Split(',');
							height = Convert.ToDouble(temp[2]) - bottomLeftBackCorner[2];
							nEndFlag = 1;

							boundary = new CellBoundary(new double[3] { fiberParams.l, width, height }, bottomLeftBackCorner);
							cb = boundary;

							break;
							#endregion
					}
				}
				#endregion
			}
			dataRead.Close();

			//Now, create the fibers (must be after boundary)
			lFibers = new List<Fiber>();
			for (int i = 0; i < lFiberPositions.Count; i++)
			{
				lFibers.Add(new Fiber(lFiberPositions[i], lFiberParams[i], cb));
			}

		}
		#endregion

		#region Private Methods
		private string[] NextLine()
		{
			bool bComment = true;
			string[] temp = new string[1];
			//Keep reading lines until it is not a comment line
			while (bComment)
			{

				temp = NextLineEvenComments();

				if (!temp[0].Contains("**"))
				{
					bComment = false;
				}
			}
			return temp;
		}

		private string[] NextLineEvenComments()
		{

			char[] charsToTrim = { ',', '.', ' ' };

			string tempLine = dataRead.ReadLine();
			tempLine = tempLine.Replace(" ", ""); //Get rid of empty spaces

			tempLine = tempLine.TrimEnd(charsToTrim);
			string[] temp = tempLine.Split('\t');

			nCount++;

			return temp;
		}
		#endregion
	}
}