/*
 * Created by SharpDevelop.
 * User: Scott_Stapleton
 * Date: 10/10/2019
 * Time: 9:17 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using RandomMath;
using System.Linq;
using System.IO;
using FDEMCore.Contact.Meshing;

namespace FDEMCore.Contact
{
	/// <summary>
	/// Description of CreateAndUpdateSprings.
	/// </summary>
	public class CreateAndUpdateSprings : CreateAndUpdateInteractions
	{
		#region Private Members
		List<MatrixProjectedFiber> lMatrixProjFibers;
		MatrixAssemblyParameters matrixParams;
		 
		
		#endregion
		
		#region Public Members
		

		#endregion
		
		#region Constructors
		public CreateAndUpdateSprings(List<Fiber> inlFibers, Grid inputGrid, CellBoundary inCellBound, ContactParameters inContPar, INonContactSpringParameters ncSpringParams)
			: base(inlFibers, inputGrid, inCellBound, inContPar)
        {
            lMatrixProjFibers = new List<MatrixProjectedFiber>();
            matrixParams = ncSpringParams as MatrixAssemblyParameters;
			//This if statement is just for unit tests.  It creates the matrix pairs without projections....
            if (matrixParams.dontMakeProjections)
            {
				Meshing.FDEMMatrixMeshing.CreateMatrixPairs(ref lFibers, ref lSprings, ref lMatrixProjFibers, inCellBound,
												   contactParams, matrixParams, matrixParams.dontMakeProjections);
			}
            else
            {
                Meshing.FDEMMatrixMeshing.CreateMatrixPairs(ref lFibers, ref lSprings, ref lMatrixProjFibers, inCellBound,
												   contactParams, matrixParams);
			}
				
		}

		/// <summary>
		/// This is just for debugging: it doesn't make projections, to simplify for unit testing.
		/// </summary>
		
		#endregion

		#region Public Methods

		public override void UpdateGrid(int timeStep){

		}
		
		public override void UpdateContacts(int timeStep, double dT){
			tIndex = timeStep;
			this.dT = dT;
			//TODO does this first line need to be there?

			FToFWithMatrix.UpdateProjectedFibers(ref lFibers, cellBound, lMatrixProjFibers);
			FToFWithMatrix.UpdateMatrix(ref lSprings, timeStep, dT);
			
			
			//This is to permanantly break the springs
			if (bCanSizingBreak) {
				foreach (FToFRelation ftof in lSprings) {
					ftof.BreakNonContactSpring ();
				}
			}
		}
		
		#endregion
		
		#region Private Methods
		
		#endregion
	}
}