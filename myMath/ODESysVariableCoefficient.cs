/*
 * Created by SharpDevelop.
 * User: sstaple
 * Date: 2/11/2010
 * Time: 12:36 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace myMath.ODESystemSolver
{
    /// <summary>
    /// Description of ODESysVariableCoefficient.
    /// </summary>
    public class ODESysVariableCoefficient
	{
		#region Private Members
		private int maxIt;
		private double Error;
		private double [] x;
		private double [][,] eAx;
		private int n;
		private double [,] ACurrent;
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
		#endregion
		
		#region Constructors
		public ODESysVariableCoefficient(IMatrixFunction CoefficientMatrix, double xf,
		                         int nsteps):this(CoefficientMatrix, xf, nsteps, 10000, 0.00001)
		{
			
		}
		//TODO Create a variable step approach where the number of points is based on an error value rather
		//than a step size
		public ODESysVariableCoefficient(IMatrixFunction CoefficientMatrix,  double xf, int nsteps, 
		                                 int maxIterations, double maxError)
		{
			Error = maxError;
			maxIt = maxIterations;
			n =CoefficientMatrix.n;
			x = new double[nsteps + 1];
			eAx = new double[ nsteps + 1][,];
			ACurrent = new double[n,n];
			
			//make F identity
			for (int i =0; i < n; i++) {
				ACurrent[i,i] = 1;
			}
			
			eAx[0] = ACurrent;
			
			double step = (xf)/nsteps;
			
			//Set the first row of the output to the initial conditions 
			x[0] = 0.0;
			
			//Iterate for each output step
			for (int j = 1; j < nsteps + 1; j++){
				
				x[j] = xf * j / nsteps;
				
				//Midpoint Method
				ACurrent = CoefficientMatrix.Eval(x[j]-step* 0.5);
				
				MatrixExponential myExp = new MatrixExponential(ACurrent, step, maxIt, maxError);
				eAx[j] =myMath.MatrixMath.Multiply(myExp.Solve(step),eAx[j-1]);
				
			}
		}
		
		#endregion
		
	}
	
	#region Interface for the matrix as a function of x
	public interface IMatrixFunction{
		int n{
			get;
		}
		double [,] Eval(double x);
	}
	#endregion

}


