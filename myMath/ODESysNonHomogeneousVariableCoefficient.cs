/*
 * Created by SharpDevelop.
 * User: sstaple
 * Date: 1/3/2011
 * Time: 4:08 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace myMath.ODESystemSolver
{
    /// <summary>
    /// Description of ODESysNonHomogeneousVariableCoefficient.
    /// </summary>
    public class ODESysNonHomogeneousVariableCoefficient
	{
		
		#region Private Members
		private double [] x;
		private double [][] px;
		private int n;
		private double [][,] eAx;
		
		#endregion
		
		#region Public Members
		public double [] X{
			get {
				return x;
			}
		}
		
		public double [][] Px{
			get {
				return px;
			}
		}
		
		public double [][,] expAx{
			get {
				return eAx;
			}
		}
		#endregion
		
		#region Constructors
		public ODESysNonHomogeneousVariableCoefficient(IMatrixFunction CoefficientMatrix, IVectorFunction ForcingVector, double xf,
		                                               int nsteps):this(CoefficientMatrix, ForcingVector,
		                               xf, nsteps, 1000, 0.0001)
		{
			
		}
		
		public ODESysNonHomogeneousVariableCoefficient(IMatrixFunction CoefficientMatrix, IVectorFunction ForcingVector, double xf,
		                                               int nsteps, int maxIterations, double maxError)
		{
			//Solve the Homogeneous Problem
			ODESysVariableCoefficient HomSol = new ODESysVariableCoefficient(CoefficientMatrix, xf, nsteps, maxIterations, maxError);
			
			//Assign Variables from Homogeneous Problem
			x = HomSol.X;
			eAx = HomSol.expAx;
			
			//Get some constants assigned
			n = eAx[0].GetLength(0);
			double step = xf/nsteps;
			
			//Now, get the particular solution
			
			px = new double[nsteps+1][];
			
			double [] currentYP = new double[n];
			double [][] B = new double[nsteps + 1][];
			
			B[0] = ForcingVector.Eval(0.0);
			
			//Set the Bs
			for (int i = 1; i < nsteps + 1; i++){
				
				B[i] = ForcingVector.Eval(x[i] - step / 2.0);
			}
			
			//Initiate first px
			px[0] = new double[n];
			
			//Iterate for each output step
			for (int i = 1; i < nsteps + 1; i++){
				
				px[i] = new double[n];
				
				//trapezoid rule for the integral
				currentYP = MatrixMath.Multiply(HomSol.expAx[i], VectorMath.Add( VectorMath.ScalarMultiply(step, B[i]), currentYP));
				
				//Assign the Results
				for (int i1 = 0; i1 < n; i1++) {
					
					px[i][i1] = currentYP[i1];
				}
			}
		}
		#endregion
	}
	
	#region Interface for the forcing function vector B as a function of X
	public interface IVectorFunction{
		double [] Eval(double x);
	}
	#endregion
}
