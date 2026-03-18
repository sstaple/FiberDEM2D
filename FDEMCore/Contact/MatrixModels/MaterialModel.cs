using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FDEMCore.Contact.FailureTheories;

namespace FDEMCore.Contact.MatrixModels
{
    public abstract class MaterialModel
    {
        #region Protected Members
        public double d, b, r1, r2;
        protected double r12, r13, r22, r23, d2, d3, d4, d5, r24, r25, r26, r14, r15, r16;
        protected double zTop, zBottom;
        public IFailureCriteria failureCriteria;
        public const string Name = "MaterialModel";
        #endregion

        #region Constructor
        public MaterialModel(double r1, double r2, double d, double b, double zTop, double zBottom, IFailureCriteria failureCriteria)
        {
            this.failureCriteria = failureCriteria;
            this.r1 = r1; this.r2 = r2; this.d = d; this.b = b;
            this.zTop = zTop; this.zBottom = zBottom;

            r22 = Math.Pow(r2, 2);
            r23 = Math.Pow(r2, 3);
            r24 = Math.Pow(r2, 4);
            r25 = Math.Pow(r2, 5);
            r26 = Math.Pow(r2, 6);
            r12 = Math.Pow(r1, 2);
            r13 = Math.Pow(r1, 3);
            r14 = Math.Pow(r1, 4);
            r15 = Math.Pow(r1, 5);
            r16 = Math.Pow(r1, 6);
            d2 = Math.Pow(d, 2);
            d3 = Math.Pow(d, 3);
            d4 = Math.Pow(d, 4);
            d4 = Math.Pow(d, 5);
        }
        #endregion

        #region Abstract Public Methods
       
        /// <summary>
        /// Calculate the stiffness in terms of all of the degrees of freedom, with the input being the current state variables
        /// </summary>
        /// <returns>the stiffness matrix of the material</returns>
        public abstract double[,] CalculateStiffness(double[] stateVariables);

        public abstract double[] CalculateDisplacements(double x, double y, double z, double[] q, double[] stateVariables);
        public abstract double[] CalculateStress(double x, double y, double z, double[] q, double[] stateVariables);
        public abstract double[] CalculateStrain(double x, double y, double z, double[] q, double[] stateVariables);
        public virtual double[] CalculateStateVariable(double x, double y, double z, double[] q, double[] stateVariables)
        {
            return new double[1];
        }
        public virtual double CalculateLengthBetweenFibers(double z)
        {
            double Yl = MatrixFiberAssembly.CalculateYAtFiber1(r1, z);
            double yR = MatrixFiberAssembly.CalculateYAtFiber2(r2, d, z);

            return (yR - Yl);
        }

        /// <summary>
        /// Determine whether the entire thing is broken.  this updates failure or damage or integration boundaries or whatever is implemented
        /// and returns the new state variables
        /// </summary>
        /// <paramref name="q"/> this is qTotal, which can be passed directly to the stress function</param>
        /// <returns>the stiffness matrix of the material</returns>
        public abstract bool IsItTotallyBroken(double[] stateVariables);

        /// <summary>
        /// Determine whether the entire thing is broken.  this updates failure or damage or integration boundaries or whatever is implemented
        /// and returns the new state variables
        /// </summary>
        /// <paramref name="q"/> assume that this is the q of the matrix, which can be passed directly to the stress function</param>
        /// <returns>the stiffness matrix of the material</returns>
        public abstract bool IsThereFailure(double[] q, ref double[] stateVariables);

        public abstract void WriteFirstIterationOutput( StreamWriter dataWrite);
        


        /// <summary>
        /// Calculate the integral of the out of plane stress (Sxx and Szz) over the volume to be added to the homogenization
        /// </summary>
        /// <returns>the stiffness matrix of the material</returns>
        public abstract double[] CalculateIntegralOfStressOverVolume(double[] q, double[] stateVariables);
        #endregion
    }

    public static class CreateMaterialModel
    {
        public static MaterialModel CreateFiberMaterialModelFromInput(string fiberMaterialName, string constants, double d, double b, double charDist, bool isItFiber1)
        {

            return fiberMaterialName switch
            {
                RigidFiberModel.Name => RigidFiberModel.ReadFirstIterationOutput(constants, d, b, charDist),
                ElasticFiberModel.Name => ElasticFiberModel.ReadFirstIterationOutput(constants, d, b, isItFiber1, charDist),
                _ => throw new Exception($"Failure theory {fiberMaterialName} not a recognized fiber model"),
            };
        }
        public static MaterialModel[] CreateMatrixMaterialModelFromInput(string MatrixMaterialName, string constants, double r1, double r2, double d, double b, double[] zBoundsTopToBottom)
        {
            return MatrixMaterialName switch
            {
                MatrixModel0.Name => MatrixModel0.ReadFirstIterationOutput(constants, r1, d, b, zBoundsTopToBottom),
                MatrixModel1.Name => MatrixModel1.ReadFirstIterationOutput(constants, r1, r2, d, b, zBoundsTopToBottom),
                _ => throw new Exception($"Failure theory {MatrixMaterialName} not a recognized matrix model"),
            };
        }
    }
}
