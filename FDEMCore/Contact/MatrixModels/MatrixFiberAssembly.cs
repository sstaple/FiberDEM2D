using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using  RandomMath;
using System.IO;
using FDEMCore.Contact.FailureTheories;

namespace FDEMCore.Contact.MatrixModels
{
    public class MatrixFiberAssembly
    {
        #region Protected Members
        protected double dCoeff;
        protected double characteristicDistance;
        protected double d, b;

        //For damping
        protected bool isImplicit = true;
        protected double I12, M12;

        public MaterialModel fiber1Material;
        public MaterialModel fiber2Material;
        public MaterialModel topMatrixMaterial;
        public MaterialModel bottomMatrixMaterial;

        protected int nOfOuterDOF = 5; //For now, this is always 5: u2, v2, w2, T2, ug
        protected int nStateVariables;
        protected int nDimTotal;

        public double[] zBoundsTopToBottom;
        public double matrixVolume;
        public string matrixModelName;

        #endregion

        #region Public Members

        //Stiffness of fiber 1, with _x being 11=Fiber node to fiber node, 
        //12=fiber node to fiber surface, 13=fiber node to out of plane strain, 
        //23=fiber surface to out of plane strain
        public double[,] Kf1;
        public double[,] Kf2;
        public double[,] Km;
        public double[,] KTotal;

        //Assembled Stiffnesses (f=fiber centerline DOF, m = matrix dof)
        public double[,] Kff;
        public double[,] Kmm;
        public double[,] Kmf;
        public double[,] Kfm;
        public double[,] KmmInverse;

        public double[,] Keq;
        public double[] qTotal;
        public double[] stateVariables;


        #endregion

        #region Constructor
        public MatrixFiberAssembly(double d, double b, MatrixAssemblyParameters matrixParameters, Fiber fiber1, Fiber fiber2,
            out double[] initialStateVariables, double dCoeff):
            this(d, b, matrixParameters, fiber1, fiber2, out initialStateVariables)
        {
            isImplicit = false;

            this.dCoeff = dCoeff;

            I12 = fiber1.Inertia * fiber2.Inertia / (fiber1.Inertia + fiber2.Inertia);
            M12 = fiber1.Mass * fiber2.Mass / (fiber1.Mass + fiber2.Mass);
        }

