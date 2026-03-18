/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 1/16/2009
 * Time: 4:23 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;


namespace myMath
{
    /// <summary>
    /// Performs Various numerical Calculus operations
    /// </summary>
    public class Calculus
	{
		#region
		/// <summary>
		/// This function uses the Multiple-Segment Trapezoidal rule
		/// to numerically integrate a function over an interval.
		/// This method has been tweaked to do this for a matrix
		/// </summary>
		/// <param name="a">left bound of the integration</param>
		/// <param name="b">right bound of integration</param>
		/// <param name="n">number of segments the function is broken up into</param>
		/// <param name="f">IMatrixFunction which gives a matrix as a function of a variable, x</param>
		/// <returns>matrix of integrals</returns>
		#endregion
		public static double[,] TrapezoidMatrix(double a, double b, int n, IMatrixFunctionOneVariable f)
		{
			
			double [,] sum; //sum of the f(x) values
			double x;  //value of x at each interval
			double [,] integral; //the answer
			double h;  //segment size
			
			
			h = (b-a)/n;
			x = a;
			
			sum = f.Eval(x);
			
			for (int i = 0; i < n-1; i++){
				x = x+h;
				sum = MatrixMath.Add(sum, MatrixMath.ScalarMultiply(2d,f.Eval(x)));
			}
			
			//add the last value
			sum = MatrixMath.Add(sum, f.Eval(b));
			
			integral = MatrixMath.ScalarMultiply((b-a)/(2*n),sum);
			
			return integral;
		}
		#region
		/// <summary>
		/// Same as normal trapezoid matrix, but this one can take an input list filled with x and [,]
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="n"></param>
		/// <param name="f"></param>
		/// <returns></returns>
		#endregion
		public static double[,] TrapezoidMatrix(double a, double b, int n, IMatrixFunctionOneVariable f, ref List<double> lx, ref List<double [,]> lFofX)
		{
			
			double [,] sum; //sum of the f(x) values
			double x;  //value of x at each interval
			double [,] integral; //the answer
			double h;  //segment size
			
			h = (b-a)/n;
			x = a;
			
			double [,] fx = FindFx(f, ref lx, ref lFofX, x);
			
			sum = fx;
			
			for (int i = 0; i < n-1; i++){
				x = x+h;
				//First, see if it's in the list
				fx = FindFx(f, ref lx, ref lFofX, x);
				sum = MatrixMath.Add(sum, MatrixMath.ScalarMultiply(2d,fx));
			}
			
			//add the last value
			fx = FindFx(f, ref lx, ref lFofX, b);
			sum = MatrixMath.Add(sum, fx);
			
			integral = MatrixMath.ScalarMultiply((b-a)/(2*n),sum);
			
			return integral;
		}
		
		public static double[] TrapezoidMatrix(double a, double b, int n, IVectorFunctionOneVariable f, ref List<double> lx, ref List<double []> lFofX)
		{
			
			double [] sum; //sum of the f(x) values
			double x;  //value of x at each interval
			double [] integral; //the answer
			double h;  //segment size
			
			
			h = (b-a)/n;
			x = a;
			double [] fx = FindFx(f, ref lx, ref lFofX, x);
			sum = fx;
			
			for (int i = 0; i < n-1; i++){
				x = x+h;
				fx = FindFx(f, ref lx, ref lFofX, x);
				sum = VectorMath.Add(sum, VectorMath.ScalarMultiply(2d,fx));
			}
			
			//add the last value
			fx = FindFx(f, ref lx, ref lFofX, b);
			sum = VectorMath.Add(sum, fx);
			
			integral = VectorMath.ScalarMultiply((b-a)/(2*n),sum);
			
			return integral;
		}
		
		public static double[] TrapezoidMatrix(double a, double b, int n, IVectorFunctionOneVariable f)
		{
			
			double [] sum; //sum of the f(x) values
			double x;  //value of x at each interval
			double [] integral; //the answer
			double h;  //segment size
			
			
			h = (b-a)/n;
			x = a;
			
			sum = f.Eval(x);
			
			for (int i = 0; i < n-1; i++){
				x = x+h;
				sum = VectorMath.Add(sum, VectorMath.ScalarMultiply(2d,f.Eval(x)));
			}
			
			//add the last value
			sum = VectorMath.Add(sum, f.Eval(b));
			
			integral = VectorMath.ScalarMultiply((b-a)/(2*n),sum);
			
			return integral;
		}
		
