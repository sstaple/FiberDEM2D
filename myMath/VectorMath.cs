/*
 * Created by SharpDevelop.
 * User: Admin
 * Date: 9/9/2009
 * Time: 10:54 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace myMath
{
	/// <summary>
	/// Description of VectorMath.
	/// </summary>
	public class VectorMath
	{
		public VectorMath()
		{
		}
		
		///Returns the Euclidian norm of an array
		public static double Norm(double[] a)
		{
			
			int n = a.Length;
			double dNorm = 0;
			
			for (int i = 0; i < n; i++) {
				
				dNorm = dNorm + Math.Pow(a[i], 2);
				
			}
			
			dNorm = Math.Sqrt(dNorm);
			
			return dNorm;
		}

		public static void NormalizeVector(ref double[] vector)
		{
			//takes in a vector, and normalizes it
			double dLength = 0;

			foreach (double d in vector)
			{
				dLength += Math.Pow(d, 2);
			}
			dLength = Math.Sqrt(dLength);

			//now reassign
			if (!dLength.Equals(0.0)) { 
				for (int i = 0; i < vector.Length; i++)
				{
					vector[i] = vector[i] / dLength;
				}
			}
		}
		
		#region
		/// <summary>
		/// Stacks two vectors: top on top, and bottom on bottom, obviously.
		/// </summary>
		#endregion
		public static double[] Stack(double[] top, double[] bottom)
		{
			int nTop = top.Length;
			int nBottom = bottom.Length;
			
			double[] total = new double[nTop + nBottom];
			
			for (int i = 0; i < nTop + nBottom; i++) {
				
				if (i < nTop) {
					total[i] = top[i];
				} else {
					total[i] = bottom[i - nTop];
				}
				
			}
			return total;
		}
		
		public static double Dot(double[] A, double[] B)
		{
			if (A.Length != B.Length) {
				
				throw new ArgumentException("Vectors must be same dimentsions");
			}
			double C = 0;
			
			for (int i = 0; i < A.Length; i++) {
				
				C += A[i] * B[i];
				
			}
			
			return C;
		}
		
		public static double [,] Cross(double[] A, double[] B)
		{
			int n = A.Length;
			int m = B.Length;
			
			double[,] C = new double[n, m];
			
			for (int i = 0; i < n; i++) {
				
				for (int j = 0; j < m; j++) {
					
					C[i, j] += A[i] * B[j];
				}
			}
			
			return C;
		}
		
		/// <summary>
		/// Cross product of two vectors: gives a vector perpendicular to both A and B
		/// </summary>
		public static double[] VectorProduct(double[] A, double[] B)
		{
			if (A.Length != 3 && B.Length != 3)
			{

				throw new ArgumentException("Vectors must be of dimension 3");
			}

			double[] C = new double[3];

			C[0] = A[1] * B[2] - A[2] * B[1];
			C[1] = A[2] * B[0] - A[0] * B[2];
			C[2] = A[0] * B[1] - A[1] * B[0];

			return C;
		}

		public static double [] ScalarMultiply(double x, double[] A)
		{
			int n = A.Length;
			
			double[] C = new double[n];
			
			for (int i = 0; i < n; i++) {
				C[i] = A[i] * x;
			}
			return C;
		}
		
		public static double [] Add(double[] A, double[] B)
		{
			int n = A.Length;
			
			double[] C = new double[n];
			
			for (int i = 0; i < n; i++) {
				C[i] = A[i] + B[i];
			}
			return C;
		}
		
		public static double [] Subtract(double[] A, double[] B)
		{
			int n = A.Length;
			
			double[] C = new double[n];
			
			for (int i = 0; i < n; i++) {
				C[i] = A[i] - B[i];
			}
			return C;
		}
		
		public static void CopyToVector(ref double[] MasterVector, double[] VectorToCopy, int startIndex)
		{
			
			int l1 = MasterVector.Length;
			int l2 = VectorToCopy.Length;
			
			if (l2 + startIndex > l1) {
				
				throw new ArgumentException("Copy is outside of master vector dimensions");
			}
			
			for (int i = 0; i < l2; i++) {
				
				MasterVector[startIndex + i] = VectorToCopy[i];
			}
		}
		/// <summary>
		/// Returns a vector extracted out of A
		/// </summary>
		/// <param name="A">Master vector</param>
		/// <param name="start">start index</param>
		/// <param name="end">Last index (length will be "start - end + 1"</param>
		/// <returns>vector extracted out of A, including start and end</returns>
		public static double [] ExtractVector(double[] A, int start, int end)
		{
			int n = end - start + 1;
			if (((n <= 0 || start < 0) || (end >= A.Length))) {
				throw new ArgumentException("Check Inputs");
			}
			double[] NewVector = new double[n];
			for (int i = 0; i < n; i++) {
				NewVector[i] = A[start + i];
			}
			return NewVector;
		}
		
		public static void AddToVector(ref double[] MasterVector, double[] VectorToCopy, int startIndex)
		{
			
			int l1 = MasterVector.Length;
			int l2 = VectorToCopy.Length;
			
			if (l2 + startIndex > l1) {
				
				throw new ArgumentException("Copy is outside of master vector dimensions");
			}
			
			for (int i = 0; i < l2; i++) {
				
				MasterVector[startIndex + i] += VectorToCopy[i];
			}
		}
		
		public static double [] RelError(double[] A, double[] B)
		{
			int n = A.Length;
			int m = B.Length;
			double[] relError = new double[n];
			
			if (n != m) {
				
				throw new ArgumentException("Matrices must be same dimentsions");
			}
			
			for (int j = 0; j < n; j++) {
				relError[j] = System.Math.Abs((A[j] - B[j]) / A[j] * 100);
				if ((A[j] - B[j]) == 0) {
					relError[j] = 0.0;
				}
			}
			return relError;
		}
		#region
		///<summary>
		/// This method gets rid of relatively small numbers and makes them zero.
		/// The input zeroError is the % of the maximum value in the matrix that
		/// becomes zero
		/// </summary>
		#endregion
		
		#region
		/// <summary>
		/// Puts a 2-D array into a 1-D array
		/// </summary>
		/// <param name="A"></param>
		/// <returns></returns>
		#endregion
		public static double [] Array2DToArray1D(double[,] A)
		{
			
			int n = A.Length;
			double[] C = new double[n];
			int i = 0;
			
			foreach (double d in A) {
				C[i] = d;
				i++;
			}
			
			return C;
		}
		
		public static double [,] Array1DToArray2D(double[] A, bool isHorizontal)
		{
			
			int n = A.Length;
			if (isHorizontal) {
				double[,] C = new double[1, n];
				for (int i = 0; i < n; i++) {
					C[0, i] = A[i];
				}
				return C;
			} else {
				double[,] C = new double[n, 1];
				for (int i = 0; i < n; i++) {
					C[i, 0] = A[i];
				}
				return C;
			}
			
			
		}
		
		/// <summary>
		/// This inserts a vector, b, into a vector, a after the index i.  If i is less then 0, b is inserted
		/// before A.  if i is the last index or greater, then b is inserted after a.  
		/// </summary>
		/// <param name="A">master vector</param>
		/// <param name="i">index which will be before the inserted vector b</param>
		/// <param name="b">vector to insert</param>
		/// <returns>a new vector</returns>
		public static double [] InsertRange_After_i(double[] A, int i, double[] b)
		{
			
			if (i < 0) {
				return Stack(b, A);
			} else if (i >= A.Length - 1) {
				return Stack(A, b);
			} else {
				double[] Aabove = ExtractVector(A, 0, i);
				double[] Abelow = ExtractVector(A, i + 1, A.Length - 1);
				return Stack(Stack(Aabove, b), Abelow);
			}
			
		}
		
		public static double [] SwapEntries(double[] A, int i1, int i2)
		{
			
			double[] swapped = ExtractVector(A, 0, A.Length - 1); //Create Deep copy of A
			swapped[i1] = A[i2];
			swapped[i2] = A[i1];
			
			return swapped;
		}
		#region
		/// <summary>
		/// This swaps sections of an array with each other. The sections must not be the same size
		/// </summary>
		/// <param name="A">arrau</param>
		/// <param name="iStart1">starting index of section 1</param>
		/// <param name="iEnd1">ending index of section 1</param>
		/// <param name="iStart2">starting index of section 2</param>
		/// <param name="iEnd2">ending index of section 2</param>
		/// <returns>array with swapped sections</returns>
		#endregion
		public static double [] SwapSections(double[] A, int iStart1, int iEnd1, int iStart2, int iEnd2)
		{
			
			double[] Beginning, Middle, End;
			
			double[] temp1 = ExtractVector(A, iStart1, iEnd1);
			double[] temp2 = ExtractVector(A, iStart2, iEnd2);
			
			if (iStart1 > 0) { //If the first entry isn't the start entry
				Beginning = ExtractVector(A, 0, iStart1 - 1);
				temp2 = Stack(Beginning, temp2);
			}
			if (iStart2 - iEnd1 > 1) { //If there is space between the two sections
				Middle = ExtractVector(A, iEnd1 + 1, iStart2 - 1);
				temp2 = Stack(temp2, Middle);
			}
			if (iEnd2 < A.Length - 1) { //If the last row isn't the end row
				End = ExtractVector(A, iEnd2 + 1, A.Length - 1);
				temp1 = Stack(temp1, End);
			}
			return Stack(temp2, temp1);
		}

		public static double[] DeepCopy(double[] A)
		{

			double[] c = new double[A.Length];
			for (int i = 0; i < A.Length; i++)
			{
				c[i] = A[i];
			}
			return c;
		}

		public static double AbsVectorSum(double[] A)
		{
			double sum = 0;
			foreach (double d in A) {
				sum += Math.Abs(d);
			}
			return sum;
		}
	
		public static double MaxAbsVal(double[] A)
		{
			double largest = 0;
			for (int i = 0; i < A.Length; i++) {
				if (largest > Math.Abs(A[i])) {
					largest = Math.Abs(A[i]);
				}
			}
			return largest;
		}
	}
}
