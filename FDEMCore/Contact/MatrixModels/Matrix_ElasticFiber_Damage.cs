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
    /// <summary>
    /// Description of IndefiniteIntegralDerivativeForK.
    /// </summary>
    public class Matrix_ElasticFiber_Damage : Matrix_ElasticFiber
    {
        #region Protected Members
        protected Matrix_ElasticFiber_Damage_Section topMatrix;
        protected Matrix_ElasticFiber_Damage_Section bottomMatrix;

        #endregion

        #region Public Members
        public double[] Damage
        {
            get { return VectorMath.Stack(bottomMatrix.Damage, topMatrix.Damage); }
        }
        
        #endregion

        #region Constructor

        /// <param name="zt1">top of top half</param>
        /// <param name="zt2">bottom of top half</param>
        /// <param name="zb1">top of bottom half</param>
        /// <param name="zb2">bottom of bottom half</param>
        public Matrix_ElasticFiber_Damage(int nIntPts, double r, double d, double b, double E, double nu, Fiber fiber1, Fiber fiber2, double dCoeff, 
            double zt1, double zt2, double zb1, double zb2, double strength, double fractureEnergy, double damageAccelerationCoefficient) :
            base(r, d, b, E, nu, fiber1, fiber2, dCoeff)
        {
            double G = E / (2.0 * (1.0 + nu));
            topMatrix = new Matrix_ElasticFiber_Damage_Section(nIntPts, zt1, zt2, true, r, d, b, E, G, strength, fractureEnergy, damageAccelerationCoefficient);
            bottomMatrix = new Matrix_ElasticFiber_Damage_Section(nIntPts, zb1, zb2, false, r, d, b, E, G, strength, fractureEnergy, damageAccelerationCoefficient);

            //Initiate the stiffness: need this because the initial stiffness is needed to calculate the qm
            //UpdateTotalStiffness();

        }

        public Matrix_ElasticFiber_Damage(int nIntPts, double r, double d, double b, double E, double nu, Fiber fiber1, Fiber fiber2, double dCoeff,
            double zt1, double zt2, double zb1, double zb2, double strength, double fractureEnergy) :
            this(nIntPts, r, d, b, E, nu, fiber1, fiber2, dCoeff,zt1, zt2, zb1, zb2, strength, fractureEnergy, 1.0)
        { }
        #endregion

        #region public Methods

        public bool UpdateDamage(double[] q)
        {
            bool hasAdditionalDamage = false;
            topMatrix.UpdateDamage(q, ref hasAdditionalDamage);
            bottomMatrix.UpdateDamage(q, ref hasAdditionalDamage);
            return hasAdditionalDamage;
        }

        public override double[] CalculateStress(double x, double y, double z, double[] qm)
        {
            throw new NotImplementedException("Stress has not been implemented for the damage model");
        }

        public override double[] CalculateStrain(double x, double y, double z, double[] qm)
        {
            throw new NotImplementedException("Strain has not been implemented for the damage model");
        }
        #endregion

        #region Private Methods
        protected override void CalculateMatrixStiffness(double zt1, double zt2, double zb1, double zb2)
        {
            //Get the top matrix stiffness values
            topMatrix.UpdateStiffness(out double[,] km_11t, out double[,] km_12t, out double[,] km_21t, out double[,] km_22t, out double[,] km_13t, out double[,] km_23t);

            //Get the bottom matrix stiffness values 
            bottomMatrix.UpdateStiffness(out double[,] km_11b, out double[,] km_12b, out double[,] km_21b, out double[,] km_22b, out double[,] km_13b, out double[,] km_23b);

            Km_11 = MatrixMath.Add(km_11t, km_11b);
            Km_12 = MatrixMath.Add(km_12t, km_12b);
            Km_21 = MatrixMath.Add(km_21t, km_21b);
            Km_22 = MatrixMath.Add(km_22t, km_22b);
            Km_13 = MatrixMath.Add(km_13t, km_13b);
            Km_23 = MatrixMath.Add(km_23t, km_23b);
        }
       
        #endregion
    }
}
