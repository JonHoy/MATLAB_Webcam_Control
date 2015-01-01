using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

using AForge.Controls;
using AForge.Video.DirectShow;

namespace DirectShow
{
    public class Webcam
    {
        private VideoCaptureDevice Device;
        private VideoSourcePlayer Player;
        private string[] Resolutions;
        private string[] DeviceNames; // names of all the capture devices
        private int deviceIDX;
        private int resolutionIDX;
        private byte[] Buffer;

        private object sync; // object for locking
        public static string[] getResolutions(int deviceIDX = 0)
        {
            // enumerate video devices
            var videoDevices = new FilterInfoCollection( FilterCategory.VideoInputDevice );
            if (videoDevices.Count > 0)
            {
                if (deviceIDX < 0)
                {
                    throw new Exception("deviceIDX must be greater than or equal to 0");
                }
                if (videoDevices.Count > deviceIDX)
                {
                    var myDevice = new VideoCaptureDevice(videoDevices[deviceIDX].MonikerString);
                    var myCapabilities = myDevice.VideoCapabilities;
                    var ResolutionStrings = new string[myCapabilities.Length];
                    for (int i = 0; i < ResolutionStrings.Length; i++)
                    {
                        var FrameSize = myCapabilities[i].FrameSize;
                        ResolutionStrings[i] = FrameSize.Width.ToString() + " x " + FrameSize.Height.ToString();
                    }
                    return ResolutionStrings;
                }
                else
                {
                    throw new Exception("Only " + videoDevices.Count.ToString() + " devices were detected!");
                }
            }
            else
            {
                throw new Exception("No Capture Devices detected!");
            }
        }
        public static string[] getDeviceNames()
        {
            // enumerate video devices
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            string[] DeviceNames;
            DeviceNames = new string[videoDevices.Count];
            for (int iDevice = 0; iDevice < videoDevices.Count; iDevice++)
            {
                DeviceNames[iDevice] = videoDevices[iDevice].Name;
            }
            return DeviceNames;
        }

        public Webcam(int deviceIDX, string Resolution, VideoSourcePlayer VideoPlayer = null)
        {
            if (VideoPlayer == null) // if the player is null then we create our own player encapsulated inside the 
                this.Player = new VideoSourcePlayer();
            else
                this.Player = VideoPlayer;
            Resolutions = getResolutions(deviceIDX);
            bool stringfound = false;
            for (int i = 0; i < Resolutions.Length; i++)
            {
                if (Resolutions[i] == Resolution)
                {
                    stringfound = true;
                    resolutionIDX = i;
                    break;
                }
            }
            if (stringfound)
            {
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count > 0)
                {
                    Device = new VideoCaptureDevice(videoDevices[deviceIDX].MonikerString);
                    var ResolutionsList = Device.VideoCapabilities;
                    Device.VideoResolution = ResolutionsList[resolutionIDX]; // set the video capture resolution
                    Player.VideoSource = Device;
                }
                else
                    throw new Exception("No devices found");
            }
            else
                throw new Exception("Camera does not support the specified resolution, please format the string like '640 x 480' ");
        }

        public byte[,,] GrabData()
        {
            var Image = GrabFrame();
            BitmapData bmpdata = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadOnly, Image.PixelFormat);
            int numbytes = bmpdata.Stride * Image.Height;
            if (Buffer == null || Buffer.Length != numbytes)
                Buffer = new byte[numbytes];
            IntPtr ptr = bmpdata.Scan0;
            System.Runtime.InteropServices.Marshal.Copy(ptr, Buffer, 0, numbytes);
            int Planes = numbytes / Image.Height / Image.Width;
            Image.UnlockBits(bmpdata);
            var Data = new byte[Image.Height, Image.Width, Planes];
            int idx = 0;
            for (int i = 0; i < Image.Height; i++)
            {
                for (int j = 0; j < Image.Width; j++)
                {
                    for (int k = 0; k < Planes; k++)
                    {
                        Data[i, j, k] = Buffer[idx];
                        idx++;
                    }
                }
            }
            return Data;
        }
        public Bitmap GrabFrame()
        {
            return Player.GetCurrentVideoFrame();
        }

        public void Start()
        {
            Player.Start();
        }
        public void Stop()
        {
            Device.SignalToStop();
            Device.WaitForStop();
        }

        public void setCameraProperty(CameraControlProperty Prop, int Val, CameraControlFlags Flag)
        {
            Device.SetCameraProperty(Prop, Val, Flag);
        }

        public void getCameraProperty(CameraControlProperty Prop, out int Val, out CameraControlFlags Flag)
        {
            Device.GetCameraProperty(Prop, out Val, out Flag);
        }
        public void getCameraPropertyRange(CameraControlProperty Prop, 
            out int minValue, out int maxValue, out int stepSize, out int defaultValue, out CameraControlFlags Flag)
        {
            Device.GetCameraPropertyRange(Prop, out minValue, out maxValue, out stepSize, out defaultValue, out Flag);
        }
    }
}