		public static double[] RhombergMatrix(double a, double b, int maxit, double es, IVectorFunctionOneVariable f)
		{
			
			double [,][] I = new double [maxit+1,maxit+1][]; // matrix storing results
			int n = 1;    //number of divisions for TrapezoidalRule
			int iter = 0; //current iteration
			double ea;  //actual current percent relative error of each entry in matrix
			int flag = 0;  //flag indicating whether the maxit or es was reached
			double dum; //4^k-1
			int k, j;
			List<double> lx = new List<double>();
			List<double []> lFofX = new List<double[]>();
			
			I[0,0] = TrapezoidMatrix(a,b,n,f, ref lx, ref lFofX); //initiate first occurance of TrapRule
			
			while (flag == 0) {
				iter = iter+1; //count iterations
				n =System.Convert.ToInt32(System.Math.Pow(2,iter)); //define number of segments for Trap
				I[iter,0] = TrapezoidMatrix(a,b,n,f, ref lx, ref lFofX);
				
				
				for (k = 2; k < iter+2; k++){
					j = 2 + iter - k;
					dum = System.Math.Pow(4,k-1);
					double [] temp1 = VectorMath.ScalarMultiply(dum, I[j,k-2]);
					double [] temp2 = VectorMath.Subtract(temp1, I[j-1,k-2]);
					double scalar = 1.0/(dum-1.0);
					I[j-1,k-1] = VectorMath.ScalarMultiply(scalar,temp2);
					
				}
				
				#region Old way of handling error (problem with 0 or near 0 values)
				/*
				ea = VectorMath.RelError(I[0,iter],I[0,iter-1]); //relative error
				
				//Tells whether all of the errors are below the minimum error
				errorBelow = true;
				foreach (double d in ea){
					if(d > es){
						errorBelow = false;
					}
				}*/
				#endregion
				double currentSum = VectorMath.AbsVectorSum(I[0,iter]);
				double prevSum = VectorMath.AbsVectorSum(I[0,iter-1]);
				ea = Math.Abs(currentSum - prevSum) / prevSum * 100.0;
				
				if(ea <= es){
					flag = 1; //trips the flag if error is within tolerance
				}
				
				if(iter >= maxit){
					flag = 1; //trips the flag if maxit is reached
					//throw new ArgumentException("Integration did not converge");
					
				}
			}
			return I[0,iter]; //returns the final integration value
			
		}
		#region
		/// <summary>
		/// This function performs Romberg Integration
		/// (Pg 612 "Numerical Methods for Engineers")
		/// uses multiple TrapezoidRule estimations to
		/// calculate a more correct approximation
		/// </summary>
		/// <param name="a">left bound of the integration</param>
		/// <param name="b">right bound of integration</param>
		/// <param name="maxit">maximum number of iterations (# of TrapezoidalRule usages)</param>
		/// <param name="es">minimum error allowed (percent relative error)</param>
		/// <param name="f">instance of Deliverables to get the matrix out of</param>
		/// <returns> Matrix of integrals</returns>
		#endregion
		public static double[,] RhombergMatrix(double a, double b, int maxit, double es, IMatrixFunctionOneVariable f)
		{
			
			double [,][,] I = new double [maxit+1,maxit+1][,]; // matrix storing results
			int n = 1;    //number of divisions for TrapezoidalRule
			int iter = 0; //current iteration
			double ea;  //actual current percent relative error of each entry in matrix
			int flag = 0;  //flag indicating whether the maxit or es was reached
			double dum; //4^k-1
			int k, j;
			List<double> lx = new List<double>();
			List<double [,]> lFofX = new List<double[,]>();
			
			I[0,0] = TrapezoidMatrix(a,b,n,f, ref lx, ref lFofX); //initiate first occurance of TrapRule
			
			while (flag == 0) {
				iter = iter+1; //count iterations
				n =System.Convert.ToInt32(System.Math.Pow(2,iter)); //define number of segments for Trap
				I[iter,0] = TrapezoidMatrix(a,b,n,f, ref lx, ref lFofX);
				
				
				for (k = 2; k < iter+2; k++){
					j = 2 + iter - k;
					dum = System.Math.Pow(4,k-1);
					double [,] temp1 = MatrixMath.ScalarMultiply(dum, I[j,k-2]);
					double [,] temp2 = MatrixMath.Subtract(temp1, I[j-1,k-2]);
					double scalar = 1.0/(dum-1.0);
					I[j-1,k-1] = MatrixMath.ScalarMultiply(scalar,temp2);
					
				}
				
				#region Old way of handling error (problem with 0 or near 0 values)
				/*
				ea = MatrixMath.RelError(I[0,iter],I[0,iter-1]); //relative error
				
				//Tells whether all of the errors are below the minimum error
				errorBelow = true;
				foreach (double d in ea){
					if(d > es){
						errorBelow = false;
					}
				}*/
				#endregion
				double currentSum = MatrixMath.AbsMatrixSum(I[0,iter]);
				double prevSum = MatrixMath.AbsMatrixSum(I[0,iter-1]);
				ea = Math.Abs(currentSum - prevSum) / prevSum * 100.0;
				
				if(ea <= es){
					flag = 1; //trips the flag if error is within tolerance
				}
				
				if(iter >= maxit){
					flag = 1; //trips the flag if maxit is reached
					//throw new ArgumentException("Integration did not converge");
					
				}
			}
			return I[0,iter]; //returns the final integration value
			
		}
		#region
		/// <summary>
		/// This performs double integration by using two different IMatrixFunctionOneVariable functions.  The first one evaluates the function at a certain x1 and x2
		/// while the second one integrates in x2 with a fixed x1.
		/// </summary>
		/// <param name="a1">initial bound of x1</param>
		/// <param name="b1">initial bound of x2</param>
		/// <param name="a2">final bound of x1</param>
		/// <param name="b2">final bound of x2</param>
		/// <param name="maxit">maximum iterations per rhombus iteration</param>
		/// <param name="error">minimum error allowed (percent relative error)</param>
		/// <param name="f">function of two variables</param>
		/// <returns>the double integral</returns>
		#endregion
		public static double[,] DoubleIntegration(double a1, double a2, double b1, double b2, int maxit, double error, IMatrixFunctionTwoVariables f){
			
			TwoVariableIntegration twoVar = new TwoVariableIntegration(f, b1, b2, maxit, error);
			double [,] doubleInt = RhombergMatrix(a1, a2, maxit, error, twoVar);
			return doubleInt;
		}
		
