/*
 * Created by SharpDevelop.
 * User: sstaple
 * Date: 1/22/2010
 * Time: 11:53 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace myMath.NewtonRaphson
{
    /// <summary>
    ///This performs the Newton-Raphson method in multiple dimensions to find some x which results in a certain y.
    /// One must supply the function and the jacobian of the function: for the case where the derivative is known.
    /// </summary>
    public class NewtonRaphsonJacobian : NewtonRaphsonBase
	{

		#region Private member variables
		private IMatrixFunctionAndDerivative f;

		#endregion

		#region Public Properties

		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="yDesired">the desired result for y</param>
		/// <param name="xInitialGuess">an initial guess for x</param>
		/// <param name="Function">the function which inherits from IMatrixFunction</param>
		/// <param name="maxRelError">the maximum relative error</param>
		/// <param name="inMaxIterations">the maximum number of iterations allowed</param>
		public NewtonRaphsonJacobian(double [] inYDesired, double [] inXInitialGuess, IMatrixFunctionAndDerivative Function,
		                             double inMaxRelError, int inMaxIterations): base(inYDesired, inXInitialGuess, inMaxRelError, inMaxIterations)
		{
			f = Function;
		}

		#endregion

		#region Private Methods
		public override double[,] DEval(double[] x)
		{
			return f.DEval(x);
		}
		protected override double[] Eval(double[] x)
		{
			return f.Eval(x);
		}
        #endregion

    }

    #region Interfaces needed for the NewtonRaphsonSecant method

    public interface IMatrixFunctionAndDerivative
	{
		double [] Eval(double [] X);
		double [,] DEval(double [] X);
		
	}
	
	#endregion
}

