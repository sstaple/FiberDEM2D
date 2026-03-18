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
	//Make an SolidObject
	[SerializableAttribute] //This allows to make a deep copy fast
	public class SolidObject:IPoint
	{
		#region Private Members
		protected double modulus1; //Axial
		protected double modulus2; //Transverse
		protected double nu12;
		protected double nu23;
		protected double G12;
		protected double mass;
		protected double inertia;
		protected double [] dt;
		protected double c0;
		protected double c1;
		protected double c3;
		protected double c4;
		protected double Cglobal = 0.0;
		protected double CglobalRotate = 0.0;
		protected double Kglobal = 0.0;
		protected double KglobalRotate=0.0;
		protected double dCrit = 0.0;

		//These are to store the Results
		protected List<double []> position;
		//protected double [][] velocity;
		//protected double [][] acceleration;
		//protected double [][] netForce;
		protected List<double> rotation;
		//protected double [] rotVel;
		//protected double [] rotAcc;
		//protected double [] netMoment;

		//These are to use
		protected double [][] x;
		protected List <double []> forces;
		protected List <double> moments;
		protected double [] r;
		protected double netM;
		protected double [] netF;

		#endregion

		#region Public Members
		public double Inertia {
			get { return inertia; }
			set {inertia = value;}
		}
		public double Mass {
			get { return mass; }
			set {mass = value;}
		}
		public double Dt {
			get { return dt[0]; }
		}
		public double Modulus1 {
			get { return modulus1; }
			set {modulus1 = value;}
		}
		public double Modulus2 {
			get { return modulus2; }
			set {modulus1 = value;}
		}
		public double Nu12 {
			get { return nu12; }
			set {nu12 = value;}
		}
		public double Nu23
		{
			get { return nu23; }
			set { nu23 = value; }
		}
		public double ShearModulus12
		{
			get { return G12; }
			set { G12 = value; }
		}
		public double[] CurrentPosition {
			get { return x[0]; }
			set { x[0] = value; }
		}
		public double[] CurrentVelocity {
			get { return x[1]; }
			set { x[1] = value; }
		}
		public double[] CurrentAcceleration {
			get { return x[2]; }
		}
		public double CurrentRotation {
			get { return r[0]; }
			set { r[0] = value; }
		}
		public double CurrentRotVel {
			get { return r[1]; }
		}
		public double CurrentRotAcc {
			get { return r[2]; }
		}
		public List<double[]> CurrentForces {
			get { return forces; }
			set { forces = value; }
		}
		public List<double> CurrentMoments {
			get { return moments; }
			set { moments = value; }
		}
		public double GlobalDampingCoeff {
			get { return (Cglobal / dCrit); }
			set { Cglobal = value * dCrit; }
		}
		//Results to be saved (not re-written)
		public List<double []> Position {
			get { return position; }
            set { position = value; }
		}
		public List<double> Rotation {
			get { return rotation; }
		}
		public double[] CurrentNetForce
		{
			get { return netF; }
		}
		public double CurrentNetMoment {
			get { return netM; }
		}

		#endregion

		#region Constructors
		
		public SolidObject(double[] initialPosition, double inMass, double inModulus1, double inModulus2, double inNu12, double inNu23, double inG12)
		{
			modulus1 = inModulus1;
			modulus2 = inModulus2;
			nu23 = inNu23;
			nu12 = inNu12;
			G12 = inG12;
			mass = inMass;

			double[] zeros = new double[initialPosition.Length];

			//Initiate results to be saved
			position = new List<double[]>();
			rotation = new List<double>();
			position.Add(initialPosition);
			rotation.Add(0d);

			//velocity = new double[nStepsRecorded + 1][];
			//acceleration = new double[nStepsRecorded + 1][];
			//rotVel = new double[nStepsRecorded + 1];
			//rotAcc = new double[nStepsRecorded + 1];
			//netForce = new double[nStepsRecorded + 1][];
			//netMoment = new double[nStepsRecorded + 1];

			x = new double[5][];
			r = new double[5];
			forces = new List<double[]>();
			netF = zeros;
			moments = new List<double>();

			for (int i = 0; i < 5; i++)
			{
				x[i] = zeros;
			}
			x[0] = initialPosition;
		}
		
		#endregion

		#region Public Methods

		public virtual void WriteOutput(int i, StreamWriter dataWrite){}
		#region
		/// <summary>
		/// This must be called at the beginning of each time step change
		/// </summary>
		/// <param name="timeStep">Size of the time step (dt)</param>
		#endregion
		public virtual void UpdateTimeStep(double timeStep){
			//This forces "Update Position" to be called first!!!
			dt = new double[4]{timeStep, timeStep * timeStep,  timeStep * timeStep * timeStep,
				timeStep * timeStep * timeStep * timeStep};
			c0 = 19d * dt[1] / (90d * 2d);
			c1 = 0.75 * dt[0] / 2d;
			c3 = 3d / (2d * dt[0]);
			c4 = 1d / dt[1];
		}

		public virtual void UpdatePosition(){
			//Add the prescribed displacements in the z-direction

			//5th order taylor's series expansion
			x[0] = VectorMath.Add(x[0],
				VectorMath.Add(VectorMath.ScalarMultiply(dt[0], x[1]),
					VectorMath.Add(VectorMath.ScalarMultiply(0.5 * dt[1], x[2]),
						VectorMath.Add(VectorMath.ScalarMultiply(dt[2] / 6d, x[3]),
							VectorMath.ScalarMultiply(dt[3] / 24d, x[4])))));
			x[1] = VectorMath.Add(x[1],
				VectorMath.Add(VectorMath.ScalarMultiply(dt[0], x[2]),
					VectorMath.Add(VectorMath.ScalarMultiply(0.5 * dt[1], x[3]),
						VectorMath.ScalarMultiply(dt[2] / 6d, x[4]))));
			x[2] = VectorMath.Add(x[2],
				VectorMath.Add(VectorMath.ScalarMultiply(dt[0], x[3]),
					VectorMath.ScalarMultiply(0.5 * dt[1], x[4])));
			x[3] = VectorMath.Add(x[3],
				VectorMath.ScalarMultiply(dt[0], x[4]));
		}

		public virtual void UpdateRotPosition(){

			r[0] = r[0] + dt[0] * r[1] + 0.5 * dt[1] * r[2] + dt[2] / 6d * r[3] + dt[3] / 24d * r[4];
			r[1] = r[1] + dt[0] * r[2] + 0.5 * dt[1] * r[3] + dt[2] / 6d * r[4];
			r[2] = r[2] + dt[0] * r[3] + 0.5 * dt[1] * r[4];
			r[3] = r[3] + dt[0] * r[4];
		}

		public virtual void UpdateAcceleration(){

			SumAndClearForces();

			double [] aTemp = VectorMath.ScalarMultiply(1.0 / mass, VectorMath.Subtract(netF, VectorMath.Add(VectorMath.ScalarMultiply(Cglobal, x[1]), VectorMath.ScalarMultiply(Kglobal, x[0]))));
			double [] dA = VectorMath.Subtract(aTemp, x[2]);

			//Now do a corrector on the position and derivatives
			x[0] = VectorMath.Add(x[0], VectorMath.ScalarMultiply(c0,dA));
			x[1] = VectorMath.Add(x[1], VectorMath.ScalarMultiply(c1,dA));
			x[2] = aTemp;
			x[3] = VectorMath.Add(x[3], VectorMath.ScalarMultiply(c3,dA));
			x[4] = VectorMath.Add(x[4], VectorMath.ScalarMultiply(c4,dA));

		}

		public virtual void UpdateRotAcceleration(){

			SumAndClearMoment();

			double aTemp = netM / inertia - Cglobal / inertia * r[1] - KglobalRotate / inertia * r[0]; //Dampen out the rotation also
			double dA = aTemp - r[2];

			//Now do a corrector on the position and derivatives
			r[0] += c0 * dA;
			r[1] += c1 * dA;
			r[2] = aTemp;
			r[3] += c3 * dA;
			r[4] += c4 * dA;

		}
		#region
		/// <summary>
		/// Record results for time step i
		/// </summary>
		/// <param name="i">current iteration, i.  Check to see what this means for multiple analysis</param>
		#endregion
		public virtual void SaveTimeStep(int i){
            if (i==0)
            {
				position[0] = VectorMath.DeepCopy(x[0]);
				rotation[0] = r[0];

            }
            else
            {
				position.Add(VectorMath.DeepCopy(x[0]));
				rotation.Add(r[0]);
			}
			
		}

		public virtual void StopObject(){

			x[1] = new double[x[1].Length];
			x[2] = new double[x[1].Length];
			x[3] = new double[x[1].Length];
			x[4] = new double[x[1].Length];

			r[1] = 0;
			r[2] = 0;
			r[3] = 0;
			r[4] = 0;
		}

		public void SetCriticalGlobalDamping(double M, double K, double globalDFactor){

			dCrit =  Math.Sqrt(M*K); //for critical damping (Global)
			Cglobal = globalDFactor * dCrit;
		}

		public void SumAndClearForces()
        {
			//reset the net force
			netF = new double[x[0].Length];

			foreach (double[] force in forces)
			{
				netF = VectorMath.Add(netF, force);
			}
			//Reset the forces
			forces = new List<double[]>();
		}
		public void SumAndClearMoment()
		{
			//reset the net moment
			netM = 0;

			foreach (double mom in moments)
			{
				netM += mom;
			}
			moments = new List<double>();
		}
		#endregion
	}


	public interface IPoint{
		double [] CurrentPosition{
			get; set;
		}
		double [] CurrentVelocity{
			get; set;
		}
	}
}
