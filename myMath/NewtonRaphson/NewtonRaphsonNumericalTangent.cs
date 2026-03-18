/*
 * Created by SharpDevelop.
 * User: sstaple
 * Date: 1/22/2010
 * Time: 11:53 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace myMath.NewtonRaphson
{
	/// <summary>
	///This performs the Newton-Raphson method in multiple dimensions to find some x which results in a certain y.
	/// Rather than finding the derivatives of the function, this uses a numerical step to approximate the derivative
	/// </summary>
	public class NewtonRaphsonNumericalTangent : NewtonRaphsonBase
	{
		
		#region Private member variables
		
		private IMatrixFunction f;
		protected double[] dx;
		public bool SlopeIsBeingCalculated = false;
		
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
		public NewtonRaphsonNumericalTangent(double [] inYDesired, double [] inXInitialGuess, IMatrixFunction Function,
		                           double inMaxRelError, double[] inStepSize, int inMaxIterations) 
			: base(inYDesired, inXInitialGuess, inMaxRelError, inMaxIterations)
		{
			dx = inStepSize;
			f = Function;
		}

		#endregion

		#region Private Methods
		public override double[,] DEval(double[] x)
		{
			SlopeIsBeingCalculated = true;
			int n = x.Length;
			double[][] YofXpDX = new double[n][];
			double[,] J = new double[n, n];
			double[] y = Eval(x);

			//Make dx the same sign as x
			double[] dxTemp = new double[n];
            for (int i = 0; i < n; i++)
            {
				dxTemp[i] = (x[i].Equals(0.0)) ? dx[i] : dx[i] * Math.Sign(x[i]);
			}

			for (int i = 0; i < n; i++)
			{
				double[] XpDX = new double[n];

				Array.Copy(x, XpDX, n);

				XpDX[i] += dxTemp[i];

				YofXpDX[i] = Eval(XpDX);

				//Set it back
				XpDX[i] -= dxTemp[i];
			}

			for (int i = 0; i < n; i++)
			{

				for (int j = 0; j < n; j++)
				{

					J[i, j] = 1 / dxTemp[j] * (YofXpDX[j][i] - y[i]);
				}
			}

			SlopeIsBeingCalculated = false;

			return J;
		}
		protected override double[] Eval(double[] x)
		{
			return f.Eval(x);
		}

		#endregion
		
	}
	
	#region Interfaces needed for the NewtonRaphsonSecant method
	
	public interface IMatrixFunction
	{
		double [] Eval(double [] X);
		
	}
	
	#endregion
}
