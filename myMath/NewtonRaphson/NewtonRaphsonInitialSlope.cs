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
    public class NewtonRaphsonInitialSlope : NewtonRaphsonBase
	{

		#region Private member variables

		private IMatrixFunction f;
		public double[,] LastJacobian;
		protected double[,] jacobian;
		protected bool firstAnalysis;

		#endregion

		#region Public Properties

		#endregion

		#region Constructor


		public NewtonRaphsonInitialSlope(double[] inYDesired, double[] inXInitialGuess, IMatrixFunction Function,
								   double inMaxRelError, int inMaxIterations, double[,] initialJacobian)
			: base(inYDesired, inXInitialGuess, inMaxRelError, inMaxIterations)
		{
			f = Function;
			LastJacobian = initialJacobian;
			firstAnalysis = true;
		}
		#endregion

		#region Private Methods
		protected override double[,] Jacobian(double[] x)
		{
            if (firstAnalysis)
            {
				//double[,] J = DEval(x);
				//jacobian = myMath.MatrixMath.InvertMatrix(J);
				jacobian = DEval(x);
			}

			return jacobian;

		}

		public override double[,] DEval(double[] x)
		{
			return LastJacobian;
		}
		protected override double[] Eval(double[] x)
		{
			return f.Eval(x);
		}

		#endregion

	}
}