        public MatrixFiberAssembly(double d, double b, MatrixAssemblyParameters matrixParameters, Fiber fiber1, Fiber fiber2, out double [] initialStateVariables)
        {
            //Set the integration limits
            characteristicDistance = matrixParameters.CharDist;
            this.b = b;
            this.d = d;
            zBoundsTopToBottom = DetermineIntegrationBounds(characteristicDistance, d, fiber1.Radius, fiber2.Radius, out double matrixArea);
            matrixVolume = matrixArea * fiber1.OLength;

            //Create the material models
            //Decide which model to use based on the name
            matrixModelName = matrixParameters.ModelName;

            if (String.Equals(matrixParameters.ModelName, MatrixModel0.Name, StringComparison.OrdinalIgnoreCase))
            {
                this.fiber1Material = new RigidFiberModel(fiber1.Radius, d, b, zBoundsTopToBottom);
                this.fiber2Material = new RigidFiberModel(fiber2.Radius, d, b, zBoundsTopToBottom);
                topMatrixMaterial = new MatrixModel0(fiber1.Radius, d, b, matrixParameters.Ep, matrixParameters.Nu, zBoundsTopToBottom[0], zBoundsTopToBottom[1], matrixParameters.FailureTheory);
                bottomMatrixMaterial = new MatrixModel0(fiber1.Radius, d, b, matrixParameters.Ep, matrixParameters.Nu, zBoundsTopToBottom[2], zBoundsTopToBottom[3], matrixParameters.FailureTheory);
                nStateVariables = 4;
                initialStateVariables = zBoundsTopToBottom;
            }
            if (String.Equals(matrixParameters.ModelName, MatrixModel1.Name, StringComparison.OrdinalIgnoreCase))
            {
                FailureCritForZIntegratedMatrix dFC;
                try
                {
                    dFC = (FailureCritForZIntegratedMatrix)matrixParameters.FailureTheory;
                }
                catch (Exception)
                {
                    throw new Exception("Must use Failure Theory that can be integrated ");
                }
                this.fiber1Material = new ElasticFiberModel(fiber1.Radius, d, b, zBoundsTopToBottom, true, fiber1);
                this.fiber2Material = new ElasticFiberModel(fiber2.Radius, d, b, zBoundsTopToBottom, false, fiber2);
                int nIntPts = Convert.ToInt32(matrixParameters.modelConstants);

                topMatrixMaterial = new MatrixModel1(matrixParameters.E, matrixParameters.Nu, fiber1.Radius, fiber2.Radius, d, b, zBoundsTopToBottom[0], zBoundsTopToBottom[1], 
                    true, nIntPts, dFC) ;
                bottomMatrixMaterial = new MatrixModel1(matrixParameters.E, matrixParameters.Nu, fiber1.Radius, fiber2.Radius, d, b, zBoundsTopToBottom[2], zBoundsTopToBottom[3],
                    false, nIntPts, dFC);
                
                nStateVariables = matrixParameters.FailureTheory.NStateVariables * (nIntPts + 1);
                initialStateVariables = new double[nStateVariables * 2]; //multiply by 2 because of the top and bottom half
            }
            else
            {
                initialStateVariables = zBoundsTopToBottom;
                throw new Exception("Matrix model name does not match with supported model");
            }

            RecalculateStiffness(initialStateVariables);
        }

        //This constructor is for visualization: PlotFDEM
        public MatrixFiberAssembly(double d, double b,  double charDist, MaterialModel topMatrix, MaterialModel bottomMatrix, 
            MaterialModel fiber1, MaterialModel fiber2, double[] zBoundsTopToBottom)
        {
            //Set the integration limits
            this.characteristicDistance = charDist;
            this.b = b;
            this.d = d;
            this.fiber1Material = fiber1;
            this.fiber2Material = fiber2;
            this.topMatrixMaterial = topMatrix;
            this.bottomMatrixMaterial = bottomMatrix;
            this.zBoundsTopToBottom = zBoundsTopToBottom;
        }

        #endregion

        #region public Methods

        public virtual void SetIteration(ref double[,] k, ref double[,] dampingMatrix, double[] qFibers,
                                         double[] stateVariables)
        {
            //Don't acutally calculate the stiffness here: only do this when failure has been detected.
            if (!isImplicit)
            {
                dampingMatrix = CalculateDampingMatrix(Keq);
            }

            CalculateTotalDOF(qFibers);
            this.stateVariables = stateVariables;
            k = Keq;
        }
        
        public double Calculate_knorm()
        {
            //Just return the normal component   
            return  Keq[2,2];
        }

        /// <summary>
        /// Convert between degrees of freedom at the fibers and degrees of freedom at the fiber/matrix interface.
        /// </summary>
        /// <param name="fiberDOF">fiber centerline dof: [5]{theta1, u2, v2, theta2, ug}</param>
        /// <returns> Matrix dof: [7]{u1, v1, w1, theta1, u2, v2, w2, theta2, ug} </returns>
        

