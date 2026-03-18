using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RandomRVEGenerator
{
    public partial class FinishedWindow : Form
    {
        public FinishedWindow(string fileName)
        {
            InitializeComponent();
            label1.Text = "Congratulations, your RVE Generation of file: " + fileName + ".txt is finished.  I hope it was successful.";
            Activate();
            Show();
        }
    }
}