		#region Old Versions (Commented Out)
		/*
		#region
		/// <summary>
		/// This function uses the Multiple-Segment Trapezoidal rule
		/// to numerically integrate a function over an interval.
		/// This method has been tweaked to do this for a matrix
		/// </summary>
		/// <param name="a">left bound of the integration</param>
		/// <param name="b">right bound of integration</param>
		/// <param name="n">number of segments the function is broken up into</param>
		/// <param name="f">instance of Deliverables to get the matrix out of</param>
		/// <returns>matrix of integrals</returns>
		#endregion
		public static double[,] TrapezoidMatrix(double a, double b,
		                                     int n, Deliverables f)
		{
						
			double [,] sum; //sum of the f(x) values
			double x;  //value of x at each interval
			double [,] integral; //the answer
			double h;  //segment size
			
			
			h = (b-a)/n;
			x = a;
			
			sum = f.WithinEnergyIntegral(x);
			
			for (int i = 0; i < n-1; i++){
				x = x+h;
				sum = MatrixMath.Add(sum, MatrixMath.ScalarMultiply(2d,f.WithinEnergyIntegral(x)));
			}
			
			//add the last value
			sum = MatrixMath.Add(sum, f.WithinEnergyIntegral(b));
			
			integral = MatrixMath.ScalarMultiply((b-a)/(2*n),sum);
			
			return integral;
		}
		
		#region
		/// <summary>
		/// This function performs Romberg Integration
		/// (Pg 612 "Numerical Methods for Engineers")
		/// uses multiple TrapezoidRule estimations to
		/// calculate a more correct approximation
		/// </summary>
		/// <param name="a">left bound of the integration</param>
		/// <param name="b">right bound of integration</param>
		/// <param name="maxit">maximum number of iterations (# of TrapezoidalRule usages)</param>
		/// <param name="es">minimum error allowed (percent relative error)</param>
		/// <param name="f">instance of Deliverables to get the matrix out of</param>
		/// <returns> Matrix of integrals</returns>
		#endregion
		public static double[,]  RhombergMatrix(double a, double b, int maxit,
		                                double es, Deliverables f)
		{
			
			double [,][,] I = new double [maxit+1,maxit+1][,]; // matrix storing results
			int n = 1;    //number of divisions for TrapezoidalRule
			int iter = 0; //current iteration
			double [,] ea;  //actual current percent relative error of each entry in matrix
			int flag = 0;  //flag indicating whether the maxit or es was reached
			double dum; //4^k-1
			bool errorBelow;
			int k, j;
			
			I[0,0] = TrapezoidMatrix(a,b,n,f); //initiate first occurance of TrapRule
			
			while (flag == 0) {
				iter = iter+1; //count iterations
				n =System.Convert.ToInt32(System.Math.Pow(2,iter)); //define number of segments for Trap
				I[iter,0] = TrapezoidMatrix(a,b,n,f);
				
				
				for (k = 2; k < iter+2; k++){
					j = 2 + iter - k;
					dum = System.Math.Pow(4,k-1);
					double [,] temp1 = MatrixMath.ScalarMultiply(dum, I[j,k-2]);
					double [,] temp2 = MatrixMath.Subtract(temp1, I[j-1,k-2]);
					double scalar = 1.0/(dum-1.0);
					I[j-1,k-1] = MatrixMath.ScalarMultiply(scalar,temp2);
					                                       
				}
				
				ea = MatrixMath.RelError(I[0,iter],I[0,iter-1]); //relative error
				
				//Tells whether all of the errors are below the minimum error
				errorBelow = true;
				foreach (double d in ea){
					if(d > es){
						errorBelow = false;
					}
				}
				if(errorBelow){
					flag = 1; //trips the flag if error is within tolerance
				}
				if(iter >= maxit){
					flag = 1; //trips the flag if maxit is reached
					 //throw new ArgumentException("Integration did not converge");
					 
				}
			}
			return I[0,iter]; //returns the final integration value
			
		}
		 */
		#endregion
		
