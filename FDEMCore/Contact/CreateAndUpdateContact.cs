/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 2/11/2013
 * Time: 11:15 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace FDEMCore.Contact
{
	/// <summary>
	/// Description of CheckForContact.
	/// </summary>
	[SerializableAttribute] //This allows to make a deep copy fast
	public class CreateAndUpdateContact : CreateAndUpdateInteractions
	{
		#region Private Members
		
		private List <int> [,] lGridFibers; //This is a grid corrosponding to the physical grid showing the index of the fiber(s) in each cell
		private List <int []> [] lGridCellWalls; //This is a little different: a list for each boundary, listing the indices on the grid where the Cell Wall is contained
		
		private int [,] nFiberSprings; //the int is -1 when empty, and the index of the contact when a spring is there already
		
		private SizingParameters sizParams;
		private bool hasSizing=false;
		private System.Random myRanNumber;
		
		
		#endregion
		
		#region Public Members
		
		
		#endregion
		
		#region Constructors
		public CreateAndUpdateContact(List<Fiber> inlFibers, Grid inputGrid, CellBoundary inCellBound, ContactParameters inContPar)
			: base(inlFibers, inputGrid, inCellBound, inContPar)
		{
			
			nFiberSprings = new int[lFibers.Count, lFibers.Count];
			SetArrayToNegOne(ref nFiberSprings);
			
			lGridCellWalls = new List<int[]>[cellWalls.Length]; //Remember: the two x-walls are the first 2 in the grid!!!
			lGridFibers = new List<int>[myGrid.Nx,myGrid.Ny];
			
		}
		#endregion
		
		#region Public Methods
		/// <summary>
		/// Must cal this each time sizing is to be added
		/// </summary>
		public void AddSizing(SizingParameters sizingParam){
			hasSizing = true;
			sizParams = sizingParam;
			myRanNumber = new System.Random();
		}
		
		public override void UpdateGrid(int timeStep){
			
			bool isVertical = true;
			tIndex = timeStep;
			
			//Instantiate all Grid lists
			for (int j = 0; j < myGrid.Nx; j++) {
				for (int k = 0; k < myGrid.Ny; k++) {
					lGridFibers[j,k] = new List<int>();
				}
			}
			
			//Now put all of the solid objects into the fiber and boundary grid lists
			for (int j = 0; j < lFibers.Count; j++) {
				
				PutIntoGrid(lFibers[j].CurrentPosition, j);
			}
			for (int j = 2; j < cellWalls.Length; j++) {
				if (j == 4) {isVertical = false;} //TODO This sucks!  Rethink this!!!
				
				lGridCellWalls[j] = PutIntoGrid(cellWalls[j], j, isVertical);
			}
			
		}
		
		public override void UpdateContacts(int timeStep, double dT){
			tIndex = timeStep;
			this.dT = dT;
			UpdateProjectedFibers(); //Do this step here
			
			#region Now Create Fiber to Fiber Contact Pairs
			for (int j = 0; j < myGrid.Nx; j++) { //Go through each square in the grid
				for (int k = 0; k < myGrid.Ny; k++) {
					//Now within each square, check each fiber with its cell and its neighbors
					for (int l = 0; l < lGridFibers[j,k].Count; l++) {
						
						//First, check all in the same cell
						CheckAllFibersInACell(lFibers[lGridFibers[j,k][l]], lGridFibers[j,k][l], j, k, l+1);
						
						//Now compare with any other fibers in Cells to the Right and Down
						if (j < myGrid.Nx - 1) {
							CheckAllFibersInACell(lFibers[lGridFibers[j,k][l]], lGridFibers[j,k][l], j+1, k, 0);
							
							if (k < myGrid.Ny - 1) {
								CheckAllFibersInACell(lFibers[lGridFibers[j,k][l]], lGridFibers[j,k][l], j+1, k+1, 0);
							}
							if (k > 0) {
								CheckAllFibersInACell(lFibers[lGridFibers[j,k][l]], lGridFibers[j,k][l], j+1, k-1, 0);
							}
						}
						if (k < myGrid.Ny - 1) {
							CheckAllFibersInACell(lFibers[lGridFibers[j,k][l]], lGridFibers[j,k][l], j, k+1, 0);
						}
					}
				}
			}
			#endregion
			
			
			//This is to permanantly break the springs
			if (hasSizing && bCanSizingBreak) {
				foreach (FToFRelation ftof in lSprings) {
					ftof.BreakNonContactSpring ();
				}
			}
		}
		
		/* Sizing is decommissioned right now.  Reinstate this when it is back.....
		 public void SaveSizingKDistribution(string fileName){

			double tempK = 0.0;
			double k0 = 0;
			double kt = 0;
			double kr = 0;
			double fmax = 0;
			double area = 0;
			//Calculate K at d=0 to normalize it
			FToFSizingSpring_EqArea.CalculateSizingKF (ref k0, ref kt, ref kr, ref fmax, ref area, lFibers [0].Radius*2.0, sizParams.E, sizParams.Nu, sizParams.MaxStress,
			                                    lFibers [0].Radius, lFibers [0].Radius, lFibers [0].OLength);
			List <double> kSizList = new List<double> ();
			//Add each sizing if it is there
			foreach (FToFRelation f in lSprings) {
				if (f.HasSizing (ref tempK)) {
					if (tempK / k0 > 1.0) {
						tempK = 0.0;
					}
					kSizList.Add (tempK/k0);
				}
			}
			//Now save the sizing distribution in a file
			StreamWriter dataWrite = new StreamWriter(fileName);
			foreach (double k in kSizList) {
				dataWrite.WriteLine (k);
			}
			dataWrite.Close();
		}*/
		
		public void AssignBoundaryToGridAndAddProjectedFibers()
        {
			this.UpdateGrid(0);
			UpdateProjectedFibers();
        }
		#endregion
		
		#region Private Methods
		private void UpdateProjectedFibers(){

            //Clear the current projected fibers
            foreach (Fiber fiber in lFibers)
            {
				fiber.ClearProjectedFibers();
            }

			#region First Check the boundary.  There will always be a check in the springs so if one is made already, the check won't be duplicated
			
			for (int j = 2; j < lGridCellWalls.Length; j++) {
				
				List<int []> indexTocheck = new List<int []>();
				
				for (int k = 0; k < lGridCellWalls[j].Count; k++) {
					int j1 = lGridCellWalls[j][k][0];
					int k1 = lGridCellWalls[j][k][1];
					
					indexTocheck.Add(new int[2]{j1, k1});
					
					//If the walls are vertical
					if (j == 2 || j == 3) {
						if (j1 < myGrid.Nx - 1) {indexTocheck.Add(new int[2]{j1+1, k1});}
						if (j1 > 0) {indexTocheck.Add(new int[2]{j1-1, k1});}
					}else{ //If the walls are horizontal
						if (k1 < myGrid.Ny - 1) {indexTocheck.Add(new int[2]{j1, k1+1});}
						if (k1 > 0) {indexTocheck.Add(new int[2]{j1, k1-1});}
					}
				}
				//Remove all of the indexes that are above/below the grid, and remove the duplicates
				indexTocheck = RemoveDuplicates(indexTocheck);
				//Then check them all
				foreach (int [] ind in indexTocheck) {
					CheckAllFibersInACell(cellWalls[j], j, ind[0], ind[1], 0);
				}
			}
			
			#endregion
			
			#region Then assign the projected fibers
			//Now put all of the solid objects into the fiber and boundary grid lists
			for (int j = 0; j < lFibers.Count; j++) {
				if (lFibers[j].HasProjectedFibers) {
					
					//Now put them into the grid (do the original fiber just in case the original switched)
					PutIntoGrid(lFibers[j].CurrentPosition, j);
					PutIntoGrid(lFibers[j].ProjectedFibers[lFibers[j].ProjectedFibers.Count-1].CurrentPosition, j);
					
					if (lFibers[j].IsCornerFiber) {
						PutIntoGrid(lFibers[j].ProjectedFibers[lFibers[j].ProjectedFibers.Count-2].CurrentPosition, j);
						PutIntoGrid(lFibers[j].ProjectedFibers[lFibers[j].ProjectedFibers.Count-3].CurrentPosition, j);
					}
				}
			}
			
			#endregion
		}
		
		private void PutIntoGrid(double [] location, int soIndex){

			int[] indices = myGrid.ConvertToZoneIndices(location, out bool wasSuccessful);  ;
			//If it isn't a duplicate already, then add it
			if (!lGridFibers[indices[0], indices[1]].Contains(soIndex)) {
				lGridFibers[indices[0], indices[1]].Add(soIndex);
			}
				//MessageBox.Show ("Point left the grid: decrease time step of enlarge grid.  Point is (" + location[0] + ", " + location[1] + ", " + location[2] + "). ");
		}
		
		private List<int[]> PutIntoGrid(CellWall cw, int soIndex, bool isVertical){
			//Go from the middle out checking at intervals of D/2
			List<int[]> GridIndexList = new List<int[]>();
			int n = (int)(myGrid.Nx*11/10); //factor of 3/4 is just to still detect when the wall is slanted
			double d = myGrid.Dx*9/10;
			
			if (isVertical) {
				n = (int)(myGrid.Ny*11/10);
				d = myGrid.Dy*9/10;
			}
			bool wasSuccessful = true;
			double [] aboveVect = new double[3]{0,1,1};
			double [] belowVect = new double[3]{0,-1,-1};
			//Add center
			int [] coor = GridCoorOfBoundary(0 , cw, aboveVect, ref wasSuccessful); //find the point at the center
			if (wasSuccessful) {GridIndexList.Add(coor);}
			
			for (int j = 1; j < n; j++) {											// Carsten: changed "n" to "n/2"
				//Above Center and Below
				coor = GridCoorOfBoundary(d * j , cw, aboveVect, ref wasSuccessful); //find the point above the center
				if (wasSuccessful) {GridIndexList.Add(coor);}
				
				coor = GridCoorOfBoundary(d * j , cw, belowVect, ref wasSuccessful); //find the point below the center
				if (wasSuccessful) {GridIndexList.Add(coor);}
				
			}
			
			//Remove duplicates
			GridIndexList = RemoveDuplicates(GridIndexList);
            //Remove any numbers outside of the grid (this is a late addition: may be unnecesary)
            for (int i = 0; i < GridIndexList.Count; i++)
            {
				if (GridIndexList[i][0] >= myGrid.Nx || GridIndexList[i][1] >= myGrid.Ny)
				{
					GridIndexList.RemoveAt(i);
				}
			}
            
			return GridIndexList;
		}
		
		private int [] GridCoorOfBoundary(double length, CellWall cw, double [] dir, ref bool wasSuccessful){
			
			double [] coor = cw.FindCoordinateOnWall(length, dir); //TODO this is 3-D, but not a great thing
			int [] intCoor =  myGrid.ConvertToZoneIndices(coor, out wasSuccessful); //Do this to not add points outside of the grid
			
			return intCoor;
		}
		
		private void CheckAllFibersInACell(Fiber f1, int nF1, int jGrid, int kGrid, int FirstCountInList){
			
			//First compare with any other fibers in the Cell of the Grid
			
			for (int j1 = FirstCountInList; j1 < lGridFibers[jGrid,kGrid].Count; j1++) {
				
				int nF = lGridFibers[jGrid,kGrid][j1];
				if (nF != nF1) { //Prevent duplicates
					Fiber f2 = lFibers[nF];
					
					//Now separate into types, if there is a spring already, update it.  If not, create one.
					int sIndex = 0;
					
					if (nFiberSprings[nF1, nF] == -1) {
						lSprings.Add(new FToFRelation(contactParams, (Fiber)f1, f2, nF1, nF));
						
						sIndex = lSprings.Count - 1;
						nFiberSprings[nF1, nF] = sIndex;
						nFiberSprings[nF, nF1] = sIndex;
					}
					sIndex = nFiberSprings[nF1, nF];
					//Add sizing if it has it and is a sizing run
					if (hasSizing) {
						
						lSprings[sIndex].AddNonContactSpring(sizParams, myRanNumber.NextDouble());
					}
					lSprings[sIndex].Update(tIndex, dT);
				}
			}
		}
		
		private void CheckAllFibersInACell(CellWall cw, int nCW, int jGrid, int kGrid, int FirstCountInList){
			
			//compare with any other fibers in the Cell of the Grid
			
			for (int j1 = FirstCountInList; j1 < lGridFibers[jGrid,kGrid].Count; j1++) {
				
				int nF = lGridFibers[jGrid,kGrid][j1];
				Fiber f2 = lFibers[nF];
				
				bool hasContact = cw.CheckContact(f2);
			}
		}
		
		private void SetArrayToNegOne(ref int [,] a){
			for (int k = 0; k < a.GetLength(0); k++) {
				for (int j = 0; j < a.GetLength(1); j++) {
					a[k,j] = -1;
				}
			}
		}
		
		/// <summary>Gets rid of duplicates in an index list </summary>
		private List<int []> RemoveDuplicates(List<int []> lIndexes){
			//Gets rid of duplicates in an index list (int[] is always 2) and indexes outside of the grid
			int index = 0;
			while (index < lIndexes.Count) {
				int i1 = 0;
				
				while (i1 < index){
					if (lIndexes[index][0] == lIndexes[i1][0] && lIndexes[index][1] == lIndexes[i1][1]) {
						lIndexes.RemoveAt(index);
						i1 = index;
						index --;
					}
					i1++;
				}
				index ++;
			}
			return lIndexes;
		}

		#endregion
	}
}