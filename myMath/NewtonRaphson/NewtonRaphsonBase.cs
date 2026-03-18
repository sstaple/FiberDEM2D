/*
 * Created by SharpDevelop.
 * User: sstaple
 * Date: 1/22/2010
 * Time: 11:53 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace myMath.NewtonRaphson
{
	/// <summary>
	///This performs the Newton-Raphson method in multiple dimensions to find some x which results in a certain y.
	/// One must supply the function and the jacobian of the function: for the case where the derivative is known.
	/// </summary>
	public abstract class NewtonRaphsonBase
	{

		#region Private member variables
		protected List<double> errors;
		protected double[] yDesired;
		protected double[] xDesired;
		protected double[] x0;
		protected double maxError;
		protected int maxIterations;
		protected int n;
		protected int iterations;
		protected double finalError;

		protected double[] xCurrent;
		protected double[] yCurrent;
		protected double[] xPrevious;
		protected double[] yPrevious;

		#endregion

		#region Public Properties
		public List<double> Errors
		{
			get { return errors; }
		}
		public double FinalError
		{
			get
			{
				return finalError;
			}
		}
		public double[] X
		{
			get
			{
				return xDesired;
			}
		}
		public int Iterations
		{
			get
			{
				return iterations;
			}
		}
		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="yDesired">the desired result for y</param>
		/// <param name="xInitialGuess">an initial guess for x</param>
		/// <param name="maxRelError">the maximum relative error</param>
		/// <param name="stepSize">the step size for calculating derivatives</param>
		public NewtonRaphsonBase(double[] inYDesired, double[] inXInitialGuess,
									 double inMaxRelError, int inMaxIterations)
		{
			errors = new List<double>();
			yDesired = inYDesired;
			x0 = inXInitialGuess;
			maxError = inMaxRelError;
			n = yDesired.Length;
			maxIterations = inMaxIterations;
		}

		#endregion

		#region Private Methods
		public abstract double[,] DEval(double[] x);
		protected abstract double[] Eval(double[] x);

		protected double[] Residual(double[] y)
		{
			double[] r = myMath.VectorMath.Subtract(yDesired, y);

			return r;
		}

		protected double ResidualNorm(double[] r)
		{
			double Norm = myMath.VectorMath.Norm(r);

			return Norm;
		}

		protected virtual double[,] Jacobian(double[] x)
		{
			double[,] J = DEval(x);
			//double[,] Jinv;


			//Jinv = myMath.MatrixMath.InvertMatrix(J);

			return J;
		}

		#endregion

		#region public methods

			public void Solve()
		{
			xCurrent = new double[n];
			
			double[] rCurrent;
			double[] rPrevious;
			double errorCurrent = 0;
			double errorPrevious; 
			double[,] T = new double[n, n];
			int flag = 0;

			//first, initiate everything
			xPrevious = x0;

			yPrevious = Eval(x0);

			rPrevious = Residual(yPrevious);

			errorPrevious = ResidualNorm(rPrevious);

			//Now, iterate till the max error is reached or the maximum number of iterations
			while (flag == 0)
			{
				double[,] J = Jacobian(xPrevious);
				T = J;
				
				double[] xStep = myMath.MatrixMath.LinSolve(T, rPrevious);

				xCurrent = myMath.VectorMath.Add(xPrevious, xStep);

				yCurrent = Eval(xCurrent);

				rCurrent = Residual(yCurrent);

				errorCurrent = ResidualNorm(rCurrent);

				iterations++;

                //Now, how do we exit:

                //First, if the error is growing (This decreases the step and tries again)
                if (errorCurrent > errorPrevious) {
					/*flag = 1;
					iterations = maxIterations;
					*/
                    int miniCurrentIt = 0;
                    while ((miniCurrentIt <= 10) && (errorCurrent > errorPrevious)) {
                        xStep = myMath.VectorMath.ScalarMultiply(0.05, xStep);
                        if (miniCurrentIt >= 9) {
                            xStep = myMath.VectorMath.ScalarMultiply(-1.0, xStep);}

                        xCurrent = myMath.VectorMath.Add(xPrevious, xStep);

                        yCurrent = Eval(xCurrent);

                        rCurrent = Residual(yCurrent);

                        errorCurrent = ResidualNorm(rCurrent);

                        miniCurrentIt++;


                    }
                    //throw new ArgumentException("Newton Raphson Method Diverging!");
					
                }
                /**/
                //Now, if the error is below the max error
                if (errorCurrent <= maxError)
				{
					flag = 1;
				}

				//Now, if it's iterated too much
				if (iterations >= maxIterations)
				{
					//throw new ArgumentException("Newton Raphson Method Didn't find it in " + iterations + " iterations!");
					flag = 1;
				}

				//reset everything
				xPrevious = xCurrent;
				yPrevious = yCurrent;
				rPrevious = rCurrent;
				errorPrevious = errorCurrent;
				errors.Add(errorCurrent);
			}

			finalError = errorCurrent;
			//double[] resultsDebugging = new double[] { xCurrent[6], xCurrent[5], xCurrent[7], xCurrent[3], iterations, errorCurrent };
			xDesired = xCurrent;
		}
		
		#endregion
	}
}

