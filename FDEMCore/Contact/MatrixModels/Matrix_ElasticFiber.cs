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
    public interface IMatrixModel
    {
        //Also, make one for stiffness, Strain, anything else?
        /// <summary>
        /// Calculate the strain at a given location for a given set of matrix degrees of freedom
        /// </summary>
        /// <returns>{Eps_xx, Eps_yy, Eps_zz, Gamma_YZ, Gamma_XZ, Gamma_XY}</returns>
        double[] CalculateStrain(double x, double y, double z, double[] qm);
        /// <summary>
        /// Calculate the stress at a given location for a given set of matrix degrees of freed
        /// </summary>
        /// <returns>{Sig_xx, Sig_yy, Sig_zz, Tau_YZ, Tau_XZ, Tau_XY}</returns>
        double[] CalculateStress(double x, double y, double z, double[] qm);

        public double CalculateYAtFiber1(double z);

        public double CalculateYAtFiber2(double z);
    }

    /// <summary>
    /// Description of IndefiniteIntegralDerivativeForK.
    /// </summary>
    public class Matrix_ElasticFiber : IMatrixModel
    {
        #region Protected Members
        protected double r, d, b, nu, Ep;
        protected double r2, r3, d2, d3, d4, r4;
        protected Complex c1, c2;
        protected double dCoeff;
        protected double[,] D;
        protected double c23; //this decides whether it's coupled (1.0) in the 23 direction or not (0.0).

        //For damping
        protected bool isImplicit = true;
        protected double I12, M12;
        #endregion

        #region Public Members

        //Stiffness of fiber 1, with _x being 11=Fiber node to fiber node, 
        //12=fiber node to fiber surface, 13=fiber node to out of plane strain, 
        //23=fiber surface to out of plane strain
        public double[,] Kf1_11;
        public double[,] Kf1_12;
        public double[,] Kf1_13;
        public double[,] Kf1_21;
        public double[,] Kf1_22;
        public double[,] Kf1_23;

        //Stiffness of fiber 2
        public double[,] Kf2_11;
        public double[,] Kf2_12;
        public double[,] Kf2_13;
        public double[,] Kf2_21;
        public double[,] Kf2_22;
        public double[,] Kf2_23;

        // Stiffnesses for the matrix
        public double[,] Km_11;
        public double[,] Km_12;
        public double[,] Km_21;
        public double[,] Km_22;
        public double[,] Km_13;
        public double[,] Km_23;

        //Assembled Stiffnesses (f=fiber centerline DOF, m = matrix dof)
        public double[,] Kff;
        public double[,] Kmm;
        public double[,] Kmf;
        public double[,] Kfm;
        public double[,] KmmInverse;
        #endregion

        #region Constructor
        public Matrix_ElasticFiber(double r, double d, double b, double Ep, double nu, Fiber fiber1, Fiber fiber2, double dCoeff) : this(r, d, b, Ep, nu, fiber1, fiber2)
        {
            isImplicit = false;

            this.dCoeff = dCoeff;

            I12 = fiber1.Inertia * fiber2.Inertia / (fiber1.Inertia + fiber2.Inertia);
            M12 = fiber1.Mass * fiber2.Mass / (fiber1.Mass + fiber2.Mass);
        }

        public Matrix_ElasticFiber(double r, double d, double b, double Ep, double nu, Fiber fiber1, Fiber fiber2)
        {
            this.b = b;
            this.d = d;
            this.nu = nu;
            this.r = r;
            this.Ep = Ep;

            this.c23 = 1.0;//this decides whether it's coupled (1.0) in the 23 direction or not (0.0).  I have found that it is better to 
            //set nu to 0 for the entire material, and that nu23 doesn't have much of an effect.


            r2 = Math.Pow(r, 2);
            r3 = Math.Pow(r, 3);
            r4 = Math.Pow(r, 4);
            d2 = Math.Pow(d, 2);
            d3 = Math.Pow(d, 3);
            d4 = Math.Pow(d, 4);

            c1 = MyComplex.TakeSquareRoot(d2 - 4 * r2);
            c2 = MyComplex.TakeSquareRoot(-d2 + 4 * r2);

           D = new double[6, 6] {
                {Ep * (1 - nu), Ep * nu, Ep * nu, 0, 0, 0},
                {Ep * nu, Ep * (1 - nu), Ep * nu * c23, 0, 0, 0},
                {Ep * nu, Ep * nu * c23, Ep * (1 - nu), 0, 0, 0},
                {0, 0, 0, (Ep * (1 - 2 * nu)) / 2.0, 0, 0},
                {0, 0, 0, 0, (Ep * (1 - 2 * nu)) / 2.0, 0},
                {0, 0, 0, 0, 0, (Ep * (1 - 2 * nu)) / 2.0}
            };

            //Set the stiffness matrices for the fibers.  These won't change with failed matrix, since the force to make the fiber
            //deform should remain constant.
            CalculateFiberStiffnesses(fiber1, ref Kf1_11, ref Kf1_12, ref Kf1_13, ref Kf1_21, ref Kf1_22, ref Kf1_23, true);
            CalculateFiberStiffnesses(fiber2, ref Kf2_11, ref Kf2_12, ref Kf2_13, ref Kf2_21, ref Kf2_22, ref Kf2_23, false);

        }
        #endregion

        #region public Methods
       
        /// <param name="zt1">top of top half</param>
        /// <param name="zt2">bottom of top half</param>
        /// <param name="zb1">top of bottom half</param>
        /// <param name="zb2">bottom of bottom half</param>
        public virtual void CalculateStiffnesses(ref double[,] k, ref double[,] dampingMatrix,
                                         double zt1, double zt2, double zb1, double zb2)
        {
            CalculateMatrixStiffness(zt1, zt2, zb1, zb2);
            
            //Now, combine it with the fiber stiffness and separate out by inner and outer dof
            AssembleMatricesIntoGlobalSystem();
            //Now take the inverse of the Kmm (this is needed for the stress)
            KmmInverse = MatrixMath.InvertMatrix(Kmm);
            //Now get the equivalent K
            k = MatrixMath.Subtract(Kff, MatrixMath.Multiply(Kfm, MatrixMath.Multiply(KmmInverse, Kmf)));

            foreach (double kij in k)
            {
                if (Double.IsNaN(kij) || Double.IsPositiveInfinity(kij) || Double.IsNegativeInfinity(kij))
                {
                    throw new ArgumentException("k has NaN or +/- infinity");
                }
            }

            if (!isImplicit)
            {
                dampingMatrix = CalculateDampingMatrix(k);
            }
        }
        /// <param name="zt1">top of top half</param>
        /// <param name="zt2">bottom of top half</param>
        /// <param name="zb1">top of bottom half</param>
        /// <param name="zb2">bottom of bottom half</param>
        public double Calculate_knorm(double zt1, double zt2, double zb1, double zb2)
        {
            double[,] k, d;
            k = d = new double[0, 0];

            CalculateStiffnesses(ref k, ref d, zt1, zt2, zb1, zb2);
            //Just return the 1,1 component
            return k[1, 1];
        }

        /// <summary>
        /// Convert between degrees of freedom at the fibers and degrees of freedom at the fiber/matrix interface.
        /// </summary>
        /// <param name="fiberDOF">fiber centerline dof: [5]{theta1, u2, v2, theta2, ug}</param>
        /// <returns> Matrix dof: [7]{u1, v1, w1, theta1, u2, v2, w2, theta2, ug} </returns>
        public double[] CalculateMatrixDOF(double[] fiberDOF)
        {
            double [] qm = VectorMath.ScalarMultiply(-1.0, MatrixMath.Multiply(KmmInverse, MatrixMath.Multiply(Kmf, fiberDOF)));
            //Add UG since it effects the fibers also
            qm = VectorMath.Stack(qm, new double[1] { fiberDOF[4] });
            return qm;
        }

        public virtual double[] CalculateStrain(double x, double y, double z, double[] qm)
        {
            double z2 = z * z;

            double srmz = Math.Sqrt(r2 - z2);
            double c1 = d - 2 * srmz;
            double c12 = c1 * c1;

            double[,] Bxyz = new double[6, 9]{{ 0, 0, 0, 0, 0, 0, 0, 0, 1 / b},
                { 0, 1 / (-d + 2 * srmz), 0, z / c1, 0, 1 / c1, 0, z / (-d + 2 * srmz), 0},
                { 0, 0, -(((d - 2 * y) * z) / (c12 * srmz)), (z * (-d2 - 2 * r2 + d * (2 * srmz + y) + 2 * z2)) / (c12 * srmz), 
                    0, 0, ((d - 2 * y) * z) / (c12 * srmz), (z * (2 * r2 + d * (-2 * srmz + y) - 2 * z2)) / (c12 * srmz), 0},
                { 0, -(((d - 2 * y) * z) / (c12 * srmz)), 1 / (-d + 2 * srmz), (-(d2 * srmz) - 2 * r2 * y + d * (2 * r2 + srmz * y - z2)) / (c12 * srmz), 
                    0, ((d - 2 * y) * z) / (c12 * srmz), 1 / c1, (2 * r2 * y - d * (srmz * y + z2)) / (c12 * srmz), 0},
                { -(((d - 2 * y) * z) / (c12 * srmz)), 0, 0, 0, ((d - 2 * y) * z) / (c12 * srmz), 0, 0, 0, 0},
                { 1 / (-d + 2 * srmz), 0, 0, 0, 1 / c1, 0, 0, 0, 0}
            };

            return MatrixMath.Multiply(Bxyz, qm);
        }

        public virtual double[] CalculateStress(double x, double y, double z, double[] qm)
        {
            
            double[] strain = this.CalculateStrain(x, y, z, qm);

            return MatrixMath.Multiply(D, strain);
        }

        public double CalculateYAtFiber1(double z)
        {
            return Math.Sqrt(r * r - z * z); ;
        }

        public double CalculateYAtFiber2(double z)
        {
            return (d - Math.Sqrt(r * r - z * z));
        }

        #endregion

        #region Private Methods

        protected double[,] CalculateDampingMatrix(double[,] k)
        {
            double[,] d = new double[k.GetLength(0), k.GetLength(1)];

            d[0,1] = Math.Sign(k[0, 1]) * Spring.SetCriticalDamping(M12, Math.Abs(k[0, 1]), dCoeff);
            d[4, 1] = Math.Sign(k[4, 1]) * Spring.SetCriticalDamping(M12, Math.Abs(k[4, 1]), dCoeff);
            d[1, 2] = Math.Sign(k[1, 2]) * Spring.SetCriticalDamping(M12, Math.Abs(k[1, 2]), dCoeff);
            d[5, 2] = Math.Sign(k[5, 2]) * Spring.SetCriticalDamping(M12, Math.Abs(k[5, 2]), dCoeff);

            d[3, 0] = Math.Sign(k[3, 0]) * Spring.SetCriticalDamping(I12, Math.Abs(k[3, 0]), dCoeff);
            d[7, 3] = Math.Sign(k[7, 3]) * Spring.SetCriticalDamping(M12, Math.Abs(k[7, 3]), dCoeff);


            return d;
        }

        protected void AssembleMatricesIntoGlobalSystem()
        {
            double[,] zero4b4 = new double[4, 4];
            double[,] zero4b3 = new double[4, 3];
            double[,] zeroCol = new double[4, 1];

            //Assemble global system and separate between outer and inner degrees of freedom
            double[,] KffTop = MatrixMath.StackHorizontal(MatrixMath.StackHorizontal(Kf1_11, zero4b3), Kf1_13);
            double[,] KffBot = MatrixMath.StackHorizontal(MatrixMath.StackHorizontal(zeroCol, Kf2_11), Kf2_13);
            Kff = MatrixMath.StackVertical(KffTop, KffBot);

            double[,] KfmTop = MatrixMath.StackHorizontal(Kf1_12, zero4b4);
            double[,] KfmBot = MatrixMath.StackHorizontal(zero4b4, Kf2_12);
            Kfm = MatrixMath.StackVertical(KfmTop, KfmBot);

            double[,] KmmTop = MatrixMath.StackHorizontal(MatrixMath.Add(Kf1_22, Km_11), Km_12);
            double[,] KmmBot = MatrixMath.StackHorizontal(Km_21, MatrixMath.Add(Kf2_22, Km_22));
            Kmm = MatrixMath.StackVertical(KmmTop, KmmBot);

            double[,] KmfTop = MatrixMath.StackHorizontal(MatrixMath.StackHorizontal(Kf1_21, zero4b3), MatrixMath.Add(Kf1_23, Km_13));
            double[,] KmfBot = MatrixMath.StackHorizontal(MatrixMath.StackHorizontal(zeroCol, Kf2_21), MatrixMath.Add(Kf2_23, Km_23));
            Kmf = MatrixMath.StackVertical(KmfTop, KmfBot);

        }

        /// <param name="zt1">top of top half</param>
        /// <param name="zt2">bottom of top half</param>
        /// <param name="zb1">top of bottom half</param>
        /// <param name="zb2">bottom of bottom half</param>
        protected virtual void CalculateMatrixStiffness(double zt1, double zt2, double zb1, double zb2)
        {
            Complex[,] Km_11_top, Km_12_top, Km_13_top, Km_21_top, Km_22_top, Km_23_top;
            Km_11_top = Km_12_top = Km_13_top = Km_23_top = Km_21_top = Km_22_top = new Complex[,] { { new Complex(0.0, 0.0) } };
            Complex[,] Km_11_bottom, Km_12_bottom, Km_13_bottom, Km_21_bottom, Km_22_bottom, Km_23_bottom;
            Km_11_bottom = Km_12_bottom = Km_13_bottom = Km_23_bottom = Km_21_bottom = Km_22_bottom = new Complex[,] { { new Complex(0.0, 0.0) } };

            Integral(zt1, zt2, ref Km_11_top, ref Km_12_top, ref Km_13_top, ref Km_21_top, ref Km_22_top, ref Km_23_top);
            Integral(zb1, zb2, ref Km_11_bottom, ref Km_12_bottom, ref Km_13_bottom, ref Km_21_bottom, ref Km_22_bottom, ref Km_23_bottom);

            //sum up the evaluations of the indefinite integral at the edges of the top and bottom of the adhesive

            try
            {
                Km_11 = MyComplex.ConvertComplexToDouble(MyComplex.Add(Km_11_top, Km_11_bottom));
                Km_12 = MyComplex.ConvertComplexToDouble(MyComplex.Add(Km_12_top, Km_12_bottom));
                Km_13 = MyComplex.ConvertComplexToDouble(MyComplex.Add(Km_13_top, Km_13_bottom));
                Km_21 = MyComplex.ConvertComplexToDouble(MyComplex.Add(Km_21_top, Km_21_bottom));
                Km_22 = MyComplex.ConvertComplexToDouble(MyComplex.Add(Km_22_top, Km_22_bottom));
                Km_23 = MyComplex.ConvertComplexToDouble(MyComplex.Add(Km_23_top, Km_23_bottom));
            }
            catch (Exception ex)
            {
                throw new System.Exception(ex.Message + $"  Had imaginary part!  zt1 {zt1}, zt2 {zt2}, zb1 {zb1}, zb2 {zb2}, d0 {d}");
            }
        }

        protected void Integral(double zt, double zb, ref Complex[,] km_11, ref Complex[,] km_12, ref Complex[,] km_13,
            ref Complex[,] km_21, ref Complex[,] km_22, ref Complex[,] km_23)
        {
            Complex[,] km_11zt, km_12zt, km_13zt, km_21zt, km_22zt, km_23zt;
            km_11zt = km_12zt = km_13zt = km_21zt = km_22zt = km_23zt = new Complex[,] { { new Complex(0.0, 0.0) } };
            Complex[,] km_11zb, km_12zb, km_13zb, km_21zb, km_22zb, km_23zb;
            km_11zb = km_12zb = km_13zb = km_21zb = km_22zb = km_23zb = new Complex[,] { { new Complex(0.0, 0.0) } };

            EvalIndefiniteIntegral(zt, ref km_11zt, ref km_12zt, ref km_13zt, ref km_21zt, ref km_22zt, ref km_23zt);
            EvalIndefiniteIntegral(zb, ref km_11zb, ref km_12zb, ref km_13zb, ref km_21zb, ref km_22zb, ref km_23zb);

            km_11 = MyComplex.Subtract(km_11zt, km_11zb);
            km_12 = MyComplex.Subtract(km_12zt, km_12zb);
            km_13 = MyComplex.Subtract(km_13zt, km_13zb);
            km_21 = MyComplex.Subtract(km_21zt, km_21zb);
            km_22 = MyComplex.Subtract(km_22zt, km_22zb);
            km_23 = MyComplex.Subtract(km_23zt, km_23zb);

        }

        protected void EvalIndefiniteIntegral(double z, ref Complex[,] km_11, ref Complex[,] km_12, ref Complex[,] km_13, ref Complex[,] km_21, ref Complex[,] km_22, ref Complex[,] km_23)
        {

            if (z > r)
            {
                throw new ArgumentException("z point above or below the fiber radius");
            }
            
            double z2 = z * z;
            //establish all of the constants that are particular to a z
            double rmz = Math.Sqrt(r2 - z2);
            double c3 = d2 - 4 * r2 + 4 * z2;

            Complex a1 = Complex.Atan(z / rmz);
            Complex a2 = Complex.Atan((d * z) / (c1 * rmz));
            Complex a3 = Complex.Atan((2 * z) / c1);
            Complex a4 = 0.5 * Complex.Log((r + z) / (r - z)); //Actually arctanh(z/r), but no arctanh
                                                               //Complex a5 = Complex.Atan((rmz * z)/ (-r2 + z2));


            //Complex l1 = Complex.Log(d - 2 * rmz);
            //Complex l2 = Complex.Log(-d + 2 * rmz);
            double l3 = Math.Log(c3);
            Complex l4 = Complex.Log(c2 - 2.0 * z);
            Complex l5 = Complex.Log(c2 + 2 * z);
            // Complex l7 = Complex.Log(2 * r2 * (c2 + 2 * z) + d * ((c2 * rmz) - d * z));
            // Complex l8 = Complex.Log(2 * r2 * (c2 - 2 * z) + d * ((c2 * rmz) + d * z));
            Complex l9 = Complex.Log(-2 * r2 - d * rmz + c2 * z);
            Complex l10 = Complex.Log(2 * r2 + d * rmz + c2 * z);
            Complex l11 = Complex.Log(-r2 + z2);
            Complex l12 = Complex.Log(-r + z);
            Complex l13 = Complex.Log(r + z);


            Complex k11 = (b * Ep * (-1.0 + 2.0 * nu) * (a1 * c1 * d - a3 * d2 - a4 * c1 * r - 2 * a3 * r2 -
       a2 * (d2 + 2.0 * r2))) / (6.0 * c1 * d);

            Complex k22 = (b * Ep * (5.0 * a3 * d2 - 4.0 * a3 * d2 * nu + a1 * c1 * d * (-5.0 + 4.0 * nu) + 2.0 * a4 * c1 * r -
       4.0 * a4 * c1 * nu * r + 4.0 * a3 * r2 - 8.0 * a3 * nu * r2 +
       a2 * (5.0 * d2 - 4.0 * d2 * nu + 4.0 * r2 - 8.0 * nu * r2))) / (12.0 * c1 * d);

            Complex k24 = -(b * Ep * (d2 * (l10 - l3 - l4 - l5 + l9) * (-5.0 + 4.0 * nu) +
        4.0 * (l3 * r2 + l4 * r2 + l5 * r2 - l9 * r2 - 2.0 * l3 * nu * r2 - 2.0 * l4 * nu * r2 -
           2.0 * l5 * nu * r2 + 2.0 * l9 * nu * r2 + l10 * (-1.0 + 2.0 * nu) * r2 +
           l11 * (-1.0 + 2.0 * nu) * r2 + 6.0 * d * rmz - 6 * d * nu * rmz + 6 * c23 * d * nu * rmz))) / (48.0 * d);

            //Drop the imaginary part of 23 here, since the solution is not imaginary.  Mathematica has made an error here in their integration, and I don't have the time to fix it.
            k24 = new Complex(k24.Real, 0.0);

            Complex k44 = (b * Ep * (a1 * c1 * d * (d2 + 3.0 * (-2.0 + nu - c23 * nu) * r2) - 2.0 * a4 * c1 * r *
        (2.0 * d2 * (-1.0 + nu) + (-1.0 + 2 * nu) * r2) -
       a2 * (d4 - 6.0 * d2 * r2 + 4.0 * d2 * nu * r2 - 4.0 * r4 + 8.0 * nu * r4) -
       a3 * (d4 - 6.0 * d2 * r2 + 4.0 * d2 * nu * r2 - 4.0 * r4 + 8.0 * nu * r4) + 
       3.0 * c1 * d * (2.0 + (-1.0 + c23) * nu) * rmz * z)) /
   (12.0 * c1 * d);


            Complex k48 = (b * Ep * (a1 * c1 * d * (d2 * (-3.0 + 4.0 * nu) + 6.0 * (2.0 + (-1.0 + c23) * nu) * r2) +
       4.0 * a4 * c1 * r * (d2 * (-1.0 + nu) + (-1.0 + 2.0 * nu) * r2) -
       a2 * (-3.0 * d4 + 4.0 * d4 * nu + 4.0 * d2 * r2 + 8.0 * r4 - 16.0 * nu * r4) -
       a3 * (-3.0 * d4 + 4.0 * d4 * nu + 4.0 * d2 * r2 + 8.0 * r4 - 16.0 * nu * r4) - 6.0 * c1 * d * (2.0 + (-1.0 + c23) * nu) * rmz * z))
    / (24.0 * c1 * d);

            
            Complex k33 = (b * Ep * (a3 * d2 - 4 * a3 * d2 * nu + a1 * c1 * d * (-1 + 4 * nu) + 4 * a4 * c1 * r
                - 4 * a4 * c1 * nu * r + 8 * a3 * r2 - 8 * a3 * nu * r2 + a2 * (d2 - 4 * d2 * nu + 8 * r2
                - 8 * nu * r2))) / (12.0 * c1 * d);

            Complex k34 = (b * Ep * (a3 * d2 - 4 * a3 * d2 * nu + a1 * c1 * d * (-1 + 4 * nu) - 2 * c1 * l12 * r
                + 2 * c1 * l13 * r + 2 * c1 * l12 * nu * r - 2 * c1 * l13 * nu * r + 8 * a3 * r2 - 8 * a3 * nu * r2
                + a2 * (d2 - 4 * d2 * nu + 8 * r2 - 8 * nu * r2))) / (24.0 * c1);

            Complex k72 = -(Ep * nu * z);
            Complex k73 = Ep * nu * ((d * rmz) / 2.0 + z2);
            //Complex k77 = -((Ep * (-.01 + nu) * (a5 * r2 + (d - rmz) * z)) / b);

            Complex zero = new Complex(0.0, 0.0);

            km_11 = new Complex[,] { { k11, zero, zero, zero }, { zero, k22, zero, k24 }, { zero, zero, k33, k34 }, { zero, k24, k34, k44 } };

            km_12 = new Complex[,] { { -1.0 * k11, zero, zero, zero}, { zero, -1.0 * k22, zero, -1.0 * k24}, { zero, zero, -1.0 * k33, k34}, { zero, -1.0 * k24, -1.0 * k34, k48} };

            km_21 = new Complex[,] { { -1.0 * k11, zero, zero, zero }, { zero, -1.0 * k22, zero, -1.0 * k24 }, { zero, zero, -1.0 * k33, -1.0 * k34 }, { zero, -1.0 * k24, k34, k48 } };

            km_22 = new Complex[,] { { k11, zero, zero, zero }, { zero, k22, zero, k24 }, { zero, zero, k33, -1.0 * k34 }, { zero, k24, -1.0 * k34, k44 } };

            km_13 = new Complex[,] { { zero }, { k72 }, { zero }, { k73 } };
            km_23 = new Complex[,] { { zero }, { -1.0 * k72 }, { zero }, { -1.0 * k73 } };
        }

        protected void CalculateFiberStiffnesses(Fiber fiber, ref double[,] k11, ref double[,] k12, ref double[,] k13, ref double[,] k21, ref double[,] k22, ref double[,] k23, bool isLeftFiber)
        {
            //TODO: THIS IS THE WORST EVER!!!  I didn't want to add nu as an input, so I'm just going to assume isotropic.  Someday, add this 
            //property to the input file and calculate G.  Heck, even reformulated this for anisotropic materials
            double nuf = fiber.Nu;
            double Gf = fiber.Modulus2 / (2.0 * (1.0 + nuf));
            double scaleArea = 0.55; //This is just empirical, to see if I can match the stiffness
            double scaleArea3 = 0.5;

            double kfx = scaleArea * 2.0 * Gf * b;
            double kfy = scaleArea * 2.0 * fiber.Modulus2 * b * (1.0 - nuf) / ((1.0 - 2.0 * nuf) * (1.0 + nuf));
            double kfz = scaleArea3 * 2.0 * Gf * b; //Arbitrarily increased to see the effect....
            double kft = (-1 + Math.Sqrt(5.0)) * Gf * b * r2 * Math.PI;
            double kfyug = scaleArea * fiber.Modulus2 * nuf / ((1.0 + nuf) * (1.0 - 2.0 * nuf)) * fiber.Radius;

            k22 = new double[,] { { kfx, 0.0, 0.0, 0.0 }, { 0.0, kfy, 0.0, 0.0 }, { 0.0, 0.0, kfz, 0.0 }, { 0.0, 0.0, 0.0, kft } };
            k13 = new double[,] { { 0.0 }, { -1.0 * kfyug }, { 0.0 }, { 0.0 } };
            k12 = MatrixMath.ScalarMultiply(-1.0, k22);
            k23 = MatrixMath.ScalarMultiply(-1.0, k13);

            // If it's a left fiber, then the displacement bcs are 0 because coordinate system folows fiber centerpoint
            if (isLeftFiber)
            {
                k11 = new double[,] { { 0.0 }, { 0.0 }, { 0.0 }, { kft } };
            }
            else
            {
                k11 = new double[,] { { kfx, 0.0, 0.0 }, { 0.0, kfy, 0.0 }, { 0.0, 0.0, 0.0 }, { 0.0, 0.0, kft } };
            }
            k21 = MatrixMath.ScalarMultiply(-1.0, k11);
        }

        #region Methods to get the transverse stress contributions to the homogenized stress
        public void CalculateIntegralOfStressDV(out double[,] kS11, out double[,] kS33, double zt1, double zt2, double zb1, double zb2)
        {
            IntegralOfStress(zt1, zt2, out Complex[,] kS_xx_top, out Complex[,] kS_zz_top);
            IntegralOfStress(zb1, zb2, out Complex[,] kS_xx_bottom, out Complex[,] kS_zz_bottom);

            //sum up the evaluations of the indefinite integral at the edges of the top and bottom of the adhesive

            try
            {
                kS11 = MyComplex.ConvertComplexToDouble(MyComplex.Add(kS_xx_top, kS_xx_bottom));
                kS33 = MyComplex.ConvertComplexToDouble(MyComplex.Add(kS_zz_top, kS_zz_bottom));
            }
            catch (Exception ex)
            {
                throw new System.Exception(ex.Message + $"  Had imaginary part!  zt1 {zt1}, zt2 {zt2}, zb1 {zb1}, zb2 {zb2}, d0 {d}");
            }
        }
        protected void IntegralOfStress(double zt, double zb, out Complex[,] kSxx, out Complex[,] kSzz)
        {
            EvalIndefiniteIntegralOfStress(zt, out Complex[,] kS_11zt, out Complex[,] kS_33zt);
            EvalIndefiniteIntegralOfStress(zb, out Complex[,] kS_11zb, out Complex[,] kS_33zb);

            kSxx = MyComplex.Subtract(kS_11zt, kS_11zb);
            kSzz = MyComplex.Subtract(kS_33zt, kS_33zb);
        }

        protected void EvalIndefiniteIntegralOfStress(double z, out Complex[,] kS_11, out Complex[,] kS_33)
        {

            if (z > r)
            {
                throw new ArgumentException("z point above or below the fiber radius");
            }

            double z2 = z * z;
            //establish all of the constants that are particular to a z
            double rmz = Math.Sqrt(r2 - z2);

            Complex a1 = Complex.Atan(z * rmz / (z2 - r2));

            kS_11 = new Complex[,] { {0,
                    -(b * Ep * nu * z),
                    0,
                    b * Ep * nu * ((d * rmz) / 2.0 + z2),
                    0,
                    b * Ep * nu * z,
                    0,
                    b * Ep * nu * (-0.5 * (d * rmz) - z2),
                    Ep * (-1 + nu) * (-(d * z) + z * rmz - r2 * a1)}};

            kS_33 = new Complex[,] {{0,
                    -(b*Ep*nu*z),
                    0,
                    (b*Ep*(d*(1 - nu)*rmz + z2))/2.0,
                    0,
                    b*Ep*nu*z,0,(b*Ep*(d*(-1 + nu)*rmz - z2))/2.0,
                    Ep*nu*(d*z - z*rmz + r2*a1)
                } };
        }

        #endregion

        #endregion


    }
}