        public bool IsItBroken(ref double[] stateVariables)
        {
            //Someday add a term for the fiber???

            //Split the state variables
            SplitStateVariables(stateVariables, out double[] topStateVariables, out double[] bottomStateVariables);

            //Check if there is failure in the top (and update the damage variables)
            bool hasTopFailure = topMatrixMaterial.IsThereFailure(qTotal, ref topStateVariables);
            bool hasBottomFailure = bottomMatrixMaterial.IsThereFailure(qTotal, ref bottomStateVariables);

            //Combine state variables since this will be passed back
            stateVariables = CombineStateVariables(topStateVariables, bottomStateVariables);

            //Now check if it is totally broken
            bool topTotallyBroken = topMatrixMaterial.IsItTotallyBroken(topStateVariables);
            bool bottomtopTotallyBroken = bottomMatrixMaterial.IsItTotallyBroken(bottomStateVariables);

            bool isBroken = bottomtopTotallyBroken && topTotallyBroken;

            //Recalculate if there has been failure and it isn't broken
            if ((hasTopFailure || hasBottomFailure) && !isBroken)
            {
                //NOT SURE ABOUT THIS CODE!!!!  Like to only recalculate stiffness when there has been failure
                isBroken = true;
                RecalculateStiffness(stateVariables);
            }
            
            return isBroken;
        }

        public void IntegralOfOutOfPlaneStressOverVolume(out double [] SdV_fiber1, out double[] SdV_fiber2, out double[] SdV_mattrix)
        {
            SdV_fiber1 = fiber1Material.CalculateIntegralOfStressOverVolume(qTotal, stateVariables);
            SdV_fiber2 = fiber2Material.CalculateIntegralOfStressOverVolume(qTotal, stateVariables);

            //Split the state variables
            SplitStateVariables(stateVariables, out double[] topStateVariables, out double[] bottomStateVariables);

            //Calculate top and bottom K matrices
            double[] m1Sdv = topMatrixMaterial.CalculateIntegralOfStressOverVolume(qTotal, topStateVariables);
            double[] m2Sdv = bottomMatrixMaterial.CalculateIntegralOfStressOverVolume(qTotal, bottomStateVariables);

            //Sum them up
            SdV_mattrix  = VectorMath.Add(m1Sdv, m2Sdv);

        }
        
        public static double CalculateYAtFiber1(double r, double z)
        {
            return Math.Sqrt(r * r - z * z); ;
        }

        public static double CalculateYAtFiber2(double r, double d, double z)
        {
            return (d - Math.Sqrt(r * r - z * z));
        }

        public  void WriteFirstIterationOutput(StreamWriter dataWrite)
        {
            dataWrite.Write("," + topMatrixMaterial.GetType().Name + "," + fiber1Material.GetType().Name + "," + fiber2Material.GetType().Name + 
                "," + characteristicDistance + "," + b + "," + d );
            dataWrite.Write("#");
            topMatrixMaterial.WriteFirstIterationOutput(dataWrite);
            dataWrite.Write("#");
            fiber1Material.WriteFirstIterationOutput(dataWrite);
            dataWrite.Write("#");
            fiber2Material.WriteFirstIterationOutput(dataWrite);
            dataWrite.WriteLine("#");
        }

        public static MatrixFiberAssembly  ReadFirstIterationOutput(string totalString)
        {
            string[] allStrings = totalString.Split('#');
            string[] assemblyString = allStrings[0].Split(',');

            double charDist = double.Parse(assemblyString[7]);
            double b = double.Parse(assemblyString[8]);
            double d = double.Parse(assemblyString[9]);

            MaterialModel f1 = CreateMaterialModel.CreateFiberMaterialModelFromInput(assemblyString[5], allStrings[2], d, b, charDist, true);
            MaterialModel f2 = CreateMaterialModel.CreateFiberMaterialModelFromInput(assemblyString[6], allStrings[3], d, b, charDist, false);

            double[] zBoundsTopToBottom = DetermineIntegrationBounds(charDist, d, f1.r1, f2.r2, out double matrixArea);

            MaterialModel[] mm = CreateMaterialModel.CreateMatrixMaterialModelFromInput(assemblyString[4], allStrings[1], f1.r1, f2.r2, d, b, zBoundsTopToBottom);

            return new MatrixFiberAssembly(d, b, charDist, mm[0], mm[1], f1, f2, zBoundsTopToBottom);

        }
        #endregion

