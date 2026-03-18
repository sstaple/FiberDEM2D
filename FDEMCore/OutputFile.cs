/*
 * Created by SharpDevelop.
 * User: Scott
 * Date: 5/28/2014
 * Time: 9:50 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;

namespace FDEMCore
{
	/// <summary>
	/// Description of OutputFile.
	/// </summary>
	public class OutputFile
	{
		private bool bSSOnly;
		
		/*public OutputFile(Analysis myAnalysis, bool bPlotSSOnly)
		{
			bSSOnly=bPlotSSOnly;
			//add a save dialogue box
			// Create new SaveFileDialog object
			SaveFileDialog DialogSave = new SaveFileDialog()
            {
                // Default file extension
                DefaultExt = "csv",
                // Available file extensions
                Filter = "CSV Files (*.csv*)|*.csv*",
                // Adds a extension if the user does not
                AddExtension = true,
                // Restores the selected directory, next time
                RestoreDirectory = true,
                Title = "Where do you want to save the file?",
                FileName = "Output"
            };



            if (DialogSave.ShowDialog() == DialogResult.OK)
			{
				
			StreamWriter dataWrite = new StreamWriter(DialogSave.FileName);
				try {
					WriteOutputFile(myAnalysis, dataWrite);
				} catch (Exception ex) {
					dataWrite.Close();

					throw new Exception("Error:" + ex.ToString());
				}
			}
			
		}*/
		public OutputFile(Analysis myAnalysis, string fileName, bool bPlotSSOnly)
		{
			bSSOnly=bPlotSSOnly;
			StreamWriter dataWrite = new StreamWriter(fileName);
			WriteOutputFile(myAnalysis, dataWrite);	
		}
		
		private void WriteOutputFile(Analysis myAnalysis, StreamWriter dataWrite){
			
			string sComment = "**";
			string sCommand = "*";
			
			#region Write out the heading
			
			dataWrite.WriteLine(sComment);
			dataWrite.WriteLine(sComment + "Created: " + DateTime.Now);
			dataWrite.WriteLine(sComment);
			dataWrite.WriteLine(sComment + "Fiber DEM");
			dataWrite.WriteLine(sComment + "Program written by Scott Stapleton");
			dataWrite.WriteLine(sComment + "at Institut für Textiltechnik der RWTH Aachen University");
			#endregion

			if (bSSOnly) {
				myAnalysis.WriteStressStrain(dataWrite, sComment, sCommand);
			} else {
				myAnalysis.WriteOutput(dataWrite, sComment, sCommand);
			}
			
			WriteHeader("END", sComment, sCommand, dataWrite);
			
			dataWrite.Close();
		}
		
		public static void WriteHeader(string sTitle, string sComment, string sCommand, StreamWriter dataWrite){
			dataWrite.WriteLine(sComment);
			dataWrite.WriteLine(sComment + "-----------------------------------------");
			dataWrite.WriteLine(sComment);
			dataWrite.WriteLine(sCommand + sTitle);
		}
	}
}
