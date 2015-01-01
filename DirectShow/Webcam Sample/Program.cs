﻿using System;
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
            var cam = new Capture(0,1280,720,24,Form.pictureBox1);
            cam.setCameraControl(DirectShowLib.CameraControlProperty.Focus, 0, DirectShowLib.CameraControlFlags.Manual);
            SubRoutine Algorithm = new SubRoutine(AddNoise);
            SubRoutine Algorithm_Bright = new SubRoutine(Brighten);
            Form.ShowDialog();

            
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
