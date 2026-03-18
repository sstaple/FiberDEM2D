/*
 * Created by SharpDevelop.
 * User: sstaple
 * Date: 2/25/2010
 * Time: 2:25 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace myMath.ODESystemSolver
{
    /// <summary>
    /// Description of MExpConstantStep.
    /// </summary>
    public class MExpConstantStep
	{
		#region Private Members
		private double [] x;
		private double [][,] eAx;
		private double [] finalErrors;
		private int [] nIterations;
		#endregion
		
		#region Public Members
		public double [] X{
			get {
				return x;
			}
		}
		public double [][,] expAx{
			get {
				return eAx;
			}
		}
		public double[] FinalErrors {
			get { return finalErrors; }
		}
		public int[] NIterations {
			get { return nIterations; }
		}
		#endregion
		
		#region Constructors
		public MExpConstantStep(double [,] A, double xf,
		                        int nsteps):this(A,
		                 xf, nsteps, 10000, 0.000001)
		{
			
		}
		
		public MExpConstantStep(double [,] A,double xf,
		                        int nsteps,
		                        int maxIterations, double maxError)
		{
			int n = A.GetLength(0);
			
			x = new double[nsteps + 1];
			eAx = new double[ nsteps + 1][,];
			nIterations = new int[nsteps + 1];
			finalErrors = new double[nsteps + 1];
			
			double step = (xf)/nsteps;
			
			#region solve it down the line (traditional)
			
			//Initialize a MatrixExponential object
			
			MatrixExponential myExp = new MatrixExponential(A, xf, maxIterations, maxError);
			
			//Iterate for each output step
			for (int j = 0; j < nsteps + 1; j++){
				
				x[j] = step*j;
				eAx[j] = myExp.Solve(x[j]);
				nIterations[j] = myExp.nIterations;
				finalErrors[j] = myExp.CurrentError;
				
			}
			#endregion
			
			#region solve it using the small interval method
			
			//Initialize a MatrixExponential object
			/*
			MatrixExponential myExp = new MatrixExponential(A, step, maxIterations, maxError);
			x[0] = 0.0;
			eAx[0] = myExp.Solve(0.0);
			nIterations[0] = myExp.nIterations;
			finalErrors[0] = myExp.CurrentError;
			
			double [,] expStep = myExp.Solve(step);
			
			for (int j = 1; j < nsteps + 1; j++){
				
				x[j] = step*j;
				
				eAx[j] =myMath.MatrixMath.Multiply(expStep, eAx[j-1]);
				
				nIterations[j] = myExp.nIterations;
				finalErrors[j] = myExp.CurrentError;
			}
			*/
			#endregion
		}
		
		
		
		#endregion
	}
}