        #region Private Methods
        public void CalculateTotalDOF(double[] fiberDOF)
        {
            double[] qm = new double[0];

            if (nDimTotal > nOfOuterDOF)
            {
                qm = VectorMath.ScalarMultiply(-1.0, MatrixMath.Multiply(KmmInverse, MatrixMath.Multiply(Kmf, fiberDOF)));
            } 
            //Add UG since it effects the fibers also
            qTotal = VectorMath.Stack(fiberDOF, qm);
        }
        public void RecalculateStiffness(double[] stateVariables)
        {
            Kf1 = fiber1Material.CalculateStiffness(stateVariables);
            Kf2 = fiber2Material.CalculateStiffness(stateVariables);

            //Split state variables and calculate matrix
            SplitStateVariables(stateVariables, out double[] topStateVariables, out double[] bottomStateVariables);
            double[,] topK = topMatrixMaterial.CalculateStiffness(topStateVariables);
            double[,] bottomK = bottomMatrixMaterial.CalculateStiffness(bottomStateVariables);

            Km = MatrixMath.Add(topK, bottomK);

            //Now, combine all of the stiffnesses
            KTotal = MatrixMath.Add(Kf1, MatrixMath.Add(Kf2, Km));

            nDimTotal = KTotal.GetLength(0);

            //Now take out the inner dofs to calculate the equivalent k
            RemoveInnerDOFInK(KTotal);


            foreach (double kij in Keq)
            {
                if (Double.IsNaN(kij) || Double.IsPositiveInfinity(kij) || Double.IsNegativeInfinity(kij))
                {
                    throw new ArgumentException("k has NaN or +/- infinity");
                }
            }
        }
        private void RemoveInnerDOFInK(double[,] KTotal)
        {
            Keq = KTotal;
            if (nDimTotal > nOfOuterDOF)
            {
                Kff = MatrixMath.ExtractMatrix(KTotal, 0, nOfOuterDOF-1, 0, nOfOuterDOF-1);
                Kmm = MatrixMath.ExtractMatrix(KTotal, nOfOuterDOF, nDimTotal - 1, nOfOuterDOF, nDimTotal - 1);
                Kfm = MatrixMath.ExtractMatrix(KTotal, 0, nOfOuterDOF - 1, nOfOuterDOF, nDimTotal - 1);
                Kmf = MatrixMath.ExtractMatrix(KTotal, nOfOuterDOF, nDimTotal - 1, 0, nOfOuterDOF - 1);

                try
                {
                    KmmInverse = MatrixMath.InvertMatrix(Kmm);  
                    Keq = MatrixMath.Subtract(Kff, MatrixMath.Multiply(Kfm, MatrixMath.Multiply(KmmInverse, Kmf)));
                }
                catch (Exception e)
                {
                    if (KmmInverse == null)
                    {
                        KmmInverse = new double[Kmm.GetLength(0), Kmm.GetLength(1)];
                    }
                    //KmmInverse = new double[Kmm.GetLength(0), Kmm.GetLength(1)];
                    //Just keep going if it's not invertible.  It'll use the last stiffness...
                    //at this point, just break the spring.
                    Keq = new double[nOfOuterDOF, nOfOuterDOF];
                }
                
            }
        }
        public static double[] DetermineIntegrationBounds(double charDist, double d, double r1, double r2, out double matrixArea)
        {
            // <param name="zt1">top of top half</param>
            // <param name="zt2">bottom of top half</param>
            // <param name="zb1">top of bottom half</param>
            // <param name="zb2">bottom of bottom half</param>
            double[] zBoundsTopToBottom = new double[4];

            //Only integrate to the smallest radius
            double rmin = r1 < r2 ? r1 : r2;

            zBoundsTopToBottom[0] = rmin - charDist * rmin;
            zBoundsTopToBottom[1] = 0.0;
            zBoundsTopToBottom[2] = 0.0;
            zBoundsTopToBottom[3] = -1.0 * zBoundsTopToBottom[0];

            //If the fibers are touching, then start it out with an initial "crack"
            if (d <= (r2 + r1))
            {
                double overlapRegionHalfLength = 1 / d * Math.Sqrt((-d + r2 - r1) * (-d - r2 + r1) * (-d + r2 + r1) * (d + r2 + r1)) / 2.0;
                zBoundsTopToBottom[1] = overlapRegionHalfLength + charDist * rmin;
                zBoundsTopToBottom[2] = -zBoundsTopToBottom[1];
                if (zBoundsTopToBottom[0] < zBoundsTopToBottom[1])
                {
                    throw new ArgumentException($"Fibers are overlapping too much");
                }
            }

            matrixArea = (CalculateMatrixArea(r1, r2, d, zBoundsTopToBottom[3], zBoundsTopToBottom[2]) 
                + CalculateMatrixArea(r1, r2, d, zBoundsTopToBottom[1], zBoundsTopToBottom[0]));

            return zBoundsTopToBottom;
        }
        protected double[,] CalculateDampingMatrix(double[,] k)
        {
            double[,] d = new double[k.GetLength(0), k.GetLength(1)];

            d[0, 1] = Math.Sign(k[0, 1]) * Spring.SetCriticalDamping(M12, Math.Abs(k[0, 1]), dCoeff);
            d[4, 1] = Math.Sign(k[4, 1]) * Spring.SetCriticalDamping(M12, Math.Abs(k[4, 1]), dCoeff);
            d[1, 2] = Math.Sign(k[1, 2]) * Spring.SetCriticalDamping(M12, Math.Abs(k[1, 2]), dCoeff);
            d[5, 2] = Math.Sign(k[5, 2]) * Spring.SetCriticalDamping(M12, Math.Abs(k[5, 2]), dCoeff);

            d[3, 0] = Math.Sign(k[3, 0]) * Spring.SetCriticalDamping(I12, Math.Abs(k[3, 0]), dCoeff);
            d[7, 3] = Math.Sign(k[7, 3]) * Spring.SetCriticalDamping(M12, Math.Abs(k[7, 3]), dCoeff);


            return d;
        }

