/*
 * Created by SharpDevelop.
 * User: Scott_Stapleton
 * Date: 10/9/2019
 * Time: 12:15 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace FDEMCore.Contact
{
	/// <summary>
	/// Description of FToFBreakableSpring.
	/// </summary>
	public abstract class FToFBreakableSpring : FToFSpring
	{
		#region Private Members
		
		protected bool isBroken; //Needed for iBreakableSpring
        protected List<bool> lIsBroken;
        #endregion

        #region Public Members

        public bool IsBroken {
			get {return isBroken;}
		}
		
		#endregion
		
		#region Constructors
		/// <summary>Creates a contact spring between fibers</summary>
		protected FToFBreakableSpring(Fiber fiber1, Fiber fiber2, int nfiber1, int nfiber2):base(fiber1, fiber2, nfiber1, nfiber2){

            lIsBroken = new List<bool>();
		}
		
		#endregion
		
		#region Public Methods
		public abstract bool BreakSpring();

        public override void SaveTimeStep(int iSaved, int iCurrent)
        {

            if (!isBroken && (iCurrent == base.tIndex))
            {  //calling bse.tIndex is the same as checking current contact
                base.SaveTimeStep(iSaved, iCurrent);
                lIsBroken.Add(isBroken);
            }
        }
        #endregion

        #region Private Methods

        #endregion

        #region Static Methods

        #endregion
    }
}
