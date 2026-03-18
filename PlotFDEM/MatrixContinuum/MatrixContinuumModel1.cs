using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDEMCore.Contact.MatrixModels;

namespace PlotFDEM.MatrixContinuum
{
    public class MatrixContinuumModel1: iMatrixModel
    {
        /// <summary>
        /// Purpose:
        /// Created By: Scott_Stapleton
        /// Created On: 11/2/2022 10:22:21 AM
        /// </summary>
        #region Protected Members

        protected MatrixFiberAssembly fiberAssembly;

        private List<double[]> damage;
        private List<double[]> topDamage;
        private List<double[]> bottomDamage;
        private List<double[]> totalQ;
        private double[] zPoints;
        public double[] zBounds;


        #endregion

        #region Public Members

        #endregion

        #region Constructor
        public MatrixContinuumModel1(string name, string totalString)
        {
            //Parse out the string to make all of the needed objects./
            fiberAssembly = MatrixFiberAssembly.ReadFirstIterationOutput(totalString);
            ZIntegratedMatrixModel zTop = (ZIntegratedMatrixModel)(fiberAssembly.topMatrixMaterial);
            ZIntegratedMatrixModel zBot = (ZIntegratedMatrixModel)(fiberAssembly.bottomMatrixMaterial);

            zPoints = RandomMath.VectorMath.Stack(zTop.zIntPts, zBot.zIntPts); 
            zBounds = fiberAssembly.zBoundsTopToBottom;

            damage = new List<double[]>();
            topDamage = new List<double[]>();
            bottomDamage = new List<double[]>();
            totalQ = new List<double[]>();
        }
        #endregion

        #region Public Methods
        public double[] CalculateDisplacement(double x, double y, double z, double[] q, int iteration, double yLeft, double yRight,
            bool bPlotMatrixResultsOnly = false)
        {
            //Decide where the inquiry is
            if (y < yLeft && !bPlotMatrixResultsOnly)
            {
                return fiberAssembly.fiber1Material.CalculateDisplacements(x, y, z, totalQ[iteration], topDamage[iteration]);
            }
            else if (y > yRight && !bPlotMatrixResultsOnly)
            {
                return fiberAssembly.fiber2Material.CalculateDisplacements(x, y, z, totalQ[iteration], topDamage[iteration]);
            }
            else
            {
                if (z >= 0)
                {
                    return fiberAssembly.topMatrixMaterial.CalculateDisplacements(x, y, z, totalQ[iteration], topDamage[iteration]);
                }
                else { return fiberAssembly.bottomMatrixMaterial.CalculateDisplacements(x, y, z, totalQ[iteration], bottomDamage[iteration]); }
            }
        }

        public double[] CalculateStrain(double x, double y, double z, double[] q, int iteration, double yLeft, double yRight,
            bool bPlotMatrixResultsOnly = false)
        {
            //Decide where the inquiry is
            if (y < yLeft && !bPlotMatrixResultsOnly)
            {
                return fiberAssembly.fiber1Material.CalculateStrain(x, y, z, totalQ[iteration], topDamage[iteration]);
            }
            else if (y > yRight && !bPlotMatrixResultsOnly)
            {
                return fiberAssembly.fiber2Material.CalculateStrain(x, y, z, totalQ[iteration], topDamage[iteration]);
            }
            else
            {
                if (z >= 0)
                {
                    return fiberAssembly.topMatrixMaterial.CalculateStrain(x, y, z, totalQ[iteration], topDamage[iteration]);
                }
                else { return fiberAssembly.bottomMatrixMaterial.CalculateStrain(x, y, z, totalQ[iteration], bottomDamage[iteration]); }
            }
        }

        public double[] CalculateStress(double x, double y, double z, double[] q, int iteration, double yLeft, double yRight,
            bool bPlotMatrixResultsOnly = false)
        {
            if (y < yLeft && !bPlotMatrixResultsOnly)
            {
                return fiberAssembly.fiber1Material.CalculateStress(x, y, z, totalQ[iteration], topDamage[iteration]);
            }
            else if (y > yRight && !bPlotMatrixResultsOnly)
            {
                return fiberAssembly.fiber2Material.CalculateStress(x, y, z, totalQ[iteration], topDamage[iteration]);
            }
            else
            {
                if (z >= 0)
                {
                    return fiberAssembly.topMatrixMaterial.CalculateStress(x, y, z, totalQ[iteration], topDamage[iteration]);
                }
                else { return fiberAssembly.bottomMatrixMaterial.CalculateStress(x, y, z, totalQ[iteration], bottomDamage[iteration]); }
            }
        }

        public double CalculateDamage(double x, double y, double z, double[] q, int iteration, double yLeft, double yRight,
            bool bPlotMatrixResultsOnly = false)
        {
            if (y < yLeft && !bPlotMatrixResultsOnly)
            {
                return fiberAssembly.fiber1Material.CalculateStateVariable(x, y, z, totalQ[iteration], topDamage[iteration])[0];
            }
            else if (y > yRight && !bPlotMatrixResultsOnly)
            {
                return fiberAssembly.fiber2Material.CalculateStateVariable(x, y, z, totalQ[iteration], topDamage[iteration])[0];
            }
            else
            {
                if (z >= 0)
                {
                    return fiberAssembly.topMatrixMaterial.CalculateStateVariable(x, y, z, totalQ[iteration], topDamage[iteration])[0];
                }
                else { return fiberAssembly.bottomMatrixMaterial.CalculateStateVariable(x, y, z, totalQ[iteration], bottomDamage[iteration])[0]; }
            }
        }



        public void AddDamageAndTotalQ(double[] damageValues, double[] fiberQ)

        {
            damage.Add(damageValues);
            MatrixFiberAssembly.SplitStateVariables(damageValues, out double[] topStateVariables, out double[] bottomStateVariables);
            topDamage.Add(topStateVariables);
            bottomDamage.Add(bottomStateVariables);
            fiberAssembly.RecalculateStiffness(damageValues);
            fiberAssembly.CalculateTotalDOF(fiberQ);
            totalQ.Add(fiberAssembly.qTotal);
        }
        
        #endregion

        #region Private Methods

        #endregion

        #region Static Methods

        #endregion
    }
}
