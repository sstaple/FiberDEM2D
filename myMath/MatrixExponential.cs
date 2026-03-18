/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 2/10/2010
 * Time: 5:28 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace myMath.ODESystemSolver
{
    /// <summary>
    /// Solves a system of constant coefficient ODE's using the taylor series expansion of the matrix exponential.  recommended to use a value
    /// of x that is normalized to stay in small values, say 0 to 1.  Method also scales the A matrix to reduce round-off error, than
    /// squares it to get it back to it's original magnitude.
    /// </summary>
    public class MatrixExponential
	{
		#region Private Members
		private int maxIt;
		private double Error;
		private double [,] AScaled;
		private int scalingFactor;
		private double[][,] PowersOfA;
		#endregion
		
		#region Public Members
		public double CurrentError;
		public int nIterations;
		#endregion
		
		#region Constructors
		public MatrixExponential(double [,] A, double xf):this(A, xf, 1000, 0.000001)
		{
		}
		
		public MatrixExponential(double [,] A, double xf, int maxIterations, double maxError)
		{
			Error = maxError;
			maxIt = maxIterations;
			PowersOfA = new double[maxIt][,];
			
			//First, find the scaling factor
			scalingFactor = FindScalingFactor(A, xf);
			
			//Then, scale the matrix A
			AScaled = Scale(A, scalingFactor);
			
			PowersOfA[0] = AScaled;
		}
		#endregion
		
		#region public methods
		public double [,] Solve(double x){
			
			double [,] Unsquared = ExponentialTaylorsSeries(AScaled, x);
			//double [,] Unsquared = ExponentialPadeApproximation(AScaled, x);
			
			double [,] Squared = Square(Unsquared, scalingFactor);
			
			return Squared;
		}
		#endregion
		
		#region private methods
		#region
		/// <summary>
		/// This solves the system of ODE's at some x.
		/// </summary>
		/// <param name="x"> location x at which you want the answer</param>
		/// <returns>exp(Ax)</returns>
		#endregion
		private double[,] ExponentialTaylorsSeries(double [,] A, double x)
		{
			int k = 1; //counter
			int n = A.GetLength(0);
			double [,] E = new double[n,n];
			double [,] F = new double[n,n];
			
			#region Some commands to check out the condition number
			//Mapack.Matrix Amatrix = myMath.MatrixMath.ArrayToMatrix(A);
			//Mapack.SingularValueDecomposition mySVD = new SingularValueDecomposition(Amatrix);
			//double condNumb = mySVD.Condition;
			
			//Mapack.EigenvalueDecomposition myEig = new EigenvalueDecomposition(Amatrix);
			/* */
			#endregion
			
			//make F identity
			for (int i =0; i < n; i++) {
				F[i,i] = 1;
			}
			
			double errorNorm = 1.0;
			double PrevError = 2.0e10;
			double XoverKfactorial = 1.0;
			while ( errorNorm > Error) {
				
				E = MatrixMath.Add(E,F);
				
				
				if (PowersOfA[k-1] == null) {
					
					PowersOfA[k-1] = MatrixMath.Multiply(A, PowersOfA[k-2]);
				}
				
				XoverKfactorial = XoverKfactorial * x/k;
				F = MatrixMath.ScalarMultiply(XoverKfactorial,PowersOfA[k-1]);
				
				k+=1;
				
				//errorNorm = MatrixMath.pNorm(1, MatrixMath.Subtract(MatrixMath.Add(E,F),E));
				errorNorm = MatrixMath.PNorm(2, F) / MatrixMath.PNorm(2, E);
				//This is my own formula: it's a relative error based on the 2nd norm.
				if (k > maxIt) {//1000){ //
					
					throw new ArgumentException("Exponentionial did not converge at x =" + x + ".  Try increasing the integration parameters.");
					
				}
				
				PrevError = errorNorm;
				
			}
			
			CurrentError = errorNorm;
			nIterations = k;
			return MatrixMath.Add(E,F);;
			
		}
		
		private double[,] ExponentialPadeApproximation(double [,] A, double x)
		{
			int n = A.GetLength(0);
			double [,] E = new double[n,n];
			double [,] D = new double[n,n];
			double c;
			double [,] cX;
			int q = 1; //Starting value: routine uses more if the error isn't sufficiently small
			bool IsEven;
			double [,] In = new double[n,n];
			double magA = Math.Abs(MatrixMath.PNorm(2, A));
			double [,] EPrev = new double[n,n];
			bool first = true;
			
			//make the identity
			for (int i =0; i < n; i++) {
				In[i,i] = 1;
			}
			
			double errorNorm = Error * 10;
			
			while ( errorNorm > Error) {
				
				q+=1; //keep increasing q
				
				//Instantiate a bunch of values
				c = 0.5 * x;
				cX = MatrixMath.ScalarMultiply(c, A);
				E = MatrixMath.Add(In, cX);
				D = MatrixMath.Subtract(In, cX);
				IsEven = false;
				
				for (int j = 2; j < q + 1; j++) {
					
					c = c * (q - j + 1.0) / (j * (2.0 * q - j + 1.0))  * x ;
					
					if (PowersOfA[j-1] == null) {
						
						PowersOfA[j-1] = MatrixMath.Multiply(A, PowersOfA[j-2]);
					}
					
					cX = MatrixMath.ScalarMultiply( c, PowersOfA[j-1]);
					
					E = MatrixMath.Add(E, cX);
					
					if(IsEven) D = MatrixMath.Add(D, cX);
					else D = MatrixMath.Subtract(D, cX);
					
					IsEven = !IsEven;
				}
				
				E = MatrixMath.Multiply( MatrixMath.InvertMatrix(D), E);
				
				#region Get error
				if (first) {
					first = false;
				}
				
				else{
					double magE = Math.Abs( MatrixMath.PNorm(1, E));
					double magEPrev = Math.Abs( MatrixMath.PNorm(1, EPrev));
					errorNorm = Math.Abs(magEPrev - magE) / magE;
				}
				
				for (int k = 0; k < n; k++) {
					for (int l = 0; l < n; l++) {
						EPrev[k,l] = E[k,l];
					}
				}
				
				
				if (q > 10000-q){ //maxIt) {
					
					throw new ArgumentException("Exponentionial did not converge at x =" + x + ", Error = " + errorNorm);
					//errorNorm = Error * 0.1;
					
				}
				#endregion
			}
			
			CurrentError = errorNorm;
			nIterations = q;
			return E;
			
		}
		
		private int FindScalingFactor(double [,] A, double xf){
			
			int j = 0;
			double Norm = myMath.MatrixMath.PNorm(2, A);
			
			double temp = Norm - 2.0;
			
			if(xf > 1.0){
				temp = Norm * xf - 2.0;
			}
			
			if( temp > 1.0){
				double dj = System.Math.Log(temp);
				
				//now, make j an int just above df
				j = (int)System.Math.Ceiling(dj);
			}
			else{
				j = 0;
			}
			
			return j;
		}
		
		private double [,] Scale(double [,] A, int j){
			
			if (j == 0) {
				return A;
			}
			
			double scalingFactor = 1.0 / System.Math.Pow(2.0, j);
			
			return myMath.MatrixMath.ScalarMultiply(scalingFactor, A);
		}
		
		private double [,] Square(double [,] Ascaled, int j){
			
			double [,] A = Ascaled;
			
			for (int i = 0; i < j; i++) {
				
				A = myMath.MatrixMath.Multiply(A, A);
				
			}
			return A;
		}
		#endregion
		
	}
}