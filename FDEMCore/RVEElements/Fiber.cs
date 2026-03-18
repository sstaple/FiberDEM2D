/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 2/6/2013
 * Time: 7:48 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using RandomMath;
using System.IO;



namespace FDEMCore
{
	/// <summary>
	/// Description of Fiber.
	/// </summary>

	[SerializableAttribute] //This allows to make a deep copy fast
	public class Fiber : SolidObject
	{
		#region Private Members
		private double radius;
		private double globalDampingFactor;
		private bool hasProjectedFibers;
		private bool isCornerFiber;
		private List<ProjectedFiber> projectedFibers;
		//Results to be saved
		private List<bool> outHasProjectedFibers;
		private List<List<ProjectedFiber>>  outProjectedFibers;
		private double oLength;
        private double currentLength;
		private CellBoundary cb;
		private double [] fOrientation; // Vector showing where the fiber is oriented
		private bool switchedOriginalFiber;
		private List<double[,]> homogenizedOutOfPlaneStress;
		
		#endregion
		
		#region Public Members
		public double Radius {
			get { return radius; }
		}
		public bool HasProjectedFibers {
			get { return hasProjectedFibers; }
			set { hasProjectedFibers = value; }
		}
		public bool IsCornerFiber {
			get { return isCornerFiber; }
		}
		public List<ProjectedFiber> ProjectedFibers {
			get { return projectedFibers; }
			set { projectedFibers = value; }
		}
		public double OLength {
			get { return oLength; }
		}
		public double[] FOrientation {
			get { return fOrientation; }
		}
        public double CurrentLength
        {
            get { return currentLength; }
		}
		public List<double[,]> HomogenizedOutOfPlaneStress
		{
			get { return homogenizedOutOfPlaneStress; }
			set { homogenizedOutOfPlaneStress = value; }
		}
		#endregion

		#region Constructors

		public Fiber(double[] initialPosition, FiberParameters inFParams, CellBoundary cellBoundary)
			: this(initialPosition, inFParams.R, inFParams.E1, inFParams.E2, inFParams.nu23, inFParams.nu12, inFParams.G12, inFParams.l, inFParams.m, inFParams.globalD, cellBoundary)
		{
			
		}
		public Fiber(double [] initialPosition, double r, double E1, double E2, double nu23, double nu12, double G12, double l, double m, double globalD, CellBoundary cellBoundary)
			:base(initialPosition, m, E1, E2, nu12, nu23, G12)
		{
			//Set critical damping
			//TODO: Check this, I doubt it is correct!!!!!!!!!!!
			double Estar = 1d / ( (1d - nu23 * nu23) / E2 + (1d - nu23 * nu23) / E2);
			double tempk = Math.PI* Estar * l / 4d;
			globalDampingFactor = globalD;
			SetCriticalGlobalDamping(m, tempk, globalD);
			
			cb = cellBoundary;
            oLength = l; // cellBoundary.ODimensions[0];
			radius = r;
			inertia = 0.5 * mass * Math.Pow(radius, 2d);
			fOrientation = new double[3]{1d, 0d, 0d};
			switchedOriginalFiber = false;
			projectedFibers = new List<ProjectedFiber>();
            currentLength = oLength;
			homogenizedOutOfPlaneStress = new List<double[,]>();

            //Initiate results to be saved
            outHasProjectedFibers = new List<bool> { false };

			outProjectedFibers = new List<List<ProjectedFiber>>{ new List<ProjectedFiber>() };

			//Give it a rotational stiffness to prevent rotation

			//base.KglobalRotate = Math.PI * Math.Pow(radius,4) * base.modulus1 /
			//	(4.0 * oLength * (1.0 + base.nu));
		}
		public Fiber(double [] initialPosition, FiberParameters inFParams, CellBoundary cellBoundary, double [] initialVelocity, double initialRotVel)
			:this(initialPosition, inFParams, cellBoundary)
		{
			x[1] = initialVelocity;
			r[1] = initialRotVel;
		}
		#endregion
		
