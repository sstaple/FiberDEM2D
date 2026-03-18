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
    public abstract class ZIntegratedMatrixModel : MaterialModel
    {
        /// <summary>
        /// Purpose:
        /// Created By: Scott_Stapleton
        /// Created On: 7/21/2022 3:22:22 PM
        /// </summary>
        #region Protected Members

        //These are all quantities per integration point
        protected int nDOF;
        protected int nStateVarPerIntPt;
        public double[] zIntPts;
        protected double[] stateVariables;
        public double[] yLeft_IntPts;
        public double[] yRight_IntPts;
        protected double[][,] BxyzLeft;
        protected double[][,] BxyzRight;
        protected double[][,] BxyzCenter;

        protected FailureCritForZIntegratedMatrix failureCritForZInt;

        #endregion

        #region Public Members

        #endregion

        #region Constructor
        public ZIntegratedMatrixModel(double r1, double r2, double d, double b, double zTop, double zBottom, bool isTopMatrix, int nIntPts, FailureCritForZIntegratedMatrix failureCriteria)
            : base(r1, r2, d, b, zTop, zBottom, failureCriteria)
        {
            //This avoids a lot of casting....
            failureCritForZInt = failureCriteria;

            //Initiate all quantities to be saved
            zIntPts = new double[nIntPts + 1];
            yLeft_IntPts = new double[nIntPts + 1];
            yRight_IntPts = new double[nIntPts + 1];
            BxyzLeft = new double[nIntPts + 1][,];
            BxyzRight = new double[nIntPts + 1][,];
            BxyzCenter = new double[nIntPts + 1][,];

            //Set all of the integration points, B matrix, and K_coeff matrices 
            for (int i = 0; i < nIntPts + 1; i++)
            {
                //Set z
                zIntPts[i] = QuadraticZ(i, zTop, zBottom, nIntPts, isTopMatrix);

                //Set y/left and y/right
                yLeft_IntPts[i] = MatrixFiberAssembly.CalculateYAtFiber1(r1, zIntPts[i]);
                yRight_IntPts[i] = MatrixFiberAssembly.CalculateYAtFiber2(r2, d, zIntPts[i]);

                //Set Bxyz
                BxyzLeft[i] = BMatrixForStrain(0, yLeft_IntPts[i], zIntPts[i]);
                BxyzRight[i] = BMatrixForStrain(0, yRight_IntPts[i], zIntPts[i]);
                BxyzCenter[i] = BMatrixForStrain(0, 0.0, zIntPts[i]);

            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Calculate the stiffness in terms of all of the degrees of freedom, with the input being the current state variables
        /// </summary>
        /// <param name="stateVariables"></param>
        /// <returns>the stiffness matrix of the material</returns>
        public override double[,] CalculateStiffness(double[] stateVariables)
        {
            double[,] stiffness = TrapezoidalIntegration(true, stateVariables);
            return stiffness;
            
        }

        public override double[] CalculateIntegralOfStressOverVolume(double[] q, double[] stateVariables)
        {

            double[,] intBD_dV = TrapezoidalIntegration(false, stateVariables);
            double[] intBD_dy_q = MatrixMath.Multiply(intBD_dV, q);
            return intBD_dy_q;

        }

        //Material Model Overrides
        public override double[] CalculateDisplacements(double x, double y, double z, double[] q, double[] stateVariables)
        {
            double[,] N = NMatrixForDisplacements(x, y, z);
            return MatrixMath.Multiply(N, q);
        }

        public override double[] CalculateStress(double x, double y, double z, double[] q, double[] stateVariables)
        {

            double[] sv_i = CalculateStateVariable(x, y, z, q, stateVariables);

            double[] strain = CalculateStrain(x, y, z, q, sv_i);
            double[,] Stiffness = CalculateMaterialStiffness(sv_i);

            return MatrixMath.Multiply(Stiffness, strain);

        }

        public override double[] CalculateStrain(double x, double y, double z, double[] q, double[] stateVariables)
        {
            double[,] B = BMatrixForStrain(x, y, z);
            return MatrixMath.Multiply(B, q);
        }

        public override double[] CalculateStateVariable(double x, double y, double z, double[] q, double[] stateVariables)
        {
            //Interpolate to get D at z
            //Find the z index that is between
            int i;

            //If the z is before the beginning....
            if (z >= zIntPts[0])
            {
                return ExtractIntegrationPointStateVariable(stateVariables, nStateVarPerIntPt, 0);
            }

            for (i = 0; i < zIntPts.Length - 1; i++)
            {
                if (z <= zIntPts[i] && z >= zIntPts[i+1])
                {
                    double[] sv_i = ExtractIntegrationPointStateVariable(stateVariables, nStateVarPerIntPt, i);
                    double[] sv_ip1 = ExtractIntegrationPointStateVariable(stateVariables, nStateVarPerIntPt, i+1);
                    return VectorMath.Add(sv_i, VectorMath.ScalarMultiply((z - zIntPts[i]) / (zIntPts[i + 1] - zIntPts[i]), VectorMath.Subtract(sv_ip1, sv_i)));
                }
            }
            //If z is past the end....
            return ExtractIntegrationPointStateVariable(stateVariables, nStateVarPerIntPt, i);
        }

        public double[] CalculateStrain(int currIntPt, int currZ_0Left_1Right_2Middle, double[] qTotal)
        {
            double[,] Bxyz = currZ_0Left_1Right_2Middle switch { 0 => BxyzLeft[currIntPt], 1 => BxyzRight[currIntPt], 2 => BxyzCenter[currIntPt], _ => null };
            return MatrixMath.Multiply(Bxyz, qTotal);
        }
        public double[] CalculateStress(int currIntPt, int currZ_0Left_1Right_2Middle, double[] qTotal, double[] intPtStateVariables)
        {

            double[] strain = CalculateStrain(currIntPt, currZ_0Left_1Right_2Middle, qTotal);
            double[,] Stiffness = CalculateMaterialStiffness(intPtStateVariables);

            return MatrixMath.Multiply(Stiffness, strain);
        }
        public double CalculateLengthBetweenFibers(int currIntPt)
        {
            return (yRight_IntPts[currIntPt] - yLeft_IntPts[currIntPt]);
        }
        #endregion

        #region Failure Methods

        /// <summary>
        /// Determine whether there is damage or failure or whatever and returns the new state variables.  This flags a stiffness re-calculation
        /// </summary>
        public override bool IsThereFailure(double[] q, ref double[] stateVariables)
        {
            //at each integration point
            bool isThereFailure = false;

            //at each integration point
            for (int i = 0; i < zIntPts.Length; i++)
            {
                failureCritForZInt.CurrentIntPt = i;

                //pull out the state variables that belong to the node
                double[] intPtStateVariables = ExtractIntegrationPointStateVariable(stateVariables, nStateVarPerIntPt, i);

                //Check the left
                failureCritForZInt.CurrZ_0Left_1Right_2Middle = 0;
                failureCriteria.FailureFunction(0, yLeft_IntPts[i], zIntPts[i], q, ref intPtStateVariables, this);

                //Check the right
                failureCritForZInt.CurrZ_0Left_1Right_2Middle = 1;
                failureCriteria.FailureFunction(0, yRight_IntPts[i], zIntPts[i], q, ref intPtStateVariables, this);

                //Check the middle
                failureCritForZInt.CurrZ_0Left_1Right_2Middle = 2;
                failureCriteria.FailureFunction(0, 0.0, zIntPts[i], q, ref intPtStateVariables, this);

                double[] ogStateVars = ExtractIntegrationPointStateVariable(stateVariables, nStateVarPerIntPt, i);

                //Return true if the state variables have changed
                if (!ogStateVars.SequenceEqual(intPtStateVariables))
                {
                    isThereFailure = true;

                    //RE-save the new state variables if there was a change
                    VectorMath.CopyToVector(ref stateVariables, intPtStateVariables, i * nStateVarPerIntPt);
                }
            }
            return isThereFailure;
        }

        public override bool IsItTotallyBroken(double[] stateVariables)
        {
            bool isItTotallyBroken = true;

            //at each integration point
            for (int i = 0; i < zIntPts.Length; i++)
            {
                //pull out the state variables that belong to the node
                double[] intPtStateVariables = ExtractIntegrationPointStateVariable(stateVariables, nStateVarPerIntPt, i);

                //Return false if one of them is not broken
                if (!intPtStateVariables.Equals(1.0))
                {
                    isItTotallyBroken = false;
                }                
            }
            return isItTotallyBroken;
        }
       
        #endregion

        #region Private Methods
        protected abstract double[,] IntegralBDB_dA(double z, double[] intPtStateVariables);

        protected abstract double[,] IntegralBD_dA(double z, double[] intPtStateVariables);

        protected abstract double[,] BMatrixForStrain(double x, double y, double z);
        
        protected abstract double[,] NMatrixForDisplacements(double x, double y, double z);

        protected abstract double[,] CalculateMaterialStiffness(double[] intPtStateVariables);

        protected double[,] TrapezoidalIntegration(bool isItK, double [] stateVariables)
        {
            double[,] sum = new double[nDOF,nDOF];
            double[,] kim1 = new double[nDOF, nDOF];
            double[,] ki = new double[nDOF, nDOF];

            //TODO: make this integration over z!!!  
            for (int i = 0; i < zIntPts.Length; i++)
            {
                //pull out the state variables that belong to the node
                double[] intPtStateVariables = ExtractIntegrationPointStateVariable(stateVariables, nStateVarPerIntPt, i);
                
                //Either calculate K or the integral of the stress
                ki = isItK ? IntegralBDB_dA(zIntPts[i], intPtStateVariables) : IntegralBD_dA(zIntPts[i], intPtStateVariables);

                //trapezoidal integration
                if (i == 0)
                {
                    sum = new double[ki.GetLength(0), ki.GetLength(1)];
                }
                else
                {
                    //The sign here is because z is decreasing, not increasing.
                    sum = MatrixMath.Add(sum, MatrixMath.ScalarMultiply(0.5 * ( - zIntPts[i] + zIntPts[i - 1]), MatrixMath.Add(kim1, ki)));
                }
                kim1 = (double[,])ki.Clone();
            }
            return sum;

        }

        #endregion

        #region Static Methods

        ///This samples the z space with a quadratic.  It is only public so that it can be easily tested.... This is from QuadraticSampling.nb
        public static double QuadraticZ(int i, double zUpper, double zLower, int nIntPts, bool isTop)
        {
            //This is quadratic with the slope 0 at the upper bound
            //The higher the slope, the more biased the distribution
            double z;
            double di = Convert.ToDouble(i);
            double dnIntPts = Convert.ToDouble(nIntPts);
            if (isTop)
            {
                z = zLower + 2.0 * (dnIntPts - di) / dnIntPts * (zUpper - zLower)  - (zUpper - zLower) * Math.Pow((dnIntPts - di) / dnIntPts, 2.0);
            }
            else
            {
                z = zLower + (Math.Pow((dnIntPts - di), 2.0) * (-zLower + zUpper)) / Math.Pow(dnIntPts, 2.0);
            }
            return z;
        }

        public static double[] ExtractIntegrationPointStateVariable(double[] stateVariables, int nStateVarPerIntPt, int i)
        {

            return nStateVarPerIntPt == 0 ? new double[0] : VectorMath.ExtractVector(stateVariables, i * nStateVarPerIntPt, (i + 1) * nStateVarPerIntPt - 1);
        }
        #endregion
    }
}
