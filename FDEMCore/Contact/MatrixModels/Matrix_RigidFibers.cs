/*
 * Created by SharpDevelop.
 * User: Scott_Stapleton
 * Date: 10/23/2019
 * Time: 3:57 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Numerics;
using myMath;


namespace FDEMCore.Contact
{
    //TODO Update project to the appropriate C# version.  Then override +-*/ operators.  Then make Sdr/sdnr complex. Then return real part only.  Check.

    /// <summary>
    /// Description of MatrixRigidFibers.
    /// </summary>
    public class Matrix_RigidFibers : IMatrixModel
    {
        protected double r, d, b, nu, Ep;
        protected double r2, r3, d2, d3, d4, r4;
        protected Complex c1, c2;
        protected double dCoeff;
        protected double[,] D;
        protected bool isImplicit = true;



        protected double I12, M12;

        public Matrix_RigidFibers(double r, double d, double b, double Ep, double nu, double m1, double m2, double I1, double I2, double dCoeff)
            :this(r, d, b, Ep, nu)
        {
            isImplicit = false;

            this.dCoeff = dCoeff;

            I12 = I1 * I2 / (I1 + I2);
            M12 = m1 * m2 / (m1 + m2);

        }

        public Matrix_RigidFibers(double r, double d, double b, double Ep, double nu)
        {

            this.b = b;
            this.d = d;
            this.nu = nu;
            this.r = r;
            this.Ep = Ep;

            r2 = Math.Pow(r, 2);
            r3 = Math.Pow(r, 3);
            d2 = Math.Pow(d, 2);
            d3 = Math.Pow(d, 3);

            c1 = MyComplex.TakeSquareRoot(d2 - 4 * r2);
            c2 = MyComplex.TakeSquareRoot(-d2 + 4 * r2);

            D = new double[6, 6] {
                {Ep * (1 - nu), Ep * nu, Ep * nu, 0, 0, 0},
                {Ep * nu, Ep * (1 - nu), Ep * nu, 0, 0, 0},
                {Ep * nu, Ep * nu, Ep * (1 - nu), 0, 0, 0},
                {0, 0, 0, (Ep * (1 - 2 * nu)) / 2.0, 0, 0},
                {0, 0, 0, 0, (Ep * (1 - 2 * nu)) / 2.0, 0},
                {0, 0, 0, 0, 0, (Ep * (1 - 2 * nu)) / 2.0}
            };
        }

        /// <param name="zt1">top of top half</param>
        /// <param name="zt2">bottom of top half</param>
        /// <param name="zb1">top of bottom half</param>
        /// <param name="zb2">bottom of bottom half</param>
        public void CalculateStiffnesses(ref double[,] k, ref double[,] d,
                                         double zt1, double zt2, double zb1, double zb2)
        {
            k = SumTopAndBottomIntegrals(zt1, zt2, zb1, zb2);

            if (!isImplicit)
            {
                d = CalculateDampingMatrix(k);
            }
        }
        /// <param name="zt1">top of top half</param>
        /// <param name="zt2">bottom of top half</param>
        /// <param name="zb1">top of bottom half</param>
        /// <param name="zb2">bottom of bottom half</param>
        public double Calculate_knorm(double zt1, double zt2, double zb1, double zb2) {
            double[,] k = SumTopAndBottomIntegrals(zt1, zt2, zb1, zb2);
            return k[1, 1];
        }

        public double CalculateYAtFiber1(double z)
        {
            return Math.Sqrt(r * r - z * z); ;
        }

        public double CalculateYAtFiber2(double z)
        {
            return (d - Math.Sqrt(r * r - z * z));
        }
        protected double[,] CalculateDampingMatrix(double[,] k)
        {
            double[,] d = new double[k.GetLength(0), k.GetLength(1)];

            for (int i = 0; i < k.GetLength(0); i++)
            {
                for (int j = 0; j < k.GetLength(1); j++)
                {
                    /*For debugging....
                    if (Double.IsNaN(k[i, j]))
                    {
                        bool debugFlag = true;
                    }*/
                    // get rid of the damping terms for 1-cos, since these terms are not really related to the angular velocity as much as the sine is.
                    if (j != 5 || j != 7)
                    {

                        if (j >= 4)
                        {
                            d[i, j] = Math.Sign(k[i, j]) * Spring.SetCriticalDamping(I12, Math.Abs(k[i, j]), dCoeff);
                        }
                        else
                        {
                            d[i, j] = Math.Sign(k[i, j]) * Spring.SetCriticalDamping(M12, Math.Abs(k[i, j]), dCoeff);

                        }

                    }
                }
            }

            return d;
        }

        /// <param name="zt1">top of top half</param>
        /// <param name="zt2">bottom of top half</param>
        /// <param name="zb1">top of bottom half</param>
        /// <param name="zb2">bottom of bottom half</param>
        protected double[,] SumTopAndBottomIntegrals(double zt1, double zt2, double zb1, double zb2) {
            double[,] integralTop = Integral(zt1, zt2);
            double[,] integralBottom = Integral(zb1, zb2);

            //sum up the evaluations of the indefinite integral at the edges of the top and bottom of the adhesive
            double[,] sumOfIntegral = MatrixMath.Add(integralTop, integralBottom);

            return sumOfIntegral;
        }

        protected double[,] Integral(double zt, double zb)
        {
            Complex[,] f_zt = EvalIndefiniteIntegral(zt);
            Complex[,] f_zb = EvalIndefiniteIntegral(zb);

            Complex[,] integral = MyComplex.Subtract(f_zt, f_zb);

            return MyComplex.ConvertComplexToDouble(integral);

        }

        protected Complex[,] EvalIndefiniteIntegral(double z) {

            double z2 = z * z;
            //establish all of the constants that are particular to a z
            double rmz = Math.Sqrt(r2 - z2);
            double c3 = d2 - 4 * r2 + 4 * z2;

            Complex a1 = Complex.Atan(z / rmz);
            Complex a2 = Complex.Atan((d * z) / (c1 * rmz));
            Complex a3 = Complex.Atan((2 * z) / c1);

            Complex l1 = Complex.Log(d - 2 * rmz);
            Complex l2 = Complex.Log(-d + 2 * rmz);
            double l3 = Math.Log(c3);
            Complex l4 = Complex.Log(c2 - 2.0 * z);
            Complex l5 = Complex.Log(c2 + 2 * z);
            Complex l7 = Complex.Log(2 * r2 * (c2 + 2 * z) + d * ((c2 * rmz) - d * z));
            Complex l8 = Complex.Log(2 * r2 * (c2 - 2 * z) + d * ((c2 * rmz) + d * z));


            Complex[,] klin = new Complex[3, 8]{
                { -((a2 + a3)*b*Ep*(-1 + 2*nu)*r)/(2*c1),0,0,0,0,0,0,0 },

                { 0,
                    (b*Ep*(-(a1*d) + (a2*(d2 + 4*(1 - 2*nu)*r2))/c1 + (a3*(d2 + 4*(1 - 2*nu)*r2))/c1 - 2*z))/ (8.0*r),(b*Ep*(d*l1 + 2*rmz))/(8.0*r),
                    (Ep*nu*(a1*r2 + rmz*z))/(2.0*r),
                    -(b*Ep*(l2*(-1 + 2*nu)*r2 + z2))/(4.0*r),
                    (b*Ep*(-((a2*d*(-1 + nu)*r2)/c1) - (a3*d*(-1 + nu)*r2)/c1 - (a1*(1 + 2*nu)*r2)/2.0 - (rmz*z)/2.0))/ (2.0*r),
                    -(b*Ep*(l1*(d2 + 2*(1 - 2*nu)*r2) + 2*d*rmz - 2*z2))/(8.0*r),
                    (b*Ep*(-(a1*(d2 + 2*(1 - 2*nu)*r2)) + (a2*d*(d2 - 4*nu*r2))/c1 + (a3*d*(d2 - 4*nu*r2))/c1 - 2*d*z - 2*rmz*z))/(8.0*r) },
                { 0,
                    (b*Ep*(d*l1 + 2*rmz))/(8.0*r),
                    -(b*Ep*(-(a1*d) + (a2*(d2 + 8*(-1 + nu)*r2))/c1 + (a3*(d2 + 8*(-1 + nu)*r2))/c1 - 2*z))/ (8.0*r),(Ep*nu*z2)/(2.0*r),
                    (b*Ep*(2*a1*(-1 + nu)*r2 + (a2*d*(-1 + 2*nu)*r2)/c1 + (a3*d*(-1 + 2*nu)*r2)/c1 + rmz*z))/ (4.0*r),
                    -(b*Ep*(2*l2*(-1 + nu)*r2 + z2))/ (4.0*r),(b*Ep*(-(a1* (d2 + 4*(-1 + nu)*r2)) + (a2*d*(d2 + 2*(-3 + 2*nu)*r2))/c1
                    + (a3*d*(d2 + 2*(-3 + 2*nu)*r2))/c1 - 2*d*z - 2*rmz*z))/(8.0*r),
                    (b*Ep*(l1*(d2 + 4*(-1 + nu)*r2) + 2*d*rmz - 2*z2))/(8.0*r)}
            };

            Complex[,] krot = new Complex[1, 8]{
                {
                    0,
                    (b*Ep*(l3 + l4 + l5 - l7 - l8)*(-1 + 2*nu)*r)/8.0,
                    (b*Ep*(2*a1*c1*c2 - 2*a3*c2*d + c1*d*(-l4 + l5 - l7 + l8))* (-1 + 2*nu)*r)/(8.0*c1*c2),
                    0,
                    (b*Ep*(-1 + 2*nu)*(2*a3*c2*r3 + c1*(l4*r3 - l5*r3 + l7*r3 - l8*r3 + 2*c2*r*z)))/(4.0*c1*c2),
                    -(b*Ep*(-1 + 2*nu)*r*rmz)/2.0,
                    -(b*Ep*(-1 + 2*nu)*r*(2*a1*c1*c2*d + c1*d2*(-l4 + l5 - l7 + l8) - 2*a3*c2*(d2 - 2*r2) + 2*c1*(l4*r2 - l5*r2 + l7*r2 - l8*r2 + 2*c2*z)))/(8.0*c1*c2),
                    (b*Ep*(-1 + 2*nu)*r*(d*(l3 + l4 + l5 - l7 - l8) + 4*rmz))/8.0
                    }
                };

            //Join the two k matrices
            Complex[,] kTot = MyComplex.StackVertical(klin, krot);

            return kTot;
        }

        public double[] CalculateStrain(double x, double y, double z, double[] q)
        {
            double z2 = z * z;

            double srmz = Math.Sqrt(r2 - z2);
            double c1 = d - 2 * srmz;
            double c12 = c1 * c1;

            double[,] Bxyz = new double[6, 7] {
                { 0,0,0,0,0,0,1/b },
            { 0,1/(-d + 2*srmz),z/c1,0,1/c1,z/(-d + 2*srmz),0 },
            { 0,0,(z*(-d2 - 2*r2 + d*(2*srmz + y) + 2*z2))/(c12*srmz),0,0,(z*(2*r2 + d*(-2*srmz + y) - 2*z2))/(c12*srmz),0 },
            { 0,-(((d - 2*y)*z)/(c12*srmz)),(-(d2*srmz) - 2*r2*y + d*(2*r2 + srmz*y - z2))/(c12*srmz),0,
                    ((d - 2*y)*z)/(c12*srmz),(2*r2*y - d*(srmz*y + z2))/(c12*srmz),0 },
            {-(((d - 2*y)*z)/(c12*srmz)),0,0,((d - 2*y)*z)/(c12*srmz),0,0,0 },{1/(-d + 2*srmz),0,0,1/c1,0,0,0}
            };

            return MatrixMath.Multiply(Bxyz, q);
        }

        public double[] CalculateStress(double x, double y, double z, double[] q)
        {

            double[] strain = this.CalculateStrain(x, y, z, q);

            return MatrixMath.Multiply(D, strain);
        }

    }
    public static class MyComplex 
    { 
        #region static complex numerical methods
        public static Complex TakeSquareRoot(double radicand)
        {
            //If the radicand is positive, make it's square root the real part
            if(radicand >= 0.0)
            {
                return new Complex(Math.Sqrt(radicand), 0.0);
            }
            //If not, then take the square root of the negative radicand as the imaginary part
            return new Complex(0.0, Math.Sqrt(-1.0 * radicand));
        }

        public static double[,] ConvertComplexToDouble(Complex [,] c)
        {
            double[,] d = new double[c.GetLength(0), c.GetLength(1)];
            for (int i = 0; i < c.GetLength(0); i++)
            {
                for (int j = 0; j < c.GetLength(1); j++)
                {
                    d[i, j] = c[i, j].Real;
                    if (Math.Abs(c[i,j].Imaginary) >  Math.Abs(c[i,j].Real) * 0.00001 && !c[i, j].Real.Equals(0))
                    {
                        throw new System.Exception("stiffness component has a significant imaginary part.  " +
                            "Either the integration bounds were outside of the fiber or within an overlap region.  Index = " + i + ", " + j +
                            " and value = " + c[i, j].Real + " (Re), " + c[i, j].Imaginary + " (Im)");
                    }
                }
            }
            return d;
        }

        public static Complex[,] Subtract(Complex[,] a, Complex[,] b)
        {
            int n = a.GetLength(0);
            int m = a.GetLength(1);
            Complex[,] sum = new Complex[n, m];

            if (n != b.GetLength(0) || m != b.GetLength(1))
            {

                throw new ArgumentException("Matrices must be same dimentsions");
            }

            for (int j = 0; j < m; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    sum[i, j] = a[i, j] - b[i, j];
                }
            }
            return sum;
        }

        public static Complex[,] Add(Complex[,] a, Complex[,] b)
        {
            int n = a.GetLength(0);
            int m = a.GetLength(1);
            Complex[,] sum = new Complex[n, m];

            if (n != b.GetLength(0) || m != b.GetLength(1))
            {

                throw new ArgumentException("Matrices must be same dimentsions");
            }

            for (int j = 0; j < m; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    sum[i, j] = a[i, j] + b[i, j];
                }
            }
            return sum;
        }

        public static Complex[,] StackVertical(Complex[,] a, Complex[,] b)
        {
            int n1 = a.GetLength(0);
            int n2 = b.GetLength(0);
            int m = a.GetLength(1);
            Complex[,] stack = new Complex[n1 + n2, m];

            if (m != b.GetLength(1))
            {
                throw new ArgumentException("Matrices must have the same column dimentsions");
            }

            for (int j = 0; j < m; j++)
            {

                for (int i = 0; i < n1; i++)
                {

                    stack[i, j] = a[i, j];
                }
                for (int i = 0; i < n2; i++)
                {

                    stack[i + n1, j] = b[i, j];
                }
            }
            return stack;
        }
        #endregion
    }
}