		#region Public Methods
		public Fiber DeepCopy()
        {
			Fiber f = new Fiber(CurrentPosition, radius, base.modulus1, base.modulus2, base.nu12, base.nu12, base.G12, oLength, base.mass, globalDampingFactor, cb);
			return f;
        }
		
		public override void WriteOutput(int i, StreamWriter dataWrite)
		{
			dataWrite.WriteLine("," + outHasProjectedFibers[i].ToString() + "," + position[i][0]
			                    + "," + position[i][1] + "," + position[i][2] + "," + rotation[i] + "," + radius + "," + oLength);
			
			if (outHasProjectedFibers[i] == true) {
				
				foreach (ProjectedFiber pf in outProjectedFibers[i]) {
					
					dataWrite.WriteLine("-1,true," + pf.CurrentPosition[0]
					                    + "," + pf.CurrentPosition[1] + "," + pf.CurrentPosition[2] + "," + rotation[i] + "," + radius + "," + oLength);
				}
			}
		}
		
		public void AddProjectedFiber(double [] oPeriodicProjection, bool hasOrignialFiberSwitched, int[] cellWallIndices){
			
			switchedOriginalFiber = hasOrignialFiberSwitched;
			//No previous Projected Fibers to initiate the positions
			if (!hasProjectedFibers) {
				
				hasProjectedFibers = true;
			}
			//If it already has a projected fiber (Intersects both top and side wall): Project it to the opposite corner
			else{
				//Get the new projection by adding the current and projection from the already-created projection
				double [] cornerProjection = VectorMath.Add(oPeriodicProjection, projectedFibers[0].OPeriodicProjection);
				int[] multipleCellWallIndices = new int[]{ projectedFibers[0].CellWallIndices[0],  cellWallIndices[0]};
				
				projectedFibers.Add(new ProjectedFiber(cornerProjection, this, cb, multipleCellWallIndices));
				
				
				isCornerFiber = true;
				//check or correct the remaining fiber
			}
			
			projectedFibers.Add(new ProjectedFiber(oPeriodicProjection, this, cb, cellWallIndices));
			
			if (hasOrignialFiberSwitched) {
				double [] tempV = VectorMath.DeepCopy(x[1]);
				double [] tempX = VectorMath.DeepCopy(x[0]);
				x[0] = projectedFibers[projectedFibers.Count - 1].CurrentPosition;
				x[1] = projectedFibers[projectedFibers.Count - 1].CurrentVelocity;
				//switch indices here???
				projectedFibers[projectedFibers.Count - 1] = new ProjectedFiber(tempX, tempV, VectorMath.ScalarMultiply(-1.0, oPeriodicProjection), cellWallIndices); //switch the projection vector because the original is now changed
			}
		}
		
		//Just project.  Who cares about anything else?
		public void AddProjectedFiber(double [] oPeriodicProjection, int[] cellWallIndices)
		{
			
			hasProjectedFibers = true;
			projectedFibers.Add(new ProjectedFiber(oPeriodicProjection, this, cb, cellWallIndices));
		}
		
		public override void UpdateTimeStep(double timeStep){
			//ClearProjectedFibers(); //Got ridd of this, and put it in the projected fiber stuff.  I figure that they should only be cleared before new ones are made
			base.UpdateTimeStep(timeStep);
            //TODO put this somewhere else!!!
            currentLength = oLength + (Math.Sqrt(2.0 * cb.currStrain[0] + 1.0) - 1.0) * oLength;
		}
		
		public void ClearProjectedFibers()
        {
			hasProjectedFibers = false;
			isCornerFiber = false;
			projectedFibers = new List<ProjectedFiber>();
		}

		public override void SaveTimeStep(int i){
            //TODO: May have to save position here??
            if (i == 0)
            {
				outHasProjectedFibers[0] = hasProjectedFibers;
				outProjectedFibers[0] = projectedFibers;
            }
            else
            {
				outHasProjectedFibers.Add(hasProjectedFibers);
				outProjectedFibers.Add(projectedFibers);
			}
			base.SaveTimeStep(i);
		}
		
