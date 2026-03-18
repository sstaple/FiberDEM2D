using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using RandomMath;
using FDEMCore.Contact.FailureTheories;

namespace FDEMCore.Contact.MatrixModels
{
    public abstract class ClosedFormMatrixModel : MaterialModel, RootFinding.iFunction
    {
        /// <summary>
        /// Purpose:
        /// Created By: Scott_Stapleton
        /// Created On: 7/21/2022 3:22:22 PM
        /// </summary>
        #region Protected Members


        //These are all quantities per integration point
        protected double[] zIntPts;
        protected double[] damage;
        protected double[] yLeft_IntPts;
        protected double[] yRight_IntPts;
        protected double[][,] BxyzLeft;
        protected double[][,] BxyzRight;

        protected double[] q;
        protected double[] stateVariables;

        protected double Ep, nu;
        protected double[,] D;

        protected bool isBroken, checkFiber1Surface;

        #endregion

        #region Public Members

        #endregion

        #region Constructor
        public ClosedFormMatrixModel(double r, double d, double b, double Ep, double nu, double zTop, double zBottom, IFailureCriteria failureCriteria) 
            : base(r, r, d, b, zTop, zBottom, failureCriteria)
        {
            this.nu = nu;
            this.Ep = Ep;

            D = new double[6, 6] {
                {Ep * (1 - nu), Ep * nu, Ep * nu, 0, 0, 0},
                {Ep * nu, Ep * (1 - nu), Ep * nu, 0, 0, 0},
                {Ep * nu, Ep * nu, Ep * (1 - nu), 0, 0, 0},
                {0, 0, 0, (Ep * (1 - 2 * nu)) / 2.0, 0, 0},
                {0, 0, 0, 0, (Ep * (1 - 2 * nu)) / 2.0, 0},
                {0, 0, 0, 0, 0, (Ep * (1 - 2 * nu)) / 2.0}
            };
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Calculate the stiffness in terms of all of the degrees of freedom, with the input being the current state variables
        /// </summary>
        /// <param name="stateVariables">in this case, stateVariables is a set of 4 values: zt1, zt2, zb1, zb2, wich is the current integration
        /// limits from top to bottom</param>
        /// <returns>the stiffness matrix of the material</returns>
        public override double[,] CalculateStiffness(double[] stateVariables)
        {
            Complex[,] KComplex = Integral(zTop, zBottom, true); ;


            //Convert to double
            double[,] K = MyComplex.ConvertComplexToDouble(KComplex);

            foreach (double kij in K)
            {
                if (Double.IsNaN(kij) || Double.IsPositiveInfinity(kij) || Double.IsNegativeInfinity(kij))
                {
                    throw new ArgumentException("k has NaN or +/- infinity");
                }
            }
            return K;

        }

        /// <summary>
        /// Calculate the stress at a given location for a given set of matrix degrees of freedom
        /// </summary>
        /// <returns>{Sig_xx, Sig_yy, Sig_zz, Tau_YZ, Tau_XZ, Tau_XY}</returns>
        public override double[] CalculateStress(double x, double y, double z, double[] q, double[] stateVariables)
        {
            double[] strain = this.CalculateStrain(x, y, z, q, stateVariables);

            return MatrixMath.Multiply(D, strain);
        }

        public override double[] CalculateIntegralOfStressOverVolume(double[] q, double[] stateVariables)
        {
            Complex[,]  integralS11S33OverVolumeComplex = Integral(zTop, zBottom, false);
            double[,] integralS11S33OverVolume = MyComplex.ConvertComplexToDouble(integralS11S33OverVolumeComplex);
           return MatrixMath.Multiply(integralS11S33OverVolume, q);
        }

        public override double[] CalculateDisplacements(double x, double y, double z, double[] q, double[] stateVariables)
        {
            return new double[3];
        }

        #endregion

        #region Failure Methods

        /// <summary>
        /// Determine whether the entire thing is broken.  this updates failure or damage or integration boundaries or whatever is implemented
        /// and returns the new state variables
        /// </summary>
        public override bool IsThereFailure(double[] q, ref double[] stateVariables)
        {
            this.stateVariables = stateVariables; 
            this.q = q;

            if (failureCriteria is FailureTheories.NoFailure)
            {
                return false;
            }

            bool isThereFailure = false;

            //Check F1 Surface
            checkFiber1Surface = true;
            bool FailureF1 = IsThereFailureInTheSection(ref stateVariables[0], ref stateVariables[1]);

            //check F2 surface
            checkFiber1Surface = false;
            bool FailureF2 = IsThereFailureInTheSection(ref stateVariables[0], ref stateVariables[1]);

            //Only Recalculate stiffness if there was failure
            if (FailureF1 || FailureF2)
            {
                isThereFailure = true;
            }

            return isThereFailure;
        }

        public override bool IsItTotallyBroken(double[] stateVariables)
        {
            return isBroken;
        }

        protected bool IsThereFailureInTheSection(ref double ztop, ref double zbottom)
        {
            bool isThereFailure = false;
            double ftop = Eval(ztop);
            double fbottom = Eval(zbottom);

            //If both sides are broken (>0)
            if (ftop >= 0.0 && fbottom >= 0.0)
            {
                double zmiddle = (ztop + zbottom) / 2.0;
                double fmiddle = Eval(zmiddle);

                //If the middle is broken too, then just break the whole thing
                if (fmiddle >= 0.0)
                {
                    ztop = zbottom;
                    isBroken = true;
                    isThereFailure = true;
                }
                //If the middle isn't broken, find the intersect in both halves
                else
                {
                    //Move both edges
                    FindNewZwhenOneSideIsFailed(ftop, fmiddle, ref ztop, ref zmiddle);
                    FindNewZwhenOneSideIsFailed(fmiddle, fbottom, ref zmiddle, ref zbottom);

                    isThereFailure = true;
                }
            }
            else if (ftop > 0.0 || fbottom > 0.0)
            {
                FindNewZwhenOneSideIsFailed(ftop, fbottom, ref ztop, ref zbottom);

                isThereFailure = true;
            }

            return isThereFailure;
        }

        protected void FindNewZwhenOneSideIsFailed(double ft, double fb, ref double zt, ref double zb)
        {
            //Debug code
            //double tempzt1 = z1;
            //double tempzt2 = z2;

            //This finds the 0 in between z1 and z2
            double zZero = RootFinding.FalsiMethod(this, zt, zb, ft, fb, 0.01, 3);

            //This moves the z of the 0 to the one that was failed
            if (ft >= 0)
            {
                //z1 = zZero;
                double diff = zt - zZero;
                //overshoot by the crack growth to speed up convergence
                zt = zZero - 2.0 * diff;
                //If we overshot past zb
                if (zt <= zb)
                {
                    zt = zb + 0.5 * (zZero - zb);
                }
            }
            else
            {
                //zb = zZero;
                double diff = zZero - zb;
                zb = zZero + 2.0 * diff;
                //if we overshot
                if (zb >= zt)
                {
                    zb = zt - 0.5 * (zt - zZero);
                }
            }
        }
        public double Eval(double x)
        {
            double y = (checkFiber1Surface) ? MatrixFiberAssembly.CalculateYAtFiber1(r1, x) : MatrixFiberAssembly.CalculateYAtFiber2(r2, d, x);
            double l = MatrixFiberAssembly.CalculateYAtFiber2(r2, d, 0.0) - MatrixFiberAssembly.CalculateYAtFiber1(r1, 0.0);

            return failureCriteria.FailureFunction( x, y, 0.0, q, ref stateVariables, this);
        }

        #endregion

        #region Private Methods
       
        protected Complex[,] Integral(double zt, double zb, bool isItK)
        {
            Complex[,] f_zt, f_zb;
            if (isItK)
            {
                f_zt = EvalIndefiniteIntegral(zt);
                f_zb = EvalIndefiniteIntegral(zb);
            }
            else
            {
                f_zt = EvalIndefiniteIntegral_OutOfPlaneStressOverVolume(zt);
                f_zb = EvalIndefiniteIntegral_OutOfPlaneStressOverVolume(zb);
            }

            Complex[,] integral = MyComplex.Subtract(f_zt, f_zb);

            return integral;

        }

        protected abstract Complex[,] EvalIndefiniteIntegral(double z);

        protected abstract Complex[,] EvalIndefiniteIntegral_OutOfPlaneStressOverVolume(double z);

        #endregion

        #region Static Methods

        #endregion
    }

    public static class MyComplex
    {
        #region static complex numerical methods
        public static Complex TakeSquareRoot(double radicand)
        {
            //If the radicand is positive, make it's square root the real part
            if (radicand >= 0.0)
            {
                return new Complex(Math.Sqrt(radicand), 0.0);
            }
            //If not, then take the square root of the negative radicand as the imaginary part
            return new Complex(0.0, Math.Sqrt(-1.0 * radicand));
        }

        public static double[,] ConvertComplexToDouble(Complex[,] c)
        {
            double[,] d = new double[c.GetLength(0), c.GetLength(1)];
            for (int i = 0; i < c.GetLength(0); i++)
            {
                for (int j = 0; j < c.GetLength(1); j++)
                {
                    d[i, j] = c[i, j].Real;
                    if (Math.Abs(c[i, j].Imaginary) > Math.Abs(c[i, j].Real) * 0.00001 && !c[i, j].Real.Equals(0))
                    {
                        /*throw new System.Exception("stiffness component has a significant imaginary part.  " +
                            "Either the integration bounds were outside of the fiber or within an overlap region.  Index = " + i + ", " + j +
                            " and value = " + c[i, j].Real + " (Re), " + c[i, j].Imaginary + " (Im)");
                        */
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
