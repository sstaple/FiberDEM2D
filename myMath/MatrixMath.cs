/*
 * Created by SharpDevelop.
 * User: e46221
 * Date: 6/13/2007
 * Time: 2:42 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Diagnostics;
using Mapack;
//using MathNet.Numerics.LinearAlgebra;
//using MathNet.Numerics.LinearAlgebra;




namespace myMath
{
	/// <summary>
	/// Some run off of the MAPACK library, most are hand-written
	/// </summary>
	
	public class MatrixMath
	{
		public bool IsInt(string s)
		{
			try
			{
				int.Parse(s);
			}
			catch
			{
				return false;
			}
			return true;
		}

		public static double[,] InvertTransposed3b3(double[,] a)
		{

			#region Programmed by me, very ugly!!

			int aRows = a.GetLength(0);
			int aCols = a.GetLength(1);
			double[,] result = new double[3, 3];

			if (aRows != 3 || aCols != 3)
			{   // A is singular
				throw new Exception("A must be 3 b 3 for this method!");
			}

			double DetA = a[0, 0] * (a[2, 2] * a[1, 1] - a[2, 1] * a[1, 2])
				- a[1, 0] * (a[2, 2] * a[1, 0] - a[2, 1] * a[0, 2])
				+ a[2, 0] * (a[1, 2] * a[0, 1] - a[1, 1] * a[0, 2]);
			if (Math.Abs(DetA) < 0.00000001)
			{   // A is singular
				throw new Exception("A is singlular buddy.  What are you trying to pull here....");
			}
			double iDetA = 1.0 / DetA;
			result[0, 0] = (a[1, 1] * a[2, 2] - a[2, 1] * a[1, 2]) * iDetA;
			result[1, 0] = -(a[0, 1] * a[2, 2] - a[0, 2] * a[2, 1]) * iDetA;
			result[2, 0] = (a[0, 1] * a[1, 2] - a[0, 2] * a[1, 1]) * iDetA;
			result[0, 1] = -(a[1, 0] * a[2, 2] - a[1, 2] * a[2, 0]) * iDetA;
			result[1, 1] = (a[0, 0] * a[2, 2] - a[0, 2] * a[2, 0]) * iDetA;
			result[2, 1] = -(a[0, 0] * a[1, 2] - a[1, 0] * a[0, 2]) * iDetA;
			result[0, 2] = (a[1, 0] * a[2, 1] - a[2, 0] * a[1, 1]) * iDetA;
			result[1, 2] = -(a[0, 0] * a[2, 1] - a[2, 0] * a[0, 1]) * iDetA;
			result[2, 2] = (a[0, 0] * a[1, 1] - a[1, 0] * a[0, 1]) * iDetA;
			#endregion

			return result;
		}
		
		public static double[,] Multiply(double[,] a, double[,] b){
			
			#region Using Mapack
			//First, convert the double array to a Matrix from the Mapack namespace
			Mapack.Matrix aMatrix = ArrayToMatrix(a);
			Mapack.Matrix bMatrix = ArrayToMatrix(b);
			
			Mapack.Matrix cMatrix = aMatrix * bMatrix;
			
			double [,] c = MatrixToArray(cMatrix);
			#endregion
			
			#region Programmed by me
			/*multiply two matrices a,b.  Puts them into new matrix, c,
			 * which is returned
			 * input:
			 * 	a - lxn matrix
			 *  b - nxm matrix
			 * output:
			 *  c - lxm matrix
			 * */
			
			/*int aRows = a.GetLength(0);
			int bCols = b.GetLength(1);
			int bRows = b.GetLength(0);
			double [,] c = new double [aRows,bCols];
			int i,j,k;
			
			
					
			for (j = 0; j < bCols; j++){
				for (k = 0; k < bRows; k++){
					if (b[k,j] != 0.0) {
						for (i = 0; i < aRows; i++){
							if(a[i,k] != 0.0){
								c[i, j] += a[i,k] * b[k,j];
							}
						}
					}
					
				}
			}*/
			#endregion
			
			return c;
		}

		public static double[] Multiply(double[,] a, double[] b){
			
			#region Using Mapack
			//First, convert the double array to a Matrix from the Mapack namespace
			Mapack.Matrix aMatrix = ArrayToMatrix(a);
			Mapack.Matrix bMatrix = ArrayToMatrix(b);
			
			Mapack.Matrix cMatrix = aMatrix * bMatrix;
			
			double [] c = MatrixToVectorArray(cMatrix);
			#endregion
			
			#region Programmed by me
			
			/*multiply a matrix a and a vector b.  Puts them into new vector c,
			 * which is returned
			 * input:
			 * 	a - mxn matrix
			 *  b - n array
			 * output:
			 *  c - m array
			 * */
			
			/*int aRows = a.GetLength(0);
			int bRows = b.GetLength(0);
			double [] c = new double [aRows];
			int i,j;
			
			for (i = 0; i < aRows; i++){
				for (j = 0; j < bRows; j++){
					c[i] += a[i,j] * b[j];
					
				}
			}*/
			#endregion
			
			return c;
		}
		
		public static double[] Multiply(double[] a, double[,] b){
			
			#region Using Mapack
			//First, convert the double array to a Matrix from the Mapack namespace
			Mapack.Matrix aMatrix = VectorTransposeArrayToMatrix(a);
			Mapack.Matrix bMatrix = ArrayToMatrix(b);
			
			Mapack.Matrix cMatrix = aMatrix * bMatrix;
			
			double [] c = MatrixToVectorArray(cMatrix);
			#endregion
			
			return c;
		}
		
		public static double Determinant(double [,] a){
			Mapack.Matrix mA = ArrayToMatrix(a);
			return mA.Determinant;
		}
		
		public static double Trace(double [,] a)
        {
			double sum = 0.0;
            for (int i = 0; i < a.GetLength(0); i++)
            {
				sum += a[i, i];
            }
			return sum;
        }
		public static double[] LinSolve(double[,] A, double[] B){
			
			#region Using Mapack
			//First, convert the double array to a Matrix from the Mapack namespace
			Mapack.Matrix aMatrix = ArrayToMatrix(A);
			Mapack.Matrix bMatrix = ArrayToMatrix(B);
			
			Mapack.Matrix cMatrix = aMatrix.Solve(bMatrix);
			
			double [] c = MatrixToVectorArray(cMatrix);
			#endregion
			
			return c;
		}
