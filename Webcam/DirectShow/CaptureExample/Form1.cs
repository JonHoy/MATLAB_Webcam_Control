using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video.DirectShow;

namespace CaptureExample
{
    public partial class Form1 : Form
    {
        private DirectShow.Webcam myCam;
        public Form1()
        {
            InitializeComponent();
            string[] devicenames = DirectShow.Webcam.getDeviceNames();
            foreach (var name in devicenames)
            {
                comboBox1.Items.Add(name);
            }
            if (devicenames.Length > 0)
                comboBox1.SelectedIndex = 0;
            if (comboBox2.Items.Count > 0)
            {
                comboBox2.SelectedIndex = 0;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string[] resolutions = DirectShow.Webcam.getResolutions(comboBox1.SelectedIndex);
            comboBox2.Items.Clear();
            foreach (var resolution in resolutions)
            {
                comboBox2.Items.Add(resolution);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            myCam = new DirectShow.Webcam(comboBox1.SelectedIndex, comboBox2.Text, videoSourcePlayer1);
            myCam.setCameraProperty(CameraControlProperty.Focus, 0, CameraControlFlags.Manual); // turn off focus and put it to infinity
            myCam.Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var Data = myCam.GrabData();
            var Frame = myCam.GrabFrame();
            pictureBox1.Image = Frame;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            myCam.Stop();
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (myCam != null)
                myCam.Stop();
        }

        private void save_button_Click(object sender, EventArgs e)
        {
            var savebox = new SaveFileDialog();
            savebox.Title = "Save Image File";
            savebox.Filter = "Image Files (*.jpg)|*.jpg";
            if (savebox.ShowDialog() == DialogResult.OK)
                pictureBox1.Image.Save(savebox.FileName);
        }
    }
}
