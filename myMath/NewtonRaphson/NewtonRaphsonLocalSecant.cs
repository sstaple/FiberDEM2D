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
    /// Rather than finding the derivatives of the function, this uses a numerical step to approximate the derivative
    /// </summary>
    public class NewtonRaphsonLocalSecant : NewtonRaphsonBase
	{

		#region Private member variables

		private IMatrixFunction f;
		protected double dx = -1;
		public double[,] LastJacobian;

		#endregion

		#region Public Properties

		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="yDesired">the desired result for y</param>
		/// <param name="xInitialGuess">an initial guess for x</param>
		/// <param name="maxRelError">the maximum relative error</param>
		/// <param name="stepSize">the step size for calculating derivatives</param>
		/// <param name="f">the function which inherits from IMatrixFunction</param>
		public NewtonRaphsonLocalSecant(double[] inYDesired, double[] inXInitialGuess, IMatrixFunction Function,
								   double inMaxRelError, int inMaxIterations, double initialStepSize)
			: base(inYDesired, inXInitialGuess, inMaxRelError, inMaxIterations)
		{
			f = Function;
			dx = initialStepSize;
		}

		public NewtonRaphsonLocalSecant(double[] inYDesired, double[] inXInitialGuess, IMatrixFunction Function,
								   double inMaxRelError, int inMaxIterations, double[,] initialJacobian)
			: base(inYDesired, inXInitialGuess, inMaxRelError, inMaxIterations)
		{
			f = Function;
			LastJacobian = initialJacobian;
		}
		#endregion

		#region Private Methods
		public override double[,] DEval(double[] x)
		{
			//For the first time
			if (Errors.Count == 0)
			{
				//If no initial jacobian was given
                if (!dx.Equals(-1))
                {
					xCurrent = new double[xPrevious.Length];
					for (int i = 0; i < xPrevious.Length; i++)
                    {
						xCurrent[i] = xPrevious[i] + dx;
                    }
					yCurrent = Eval(xCurrent);
                }
				//If an initial Jacobian was given
                else
                {
					return LastJacobian;
                }
			}
			
			int n = x.Length;
			double[,] J = new double[n, n];

			for (int i = 0; i < n; i++)
			{
				for (int j = 0; j < n; j++)
				{
					double dx = (xCurrent[j] - xPrevious[j]);
					//if the points are really close, this makes the slope 0
					J[i, j] = (dx.Equals(0.0)) ? 0.0 : (yCurrent[i] - yPrevious[i]) / dx;
				}
			}
			LastJacobian = J;
			return J;
		}
		protected override double[] Eval(double[] x)
		{
			return f.Eval(x);
		}

		#endregion

	}
}
