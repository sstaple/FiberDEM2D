using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FDEMCore;
using System.IO;

namespace FDEMWindows
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //add an open dialogue box
            OpenFileDialog openFldr = new OpenFileDialog()
            {
                Title = "Select File to Read From",
                Filter = "TXT Files (*.txt*)|*.txt*",
                FilterIndex = 2,
                RestoreDirectory = true,
                //InitialDirectory = Directory.GetCurrentDirectory()
            };


            //openFldr.RestoreDirectory = true;  //This should have worked before, but seems to not be working....


            if (openFldr.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string fullFileName = openFldr.FileName;
                    string dirName = System.IO.Path.GetDirectoryName(openFldr.FileName); ;
                    string sFileName = System.IO.Path.GetFileName(openFldr.FileName);
                    InputFile myInputFile = new InputFile(sFileName, dirName);
                    myInputFile.Initiate();
                    MessageBox.Show("Congratulations, your run is finished.  I hope it was successful.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                
            }
            

            //Application.SetHighDpiMode(HighDpiMode.SystemAware);
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());
        }
    }
}