/*
		/// <summary>
		/// Solve a system of linear equations (B = A * x) using Pardiso.  Best for smaller systems, especially sparse matrices
		/// </summary>
		/// <param name="A">Coefficient matrix</param>
		/// <param name="B">b vector</param>
		/// <returns>Returns x, for B = A * x</returns>
		public static double[] LinSolvePardiso(double[,] A, double[] B)
		{

			#region Using pardiso

			var aMatrix = Matrix<double>.Build.DenseOfArray(A);
			var bVector = Vector<double>.Build.DenseOfArray(B);

			var c = aMatrix.Solve(bVector);

			double[] C = c.ToArray();
			#endregion

			return C;
		}
*/
		public static void Gauss(double[,] A, double[,] B, int n, int C){
			/*L.A. Riddle 09/07/91
		This function solves simultaneous linear equations using Gauss
		Elimination with Scaled Partial Pivoting
		(ref pg 139, "Numerical Methods, Software and Analysis" by John R. Rice.)
		A (n,n) = the coefficient matrix (gets mangled)
		B (n,c) = the right hand matrix
		(A) X = B
		X (1,n), is returned as pointer to B
		c = 0 for a column vector, 0 - n for n x n matrix...use successive calls
			 */
			
			int i; //Dim i As Integer
			int j; //Dim j As Integer
			int k; //Dim k As Integer
			int imax; //Dim imax As Integer
			double R; //Dim R As Double
			double S; //Dim S As Double
			double m; //Dim m As Double
			double buffer; //Dim buffer As Double
			double[,] scaleFactors = new double[n,1]; //Dim scaleFactors(6, 1) As Double: was 3 where n is  SES
			

			//first, make a string of A so that I can send it out if there is a problem
			string ACopy = "";
			for (i = 0; i < n; i++) {
				ACopy += "[";
				for (j = 0; j < n; j++) {
					ACopy += A[j,i] + ", ";
				}
				ACopy += "],";
			}
			
			//initialize:
			//TODO: (LAR) get rid of magic numbers:
			buffer = 0.000000001;
			imax = 0;
			// TODO:(LAR) determine what to do with optional arguement C:
			// initialize to zero for now...
			//C = 0; //This term limited the solution to a column matrix SES
			for (i = 0; i<n;i++){
				scaleFactors[i, 0] = 0.0;
				for (j = 0; j<n;j++){
					if(Math.Abs(A[i,j])>scaleFactors[i,0]){
						scaleFactors[i,0] = Math.Abs(A[i,j]);
					}
				}
			}
			for (k = 0; k<n-1; k++){	// loop over columns of A
				S = 0.0;	// reset check value
				for (i= k; i<n; i++){	// loop over rows   '
					if(Math.Abs(A[i,k]/scaleFactors[i,0])>S){ 		// look for the
						S = Math.Abs(A[i,k]/scaleFactors[i,0]);		// maximum element
						imax = i;									// in column k                                        '
					}
				}
				if(Math.Abs(A[imax,k]) <= buffer){
					goto End_of_k_loop;
				}

				if(imax == k) {
					goto Calculate_m;// row does not need interchanged
				}
				for(j = 0; j<n; j++){ // interchange rows k and imax
					R = A[k,j];
					A[k,j] = A[imax,j];
					A[imax,j] = R;
				}
				R = B[k,C];				// interchange corresponding right hand sides
				B[k,C] = B[imax,C];
				B[imax,C] = R;

				Calculate_m:
					for(i = k + 1; i<n; i++){
					A[i,k] = A[i,k]/A[k,k];		// compute multiplier
					m = A[i,k];					// assign it to m
					for(j = k + 1; j<n; j++){ // loop over elements
						A[i,j] = A[i,j] - m*A[k,j];	// of row i
					}
					B[i,C] = B[i,C]-m*B[k,C];	// do the right hand side
				}
				End_of_k_loop:

					; // don't know why we need the ;...
			}
			// Back substitution
			i = n-1;

			while(i>=0){	// loop backwards over rows of A
				R = 0.0;
				for(j = i+1; j<n; j++){
					R += A[i,j] * B[j,C]; // sum up known values
				}
				R = B[i,C] - R;
				if(Math.Abs(A[i,i])>buffer){
					B[i,C] = R / A[i,i];
				}
				else if(Math.Abs(R) <=buffer) {	// A is singular
					// system is consistent
					Debug.Write("A matrix: " + ACopy);
					throw new Exception("A is Singluar, system is consistent, and could not solve system");
				}
				else {
					throw new Exception("A is Singluar, system is inconsistent, and could not solve system");
				}
				i -= 1;
			} //Wend
			
		} // end gauss
		
		public static double[] Gauss(double[,] inA, double[] inB){
//		L.A. Riddle 09/07/91
//		This function solves simultaneous linear equations using Gauss
//		Elimination with Scaled Partial Pivoting
//		(ref pg 139, "Numerical Methods, Software and Analysis" by John R. Rice.)
//		A (n,n) = the coefficient matrix (gets mangled)
//		B (n) = the right hand matrix
//		(A) X = B
//		X (1,n), is returned
//
//		//TODO: (LAR) fix error handling for guass subroutine
//		Return ERROR for singular matrix, consistent system
//		or singular matrix, inconsistent system

			
			int n = inA.GetLength(0);
			int i;
			int j;
			int k;
			int imax;
			double R;
			double S;
			double m;
			double buffer; //Dim buffer As Double
			double[,] scaleFactors = new double[n,1];
			int errorFlag;
			
			//make these so that inA and inB don't get mangled
			double [] B = new double[n];
			double [,] A = new double[n,n];
			
			for (i = 0; i < n; i++) {
				B[i] = inB[i];
				for (j = 0; j < n; j++) {
					A[i,j] = inA[i,j];
				}
			}
			
			
			//initialize:
			//TODO: (LAR) get rid of magic numbers:
			errorFlag = 0;
			buffer = 0.000000001;
			imax = 0;
			
			for (i = 0; i<n;i++){
				scaleFactors[i, 0] = 0.0;
				for (j = 0; j<n;j++){
					if(Math.Abs(A[i,j])>scaleFactors[i,0]){
						scaleFactors[i,0] = Math.Abs(A[i,j]);
					}
				}
			}
			for (k = 0; k<n-1; k++){	// loop over columns of A
				S = 0.0;	// reset check value
				for (i= k; i<n; i++){	// loop over rows   '
					if(Math.Abs(A[i,k]/scaleFactors[i,0])>S){ 		// look for the
						S = Math.Abs(A[i,k]/scaleFactors[i,0]);		// maximum element
						imax = i;									// in column k                                        '
					}
				}
				if(Math.Abs(A[imax,k]) <= buffer){
					goto End_of_k_loop;
				}

				if(imax == k) {
					goto Calculate_m;// row does not need interchanged
				}
				for(j = 0; j<n; j++){ // interchange rows k and imax
					R = A[k,j];
					A[k,j] = A[imax,j];
					A[imax,j] = R;
				}
				R = B[k];				// interchange corresponding right hand sides
				B[k] = B[imax];
				B[imax] = R;

				Calculate_m:
					for(i = k + 1; i<n; i++){
					A[i,k] = A[i,k]/A[k,k];		// compute multiplier
					m = A[i,k];					// assign it to m
					for(j = k + 1; j<n; j++){ // loop over elements
						A[i,j] = A[i,j] - m*A[k,j];	// of row i
					}
					B[i] = B[i]-m*B[k];	// do the right hand side
				}
				End_of_k_loop:

					; // don't know why we need the ;...
			}
			// Back substitution
			i = n-1;

			while(i>=0){	// loop backwards over rows of A
				R = 0.0;
				for(j = i+1; j<n; j++){
					R += A[i,j] * B[j]; // sum up known values
				}
				R = B[i] - R;
				if(Math.Abs(A[i,i])>buffer){
					B[i] = R / A[i,i];
				}
				else if(Math.Abs(R) <=buffer) {	// A is singular
					// system is consistent
					errorFlag = 1;
					goto ErrorHandler;
				}
				else {
					errorFlag = 1;
					goto ErrorHandler;
				}
				i -= 1;
			} //Wend
			ErrorHandler: ;
			if(errorFlag == 1){
				// zero out the result matrix
				for(i=0; i<n-1; i++){
					B[i] = 0.0;
				}
			}
			return B;
		} // end gauss
		
		public static double [] SymmetricBandedSolver(double[,] A, double [] b){
			/*SymmetricBandedSolver solves the equaton Ax=b for the special case
			 * where A is a banded, symmetric matrix.
			 * A is a matrix in a reduced
			 *  storage form:
			 * [a11 .... a1bw 0 ... 0]
			 * [a21 a22 ....  a2bw 0 .. 0]
			 * [.........................]
			 * [0..0 an(n-bw).......ann]
			 * stored as:
			 * [a11 a12 a13 ... a1bw]
			 * [a22 a23 a24 ... a2(bw+1)]
			 * [........................]
			 * [ann 0................0]
			 * or diagonals are stored as columns
			 * b is an array
			 * returns an array containing the solution, x
			 * ref: Intro to FE in Engineering pp 34
			 * */
			int k,i,j,i1,j1,i2,j2; //index variables
			double c; //multiplying constant
			double sum; //summing variable
			int nbw =A.GetLength(1); //half band-width
			int n = A.GetLength(0);  //dimension of A
			double nbk; //number of elements in the kth row
			double nbi; //number of elements in the ith row
			
			//forward elimination
			
			for (k = 1; k < n; k++){
				nbk = System.Math.Min(n-k+1,nbw);
				
				for (i = k+1; i < (nbk + k); i++){
					i1 = i-k+1;
					c = A[k-1,i1-1]/A[k-1,1-1];
					
					for (j = i; j < (nbk + k); j++){
						j1 = j-i+1;
						j2 = j-k+1;
						A[i-1,j1-1] = A[i-1,j1-1] - c*A[k-1,j2-1];
					}
					b[i-1] = b[i-1] - c*b[k-1];
				}
			}
			
			//Backward Elimination
			
			b[n-1] = b[n-1]/A[n-1,1-1];
			for (i2 = 1; i2 < n; i2++){
				i = n-i2;
				nbi = System.Math.Min(n-i+1,nbw);
				sum = 0;
				
				for (j = 2; j < nbi+1; j++){
					sum += A[i-1,j-1]*b[i+j-1-1];
				}
				
				b[i-1] = (b[i-1]-sum)/A[i-1,1-1];
			}
			
			return b;
			
		}
		
		public static double[,] InvertMatrix(double[,] inMatrix){
			
			//Check for singularity
			bool issingular = IsSingular(inMatrix);
			if (issingular) {
				//	throw new ArgumentException("Matrix is Singular, cannot compute inverse");
				
			}
			
			#region Using Mapack
			//First, convert the double array to a Matrix from the Mapack namespace
			Mapack.Matrix aMatrix = ArrayToMatrix(inMatrix);
			
			Mapack.Matrix cMatrix = aMatrix.Inverse;
			
			double [,] c = MatrixToArray(cMatrix);
			#endregion
			
			#region My Code
			/*
			int I;
			int j;
			int k;
			int nRows = inMatrix.GetLength(0);
			int nCols = inMatrix.GetLength(1);
			double [,] outMatrix = new double[nRows,nCols];
			double[,] callMatrix = new double[nRows,nCols];
			// fill inverse matrix with the identity matrix...
			for (j = 0; j < nCols; j++){                  // column loop...
				for (k = 0; k < nRows; k++){              // row loop...
					if (k == j){
						outMatrix[k, j] = 1;
					}
					else{
						outMatrix[k, j] = 0;
					}
				}
			}
			// loop over columns of the inverse matrix
			for (I = 0; I < nCols; I++){  //column loop make the surrogate copy of ABD_inv:
				for (j = 0; j < nCols; j++){
					for (k = 0; k < nRows; k++){
						callMatrix[k, j] = inMatrix[k, j];
					}
				}
				
				MatrixMath.gauss(callMatrix, outMatrix, nCols, I); // calculate inverse column j
			}
			return outMatrix; //return outmatrix
			 */
			#endregion
			
			return c;
			
		}
		
		///Computes the "entry-wise" p-norm of a matrix
		public static double PNorm(int p, double[,] inMatrix)
		{
			int n = inMatrix.GetLength(0);
			int m = inMatrix.GetLength(1);
			double sum = 0;
			
			for (int i =0; i < n; i++) {
				for (int j = 0; j < m; j++) {
					sum += System.Math.Pow(System.Math.Abs(inMatrix[i,j]), p);
				}
			}
			sum = System.Math.Pow(sum, 1.0/p);
			return sum;
		}
		
		public static double [,] Subtract(double [,] A, double [,] B)
		{
			int n = A.GetLength(0);
			int m = A.GetLength(1);
			double [,] diff = new double[n,m];
			
			if (n != B.GetLength(0) || m != B.GetLength(1)) {
				
				throw new ArgumentException("Matrices must be same dimentsions");
			}
			
			for (int i =0; i < n; i++) {
				for (int j = 0; j < m; j++) {
					diff[i,j] = A[i,j] - B[i,j];
				}
			}
			return diff;
		}
		
		public static double [,] Add(double [,] A, double [,] B)
		{
			int n = A.GetLength(0);
			int m = A.GetLength(1);
			double [,] sum = new double[n,m];
			
			if (n != B.GetLength(0) || m != B.GetLength(1)) {
				
				throw new ArgumentException("Matrices must be same dimentsions");
			}
			
			for (int j = 0; j < m; j++) {
				for (int i =0; i < n; i++) {
					sum[i,j] = A[i,j] + B[i,j];
				}
			}
			return sum;
		}
		
		public static double [,] ScalarMultiply(double scalar, double [,] A)
		{
			int n = A.GetLength(0);
			int m = A.GetLength(1);
			double [,] Anew = new double[n,m];
			
			for (int j = 0; j < m; j++) {
				for (int i =0; i < n; i++) {
					
					Anew[i,j] = A[i,j]*scalar;
				}
			}
			return Anew;
		}
		
		public static double [,] Transpose(double [,] A)
		{
			int n = A.GetLength(0);
			int m = A.GetLength(1);
			double [,] At = new double[m,n];
			for (int j = 0; j < m; j++) {
				for (int i =0; i < n; i++) {
					
					At[j,i] = A[i,j];
				}
			}
			return At;
		}
		#region
		///<summary>
		/// This method returns a matrix with the specified column
		/// from A removed.  Note: The col # is a 0-based index
		/// </summary>
		#endregion
		public static double [,] RemoveCol(double [,] A, int col)
		{
			int nRow = A.GetLength(0);
			int nCol = A.GetLength(1);
			int colCount = 0;
			double [,] Anew = new double[nRow,nCol-1];
			
			if (col >= nCol) {
				
				throw new ArgumentException("Cannot remove colum: column outside of matrix");
			}
			
			for (int i =0; i < nCol; i++) {
				
				if (i != col) {
					
					for (int j = 0; j < nRow; j++) {
						
						Anew[j,colCount] = A[j,i];
						
					}
					colCount += 1;
				}
				
			}
			return Anew;
		}
		
		public static double [] ExtractCol(double [,] A, int nCol){
			double [] Col = new double[A.GetLength(0)];
			for (int i = 0; i < Col.Length; i++) {
				Col[i] = A[i, nCol];
			}
			return Col;
		}
		
		public static double [,] ExtractMatrix(double [,] A, int startRow, int endRow, int startCol, int endCol){
			int nRow = endRow - startRow + 1;
			int nCol = endCol - startCol + 1;
			if (((nRow <=0 || nCol <=0) || (startCol < 0 || startRow < 0))
			    || (endRow >= A.GetLength(0) || endCol >= A.GetLength(1))) {
				throw new ArgumentException("Check Matrix Inputs");
			}
			double [,] NewMatrix = new double[nRow, nCol];
			for (int i = 0; i < nRow; i++) {
				for (int j = 0; j <nCol; j++) {
					NewMatrix[i,j] = A[startRow + i, startCol + j];
				}
			}
			return NewMatrix;
		}
		
		public static double [,] Abs(double [,] A)
		{
			int n = A.GetLength(0);
			int m = A.GetLength(1);
			double [,] Anew = new double[n,m];
			
			for (int j = 0; j < m; j++) {
				for (int i =0; i < n; i++) {
					Anew[i,j] = System.Math.Abs(A[i,j]);
				}
			}
			return Anew;
		}
		#region
		///<summary>
		/// This method returns a matrix of relative errors of
		/// A based on B abs(A-B)/A
		/// </summary>
		#endregion
		public static double [,] RelError(double [,] A, double [,] B)
		{
			int n = A.GetLength(0);
			int m = A.GetLength(1);
			double [,] relError = new double[n,m];
			
			if (n != B.GetLength(0) || m != B.GetLength(1)) {
				
				throw new ArgumentException("Matrices must be same dimentsions");
			}
			
			for (int j = 0; j < m; j++) {
				for (int i =0; i < n; i++) {
					relError[i,j] = System.Math.Abs((A[i,j] - B[i,j])/A[i,j]*100);
					if ((A[i,j]-B[i,j]) == 0) {
						relError[i,j] = 0;
					}
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
		public static void MakeSmallNumbersZero(ref double [,] A, double zeroError)
		{
			double Max = 0;
			
			//first, find the maximum value
			foreach (double d in A){
				if (Math.Abs(Max) < Math.Abs(d)){
					Max = d;
				}
			}
			MakeSmallNumbersZero(ref A, zeroError, Max);
			
		}
		#region
		///<summary>
		/// This method gets rid of relatively small numbers compared to the input number and makes them zero.
		/// The input zeroError is the % of the maximum value in the matrix that
		/// becomes zero
		/// </summary>
		#endregion
		public static void MakeSmallNumbersZero(ref double [,] A, double zeroError, double InputNumber)
		{
			int n = A.GetLength(0);
			int m = A.GetLength(1);
			
			
			for (int j = 0; j < m; j++) {
				
				for (int i =0; i < n; i++) {
					
					if (Math.Abs(A[i,j]) < Math.Abs(InputNumber)*zeroError) {
						A[i,j] = 0;
					}
				}
			}
			
		}
		#region
		///<summary>
		/// goes through an array and returns the absolute value of the maximum entry
		/// </summary>
		#endregion
		public static double GetMax(double [] Array){
			int i;
			int n = Array.GetLength(0);
			double largest=0;
			
			for (i=0;i<n;i++) {
				if (Math.Abs(Array[i])>largest) {
					largest = Math.Abs(Array[i]);
				}
			}
			return largest;
		}
		#region
		///<summary>
		/// goes through an array and returns the absolute value of the maximum entry
		/// </summary>
		#endregion
		public static double GetMax(double [,] Array){
			int i,j;
			int n1 = Array.GetLength(0);
			int n2 = Array.GetLength(1);
			double largest=0;
			
			for (j=0;j<n2;j++) {
				for (i=0;i<n1;i++) {
					if (Math.Abs(Array[i,j])>largest) {
						largest = Math.Abs(Array[i,j]);
					}
				}
			}
			return largest;
		}
		public static double GetMax(double [,] Array, int nRow){
			int i,j;
			i = nRow;
			int n2 = Array.GetLength(1);
			double largest=0;
			
			for (j=0;j<n2;j++) {
				
				if (Math.Abs(Array[i,j])>largest) {
					largest = Math.Abs(Array[i,j]);
				}
			}
			return largest;
		}
		#region
		///<summary>
		/// Puts matrix A and matrix B together into one matrix, where A is left and B is right
		/// </summary>
		#endregion
		public static double [,] StackHorizontal(double [,] A, double [,] B)
		{
			int n = A.GetLength(0);
			int m1 = A.GetLength(1);
			int m2 = B.GetLength(1);
			double [,] stack = new double[n, m1 + m2];
			
			if (n != B.GetLength(0)) {
				
				throw new ArgumentException("Matrices must have the same row dimentsions");
			}
			
			
			for (int i =0; i < n; i++) {
				for (int j = 0; j < m1; j++) {
					
					stack[i,j] = A[i,j];
				}
				for (int j = 0; j < m2; j++) {
					
					stack[i, m1 + j] = B[i,j];
				}
			}
			return stack;
		}
		public static double [,] StackHorizontal(double [,] A, double [] B)
		{
			int n = A.GetLength(0);
			int m1 = A.GetLength(1);
			int m2 = B.Length;
			double [,] stack = new double[n, m1 +1];
			
			if (n != m2) {
				
				throw new ArgumentException("Matrix row and vector must have the same  dimentsions");
			}
			
			
			for (int i =0; i < n; i++) {
				for (int j = 0; j < m1; j++) {
					
					stack[i,j] = A[i,j];
					
					
					stack[i, m1] = B[i];
				}
			}
			return stack;
		}
		#region
		/// <summary>
		/// Stack A next to B (A is the first column, B is the second)
		/// </summary>
		/// <param name="A"></param>
		/// <param name="B"></param>
		/// <returns></returns>
		#endregion
		public static double [,] StackHorizontal(double [] A, double [] B)
		{
			int n = A.Length;
			double [,] stack = new double[n, 2];
			
			if (n != B.Length) {
				
				throw new ArgumentException("Vectors must have the same  dimentsions");
			}
			
			
			for (int i =0; i < n; i++) {
				
					stack[i, 0] = A[i];
					
					stack[i, 1] = B[i];
			}
			return stack;
		}
		
		#region
		///<summary>
		/// Puts matrix A and matrix B together into one matrix, where A is top and B is bottom
		/// </summary>
		#endregion
		public static double [,] StackVertical(double [,] A, double [,] B)
		{
			int n1 = A.GetLength(0);
			int n2 = B.GetLength(0);
			int m = A.GetLength(1);
			double [,] stack = new double[n1 + n2, m];
			
			if (m != B.GetLength(1)) {
				
				throw new ArgumentException("Matrices must have the same column dimentsions");
			}
			
			for (int j =0; j < m; j++) {
				
				for (int i = 0; i < n1; i++) {
					
					stack[i,j] = A[i,j];
				}
				for (int i = 0; i < n2; i++) {
					
					stack[i + n1, j] = B[i,j];
				}
			}
			return stack;
		}
		#region
		/// <summary>
		/// Stack A on top of B (A is the first row, B is the second)
		/// </summary>
		/// <param name="A"></param>
		/// <param name="B"></param>
		/// <returns></returns>
		#endregion
		public static double [,] StackVertical(double [] A, double [] B)
		{
			int n = A.Length;
			double [,] stack = new double[2, n];
			
			if (n != B.Length) {
				
				throw new ArgumentException("Vectors must have the same  dimentsions");
			}
			
			
			for (int i =0; i < n; i++) {
				
					stack[0,i] = A[i];
					
					stack[1, i] = B[i];
			}
			return stack;
		}
		public static void CopyToMatrix(ref double [,] MasterMatrix, double [,] MatrixToCopy, int rowToStart, int colToStart){
			int r1 = MasterMatrix.GetLength(0);
			int r2 = MatrixToCopy.GetLength(0);
			int c1 = MasterMatrix.GetLength(1);
			int c2 = MatrixToCopy.GetLength(1);
			
			
			if ((c2 + colToStart > c1) || (r2 + rowToStart > r1)) {
				
				throw new ArgumentException("Copy is outside of master matrix dimensions");
			}
			
			for (int j = 0; j < c2; j++) {
				for (int i = 0; i < r2; i++) {
					MasterMatrix[rowToStart + i, colToStart + j] = MatrixToCopy[i, j];
				}
			}
		}
		public static void CopyToMatrix(ref double [,] MasterMatrix, double [] ArrayToCopy, int rowToStart, int colToStart, bool isHorizontal){
			
			int n = ArrayToCopy.Length;
			
			for (int j = 0; j < n; j++) {
				
				if (isHorizontal){
					MasterMatrix[rowToStart, colToStart + j] = ArrayToCopy[j];
				}
				else{
					MasterMatrix[rowToStart + j, colToStart] = ArrayToCopy[j];
				}
			}
		}
		
		public static void AddToMatrix(ref double [,] MasterMatrix, double [,] MatrixToCopy, int rowToStart, int colToStart){
			int r1 = MasterMatrix.GetLength(0);
			int r2 = MatrixToCopy.GetLength(0);
			int c1 = MasterMatrix.GetLength(1);
			int c2 = MatrixToCopy.GetLength(1);
			
			
			if ((c2 + colToStart > c1) || (r2 + rowToStart > r1)) {
				
				throw new ArgumentException("Copy is outside of master matrix dimensions");
			}
			
			for (int j = 0; j < c2; j++) {
				for (int i = 0; i < r2; i++) {
					MasterMatrix[rowToStart + i, colToStart + j] += MatrixToCopy[i, j];
				}
			}
		}
		public static void AddToMatrix(ref double [,] MasterMatrix, double [] ArrayToCopy, int rowToStart, int colToStart, bool isHorizontal){
			
			int r2, c2, i1;
			int r1 = MasterMatrix.GetLength(0);
			int c1 = MasterMatrix.GetLength(1);
			
			if (isHorizontal) {
				r2 = 1;
				c2 = ArrayToCopy.Length;
			}
			else{
				r2 = ArrayToCopy.Length;
				c2 = 1;
			}
			
			if ((c2 + colToStart > c1) || (r2 + rowToStart > r1)) {
				
				throw new ArgumentException("Copy is outside of master matrix dimensions");
			}
			
			for (int j = 0; j < c2; j++) {
				for (int i = 0; i < r2; i++) {
					if (isHorizontal)  i1 = i;
					else i1 = j;
					
					MasterMatrix[rowToStart + i, colToStart + j] += ArrayToCopy[i1];
				}
			}
		}
		
		public static bool IsSingular(double [,] SquareMatrix){
			
			//First, convert the double array to a Matrix from the Mapack namespace
			Mapack.Matrix aMatrix = ArrayToMatrix(SquareMatrix);
			
			double Det = aMatrix.Determinant;
			
			if (Math.Abs(Det) <= 0.000000000001) {
				return true;
			}
			return false;
		}
		
		public static void ModifiedGuyanReduction(double [,] Ktotal, double [] Rtotal, int nReducedDOF, ref double [,] reducedK, ref double [] reducedR){
			int nDOF = Rtotal.Length;
			if (nDOF == nReducedDOF) {
				reducedK = Ktotal;
				reducedR = Rtotal;
				return;
			}
			double [,] krr = ExtractMatrix(Ktotal, 0, nReducedDOF - 1, 0, nReducedDOF - 1);
			double [,] kroT = ExtractMatrix(Ktotal, nReducedDOF, nDOF - 1, 0, nReducedDOF - 1);
			double [,] kro = ExtractMatrix(Ktotal, 0, nReducedDOF - 1, nReducedDOF, nDOF - 1);
			double [,] koo = ExtractMatrix(Ktotal, nReducedDOF, nDOF - 1, nReducedDOF, nDOF - 1);
			
			double [] Rrr = VectorMath.ExtractVector(Rtotal, 0, nReducedDOF - 1);
			double [] Roo = VectorMath.ExtractVector(Rtotal, nReducedDOF, nDOF - 1);
			double [,] kooInv = InvertMatrix(koo);
			double [,] krokooInv = Multiply(kro, kooInv);
			
			reducedK = Subtract(krr, Multiply(krokooInv, kroT));
			reducedR = VectorMath.Add(Rrr, Multiply(krokooInv, Roo));
		}
		
		public static double [,] SwapRows(double [,] A, int row1, int row2){
			
			int nCols = A.GetLength(1);
			double [,] newA = ExtractMatrix(A, 0, A.GetLength(0)-1, 0, A.GetLength(1)-1); //Make deep copy!!!
			
			for (int i = 0; i < nCols; i++) {
				newA[row1, i] = A[row2, i];
				newA[row2, i] = A[row1, i];
			}
			return newA;
		}
		#region
		/// <summary>
		/// This swaps a section of rows with another section.  Sections must not be the same size, and first section must be above the second.
		/// </summary>
		/// <param name="A">Master Matrix</param>
		/// <param name="startRow1">starting index of row of first section</param>
		/// <param name="endRow1">ending index of row of first section </param>
		/// <param name="startRow2">starting index of row of second section</param>
		/// <param name="endRow2">ending index of row of second section</param>
		/// <returns>Matrix with swapped row sections</returns>
		#endregion
		public static double [,] SwapRows(double [,] A, int startRow1, int endRow1, int startRow2, int endRow2){
			
			int nCols = A.GetLength(1);
			int nRows = A.GetLength(0);
			double [,] Beginning, Middle, End;
			
			double [,] temp1 = ExtractMatrix(A, startRow1, endRow1, 0, nCols -1);
			double [,] temp2 = ExtractMatrix(A, startRow2, endRow2, 0, nCols -1);
			
			if (startRow1 > 0) { //If the first row isn't the start row
				Beginning = ExtractMatrix(A, 0, startRow1 -1, 0, nCols -1);
				temp2 = StackVertical(Beginning, temp2);
			}
			if ( startRow2 - endRow1 > 1) { //If there is space between the two sections
				Middle = ExtractMatrix(A, endRow1+1, startRow2-1, 0, nCols -1);
				temp2 = StackVertical(temp2, Middle);
			}
			if (endRow2 < nRows - 1) { //If the last row isn't the end row
				End = ExtractMatrix(A, endRow2+1, nRows -1, 0, nCols -1);
				temp1 = StackVertical(temp1, End);
			}
			return StackVertical(temp2, temp1);
		}
		
		public static double [,] SwapCols(double [,] A, int col1, int col2){
			
			int nRows = A.GetLength(0);
			double [,] newA = ExtractMatrix(A, 0, A.GetLength(0)-1, 0, A.GetLength(1)-1); //Make deep copy!!!
			
			for (int i = 0; i < nRows; i++) {
				newA[i, col1] = A[i, col2];
				newA[i, col2] = A[i, col1];
			}
			return newA;
		}
		#region
		/// <summary>
		/// This swaps a section of columns with another section.  Sections must not be the same size, and first section must be to the left of the second.
		/// </summary>
		/// <param name="A">Master Matrix</param>
		/// <param name="startCol1">starting index of column of first section </param>
		/// <param name="endCol1">ending index of column of first section</param>
		/// <param name="startCol2">starting index of column of second section</param>
		/// <param name="endCol2">ending index of column of second section</param>
		/// <returns>Matrix with swapped column sections</returns>
		#endregion
		public static double [,] SwapCols(double [,] A, int startCol1, int endCol1, int startCol2, int endCol2){
			
			int nCols = A.GetLength(1);
			int nRows = A.GetLength(0);
			double [,] Beginning, Middle, End;
			
			double [,] temp1 = ExtractMatrix(A, 0, nRows -1, startCol1, endCol1);
			double [,] temp2 = ExtractMatrix(A, 0, nRows -1, startCol2, endCol2);
			
			if (startCol1 > 0) { //If the first column isn't the start column
				Beginning = ExtractMatrix(A, 0, nRows -1, 0, startCol1 -1);
				temp2 = StackHorizontal(Beginning, temp2);
			}
			if ( startCol2 - endCol1 > 1) { //If there is space between the two sections
				Middle = ExtractMatrix(A, 0, nRows -1, endCol1+1, startCol2-1);
				temp2 = StackHorizontal(temp2, Middle);
			}
			if (endCol2 < nCols - 1) { //If the last column isn't the end column
				End = ExtractMatrix(A, 0, nRows -1, endCol2+1, nCols -1);
				temp1 = StackHorizontal(temp1, End);
			}
			return StackHorizontal(temp2, temp1);
		}
		
		public static double AbsMatrixSum(double [,] A){
			double sum = 0;
			foreach(double d in A){
				sum+=Math.Abs(d);
			}
			return sum;
		}
		
		public static double[,] IdentityMatrix(int dim)
        {
			double[,] Ident = new double[dim, dim];
            for (int i = 0; i < dim; i++)
            {
				Ident[i, i] = 1.0;
            }
			return Ident;
        }

		/// <summary>
		/// Finds the eigenvalues of a 3 x 3 symmetric matrix, from https://en.wikipedia.org/wiki/Eigenvalue_algorithm#3%C3%973_matrices
		/// </summary>
		/// <param name="A">3 x 3 symmetric matrix</param>
		/// <returns>3 eigenvalues, with [0] as the max, [2] as the min</returns>
		public static double[] EigenvaluesOf3by3SymmetricMatrix(double[,] A)
		{
			// Note that acos and cos operate on angles in radians
			double eig1, eig2, eig3;

			double p1 = Math.Pow(A[0, 1], 2.0) + Math.Pow(A[0, 2], 2.0) + Math.Pow(A[1, 2], 2.0);

			if (p1.Equals(0.0))
            {
				// A is diagonal.
				eig1 = A[0, 0];
				eig2 = A[1, 1];
				eig3 = A[2, 2];
            }
            else
            {
				// trace(A) is the sum of all diagonal values
				double q = Trace(A) / 3.0;
				double p2 = Math.Pow(A[0, 0] - q, 2.0) + Math.Pow(A[1, 1] - q, 2.0) 
					+ Math.Pow(A[2, 2] - q, 2.0) + 2.0 * p1;
				double p = Math.Sqrt(p2 / 6.0);
				double[,] I = MatrixMath.IdentityMatrix(3);
				double[,] B = MatrixMath.ScalarMultiply( (1.0 / p) , MatrixMath.Subtract(A, MatrixMath.ScalarMultiply(q, I)));

				double r = MatrixMath.Determinant(B) / 2.0;
				//In exact arithmetic for a symmetric matrix - 1 <= r <= 1
				//but computation error can leave it slightly outside this range.
				double phi;
				if (r <= -1)
                {
					phi = Math.PI / 3.0;

				}
                else if (r >= 1)
                {
					phi = 0;

				}
                else
                {
					phi = Math.Acos(r) / 3.0;
				}

				//the eigenvalues satisfy eig3 <= eig2 <= eig1
				eig1 = q + 2.0 * p * Math.Cos(phi);
				eig3 = q + 2.0 * p * Math.Cos(phi + (2.0 * Math.PI / 3.0));
				// since trace(A) = eig1 + eig2 + eig3;
				eig2 = 3.0 * q - eig1 - eig3;

			}
  
			return new double[3] { eig1, eig2, eig3 };
		}

		#region Methods to use the Mapack library
		public static Mapack.Matrix ArrayToMatrix(double [,] A){
			
			int nrow = A.GetLength(0);
			int ncol = A.GetLength(1);
			
			Mapack.Matrix myMatrix = new Matrix(nrow, ncol);
			
			for (int j = 0; j < ncol; j++) {
				
				for (int i = 0; i < nrow; i++) {
					
					myMatrix[i,j] = A[i,j];
				}
			}
			return myMatrix;
		}
		
		public static Mapack.Matrix ArrayToMatrix(double [] A){
			
			int nrow = A.Length;
			
			Mapack.Matrix myMatrix = new Matrix(nrow, 1);
			
			for (int i = 0; i < nrow; i++) {
				
				myMatrix[i,0] = A[i];
				
			}
			return myMatrix;
		}
		
		public static Mapack.Matrix VectorTransposeArrayToMatrix(double [] A){
			
			int nCol = A.Length;

			Mapack.Matrix myMatrix = new Matrix(1, nCol);
			
			for (int i = 0; i < nCol; i++) {
				
				myMatrix[0,i] = A[i];
				
			}
			return myMatrix;
		}
		
		public static double [,] MatrixToArray(Mapack.Matrix A){
			
			int nrow = A.Rows;
			int ncol = A.Columns;
			
			double [,] myArray= new double[nrow, ncol];
			
			for (int j = 0; j < ncol; j++) {
				
				for (int i = 0; i < nrow; i++) {
					
					myArray[i,j] = A[i,j];
				}
			}
			return myArray;
		}
		
		public static double [] MatrixToVectorArray(Mapack.Matrix A){
			
			int nrow = A.Rows;
			int nCol = A.Columns;
			
			if(nrow != 1){
				double [] myArray= new double[nrow];
				
				for (int i = 0; i < nrow; i++) {
					
					myArray[i] = A[i,0];
				}
				return myArray;
			}
			else {
				double [] myArray= new double[nCol];
				
				for (int i = 0; i < nCol; i++) {
					
					myArray[i] = A[0,i];
				}
				return myArray;
			}
		}
		#endregion
	}
}



