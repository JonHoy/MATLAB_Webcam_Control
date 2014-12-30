using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectShow;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;


namespace Webcam_Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var Cameras = Capture.GetCameras();
            var Form = new PreviewForm();
            var Form1 = new PreviewForm();
            var Form2 = new PreviewForm();
            Form.Visible = true;
            Form1.Visible = true;
            Form2.Visible = true;
            Form2.Location = new Point(800, 100);
            var cam = new Capture(0,640,480,24,Form.pictureBox1);
            cam.setCameraControl(DirectShowLib.CameraControlProperty.Focus, 0, DirectShowLib.CameraControlFlags.Manual);
            SubRoutine Algorithm = new SubRoutine(AddNoise);
            SubRoutine Algorithm_Bright = new SubRoutine(Brighten);
            var myStream = new VideoStream(ref cam, Algorithm);
            var myStream2 = new VideoStream(ref cam, Algorithm_Bright);
                while (Form.IsDisposed == false)
                {
                    var Photo = myStream.GetPhoto();
                    var Photo2 = myStream2.GetPhoto();
                    Form1.pictureBox1.Image = Photo;
                    Form2.pictureBox1.Image = Photo2;
                    Form1.Refresh();
                    Form2.Refresh();
                    Console.WriteLine("Data Grabbed");
                    Thread.Sleep(1000 / 30);
                }

            
        }

        static byte[] RandNoise(byte[] Stream)
        {
            Random rng = new Random();
            var Output = new byte[Stream.Length];
            rng.NextBytes(Output);
            return Output;
        }
        static byte[] AddNoise(byte[] Stream)
        {
            Random rng = new Random();
            var Output = new byte[Stream.Length];
            for (int i = 0; i < Output.Length; i++)
            {
                Output[i] = (byte)(Stream[i] + (rng.NextDouble()-0.5)*55);
            }
            return Output;
        }
        static byte[] Brighten(byte[] Stream)
        {
            var Output = new byte[Stream.Length];
            for (int i = 0; i < Stream.Length; i++)
            {
                Output[i] = (byte)(Stream[i] + 75);
            }
            return Output;
        }
        static byte[] Sobel(byte[] Stream)
        {
            var Output = new byte[Stream.Length];
            return Output;
        }

    }
}