		public static double [,] FindFx(IMatrixFunctionOneVariable f, ref List<double> lx, ref List<double [,]> lFofX, double x){
			int indX = FindX(lx, x);
			double [,] fx;
			if (indX != -1) {
				fx = lFofX[indX];
			}
			else{
				fx = f.Eval(x);
				lx.Add(x);
				lFofX.Add(fx);
			}
			return fx;
		}
		public static double [] FindFx(IVectorFunctionOneVariable f,ref List<double> lx, ref List<double []> lFofX, double x){
			int indX = FindX(lx, x);
			double [] fx;
			if (indX != -1) {
				fx = lFofX[indX];
			}
			else{
				fx = f.Eval(x);
				lx.Add(x);
				lFofX.Add(fx);
			}
			return fx;
		}
		public static int FindX(List<double> lx, double x){
			for (int i = 0; i < lx.Count; i++) {
				if (Math.Abs(x-lx[i]) <= Math.Abs(x*0.0000001)) {
					return i;
				}
			}
			return (-1);
		}
		#region Private Methods
		
		#endregion
		
	}
	
	
	public class TwoToOneVariableFunction: IMatrixFunctionOneVariable
	{
		private IMatrixFunctionTwoVariables twoVarFunc;
		private bool isConstantX1;
		private double ConstantValue;
		public TwoToOneVariableFunction(IMatrixFunctionTwoVariables TwoVarFunc){
			twoVarFunc = TwoVarFunc;
		}
		public void MakeX1Constant(double x1){
			ConstantValue = x1;
			isConstantX1 = true;
		}
		public void MakeX2Constant(double x2){
			ConstantValue = x2;
			isConstantX1 = false;
		}
		public double [,] Eval(double x){
			if (isConstantX1) {
				return twoVarFunc.Eval(ConstantValue, x);
			}
			return twoVarFunc.Eval(x, ConstantValue);
		}
		
	}
	
	public class TwoVariableIntegration: IMatrixFunctionOneVariable
	{
		private IMatrixFunctionTwoVariables twoVarFunc;
		private TwoToOneVariableFunction oneVarFunc;
		private double B1, B2;
		private int Maxit;
		private double Error;
		
		public TwoVariableIntegration(IMatrixFunctionTwoVariables twoVar, double b1, double b2,
		                              int maxit, double error){
			B1 = b1;
			B2 = b2;
			Maxit = maxit;
			Error = error;
			twoVarFunc = twoVar;
			oneVarFunc = new TwoToOneVariableFunction(twoVar);
		}
		#region
		/// <summary>
		/// Input x1: this returns the integral of the function at x1 over x2
		/// </summary>
		/// <param name="x">x1 value</param>
		/// <returns>the integral of the function at x1 over x2</returns>
		#endregion
		public double [,] Eval(double x){
			
			oneVarFunc.MakeX1Constant(x);
			
			double [,] intOverX2 = Calculus.RhombergMatrix(B1, B2, Maxit, Error, oneVarFunc);
			
			return intOverX2;
		}
		
	}
	
	public interface IMatrixFunctionOneVariable{
		double [,] Eval(double x);
	}
	public interface IMatrixFunctionTwoVariables{
		double [,] Eval(double x1, double x2);
	}
	public interface IVectorFunctionOneVariable{
		double [] Eval(double x);
	}
	
}
