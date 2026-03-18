using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FDEMCore.Contact.MatrixModels
{
    /// <summary>
    /// Purpose: Model of a fiber in a fiber/matrix/fiber assembly
    /// Created By: Scott_Stapleton
    /// Created On: 7/21/2022 2:26:37 PM
    /// Note: this runs on a 5 dof model, with Uf2, Vf2, wf2, Thetaf2, Ug = q.
    /// It does nothing since the fiber is rigid
    /// </summary>
    public class RigidFiberModel:MaterialModel
    {

        #region Protected Members

        protected double rf, rf2;
        #endregion

        #region Public Members
        public new const string Name = "RigidFiberModel";
        #endregion

        #region Constructor
        public RigidFiberModel(double r, double d, double b, double [] zBoundsTopToBottom) : base(r, 0.0, d, b, zBoundsTopToBottom[0], 
            zBoundsTopToBottom[3], new FailureTheories.NoFailure()) 
        {
            rf = r;
            rf2 = r * r;
        }
        #endregion

        #region Public Methods
        public override double[] CalculateDisplacements(double x, double y, double z, double[] q, double[] stateVariables)
        {
            return new double[3];
        }

        public override double[] CalculateStress(double x, double y, double z, double[] q, double[] stateVariables)
        {
            return new double[6];
        }

        public override double[] CalculateStrain(double x, double y, double z, double[] q, double[] stateVariables)
        {
            return new double[6];
        }

        public override double[,] CalculateStiffness(double[] stateVariables)
        {
            return new double[5,5];
        }

        public override bool IsThereFailure(double[] q, ref double[] stateVariables)
        {
            return false;
        }

        public override bool IsItTotallyBroken(double[] stateVariables)
        {
            return false;
        }

        public override double[] CalculateIntegralOfStressOverVolume(double[] q, double[] stateVariables)
        {
            return new double[6];
        }

        public override void WriteFirstIterationOutput(StreamWriter dataWrite)
        {
            dataWrite.Write(rf);
        }
        public static RigidFiberModel ReadFirstIterationOutput(string totalString, double d, double b, double charDist)
        {
            string[] allStrings = totalString.Split(',');

            double r = double.Parse(allStrings[0]);

            double[] zBoundsTopToBottom = MatrixFiberAssembly.DetermineIntegrationBounds(charDist, d, r, r, out double matrixArea);

            //Assumes that r1 = r2
            return new RigidFiberModel(r, d, b, zBoundsTopToBottom);

        }


        #endregion

        #region Private Methods

        #endregion

        #region Static Methods

        #endregion
    }
}