        public static void SplitStateVariables(double[] stateVariables, out double[] topStateVariables, out double[] bottomStateVariables)
        {
            topStateVariables = new double[0];
            bottomStateVariables = new double[0];
            if (stateVariables.Length != 0)
            {
            topStateVariables = VectorMath.ExtractVector(stateVariables, 0, stateVariables.Length / 2 - 1);
            bottomStateVariables = VectorMath.ExtractVector(stateVariables, stateVariables.Length / 2, stateVariables.Length - 1);
            }
        }
        protected static double[] CombineStateVariables(double[] topStateVariables, double[] bottomStateVariables)
        {
            double[] stateVariables = VectorMath.Stack(topStateVariables, bottomStateVariables);
            return stateVariables;
        }

        protected static double CalculateMatrixArea(double r1, double r2, double d, double a0, double a1)
        {
            double r12 = r1 * r1;
            double r22 = r2 * r2;
            double a02 = a0 * a0;
            double a12 = a1 * a1;

            return (a0 * (-2 * d + Math.Sqrt(-a02 + r12) + Math.Sqrt(-a02 + r22)) - a1 * (-2 * d + Math.Sqrt(-a12 + r12) + Math.Sqrt(-a12 + r22)) 
                + r12 * (Math.Atan(a0 / Math.Sqrt(-a02 + r12)) - Math.Atan(a1 / Math.Sqrt(-a12 + r12))) + r22 * (Math.Atan(a0 / Math.Sqrt(-a02 + r22)) 
                - Math.Atan(a1 / Math.Sqrt(-a12 + r22)))) / 2.0;
        }

        #endregion

    }
}