		public override void UpdatePosition(){
			//Just run the other update position, then set the x-position, x-acceleration, and x-velocity since they are prescribed
			if (!switchedOriginalFiber) { //Don't update everything if the original fiber switched: it would give an artificial velocity/position/acceleration!!!
				base.UpdatePosition();
			}
			double [] x_v = cb.DefyzToDefxAndv(x[0], oLength / 2.0);
			x [0] [0] = x_v [0];  //Set fiber direction position
			x[1][0] = x_v [1]; //Set fiber direction velocity
			x[2][0] = 0.0; //Acceleration: in fiber direction, there is NONE
			x[3][0] = 0.0;
			x[4][0] = 0.0;
			
			switchedOriginalFiber = false;
		}

		#region Extremely buggy code having to do with homogenization based on outer forces.  See version from 2013_03_17 to see it in action
		/*public int FiberIndex(double [] position){
			int index = -2;
			double pmago = VectorMath.Norm(position);
			double p1mag = VectorMath.Norm(currentPosition);
			double p2mag = 0;
			if ((Math.Abs(pmago - p1mag) / radius) < 1) { //TODO should be lower
				index = -1;
			}
			if (hasProjectedFibers) {
				for (int i = 0; i < projectedFibers.Count; i++) {
					p2mag = VectorMath.Norm(projectedFibers[i].Position);
					if ((Math.Abs(pmago - p2mag) / radius) < 1) { //TODO should be lower
						index = i;
					}
					//if (index == -2) {
					//	index = -2;}
				}
			}
			if (index == -2) {
				index = -2;
				//for (int i = 0; i < projectedFiberPosition.Count; i++) {
				//	p2mag = VectorMath.Norm(projectedFiberPosition[i]);
				//}
			}
			return index;
		}
		
		public int FindProjectionIndex(int direction){
			//-1 is the original, return -1
			//0 is the projection in the x direction: find 0 dir
			//1 is the projection in the y direction: find 1 dir
			//2 is the projection in the xy direction: find 2 dir
			int index = -1;
			
			if (direction != -1) {
				index = projectedDirection.IndexOf(direction);
			}
			return index;
		}
		 */
		#endregion

		public double[,] AverageHomogenizedOutOfPlaneStress()
        {
			double[,] sumAndAverageOfOOPStress = new double[3, 3];

			#region Average the stresses
			/*

						foreach (double[,] s in homogenizedOutOfPlaneStress)
						{
							sumAndAverageOfOOPStress = MatrixMath.Add(sumAndAverageOfOOPStress, s);
						}
			//Now this finds the average, and multiplies by 2 because each contribution is over half of the volume

			if (homogenizedOutOfPlaneStress.Count > 0)
			{
				sumAndAverageOfOOPStress = MatrixMath.ScalarMultiply(2.0 / homogenizedOutOfPlaneStress.Count, sumAndAverageOfOOPStress);
			}
			*/		
			#endregion

			#region try taking the maximum component in each direction
			double[,] maxS = new double[3, 3];
			foreach (double[,] s in homogenizedOutOfPlaneStress)
			{
				for (int i = 0; i < s.GetLength(0); i++)
				{
					for (int j = 0; j < s.GetLength(1); j++)
					{
						maxS[i, j] = Math.Abs(s[i, j]) > Math.Abs(maxS[i, j]) ? s[i, j] : maxS[i, j];
					}
				}
			}
			sumAndAverageOfOOPStress = maxS;
			
			#endregion

			sumAndAverageOfOOPStress = MatrixMath.ScalarMultiply(2.0, sumAndAverageOfOOPStress);

//Now reset the stress
homogenizedOutOfPlaneStress = new List<double[,]>();

return sumAndAverageOfOOPStress;
}
#endregion

#region Private Methods

#endregion
}
}
