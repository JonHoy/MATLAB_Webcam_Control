using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FilterExample
{
    public partial class Form1 : Form
    {
        public DirectShow.Capture myCam;

        private float[,] Kernel;

        
        public Form1()
        {
            InitializeComponent();
            var myCam = new DirectShow.Capture(0, 640, 480, 24, pictureBox1);
            backgroundWorker1.RunWorkerAsync();
        }



        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Kernel = new float[,]{{-1, -2, -1}, {0, 0, 0}, {1, 2, -1}};
            //var Data = myCam.GrabData();
        }
    }
}
