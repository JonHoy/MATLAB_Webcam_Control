using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;

using DirectShowLib;

namespace DirectShow
{
    public class Capture : ISampleGrabberCB, IDisposable
    {
        #region Member variables

        /// <summary> graph builder interface. </summary>
        private IFilterGraph2 m_FilterGraph = null;

        // Used to snap picture on Still pin
        private IAMVideoControl m_VidControl = null;
        private IPin m_pinStill = null;

        private IBaseFilter capFilter = null;


        // define interfaces for changing image processing and camera control
        private IAMCameraControl m_CameraControl = null;
        private IAMVideoProcAmp m_VideoProcess = null;

        /// <summary> so we can wait for the async job to finish </summary>
        private ManualResetEvent m_PictureReady = null;

        private bool m_WantOne = false;

        /// <summary> Dimensions of the image, calculated once in constructor for perf. </summary>
        private int m_videoWidth;
        private int m_videoHeight;
        private int m_stride;

        /// <summary> buffer for bitmap data.  Always release by caller</summary>
        private IntPtr m_ipBuffer = IntPtr.Zero;

        /// <summary> camera Bits per-pixel </summary>
        private int BPP;
        
        /// <summary> Camera Index Number </summary>
        private int CamIndex;

        /// <summary> Optional GUI this class can have </summary>
        private PreviewForm previewGUI = null;

        /// <summary> Managed Buffer For Live Stream Data </summary>
        public byte[] Buffer { get; private set; }


        private Task BufferUpdater;

#if DEBUG
        // Allow you to "Connect to remote graph" from GraphEdit
        DsROTEntry m_rot = null;
#endif
        #endregion

        #region APIs
        [DllImport("Kernel32.dll", EntryPoint="RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr Destination, IntPtr Source, [MarshalAs(UnmanagedType.U4)] int Length);
        #endregion

        // Zero based device index and device params and output window
        public Capture(int iDeviceNum = 0, int iWidth = 640, int iHeight = 480, short iBPP = 24, Control hControl = null)
        {
            Buffer = new Byte[iHeight * iWidth * iBPP / 8];
            
            DsDevice[] capDevices;
            if (hControl == null)
            {
                previewGUI = new PreviewForm();
                hControl = previewGUI.pictureBox1;
            }
            // Get the collection of video devices
            capDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            BPP = iBPP;
            CamIndex = iDeviceNum;

            if (iDeviceNum + 1 > capDevices.Length)
            {
                throw new Exception("No video capture devices found at that index!");
            }

            try
            {
                // Set up the capture graph
                SetupGraph( capDevices[iDeviceNum], iWidth, iHeight, iBPP, hControl);

                // tell the callback to ignore new images
                m_PictureReady = new ManualResetEvent(false);

                BufferUpdater = Task.Run(() =>
                {
                    while (true)
                    {
                        UpdateBuffer();
                    }
                });
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary> release everything. </summary>
        public void Dispose()
        {
#if DEBUG
            if (m_rot != null)
            {
                m_rot.Dispose();
            }
#endif
            CloseInterfaces();
            if (m_PictureReady != null)
            {
                m_PictureReady.Close();
            }
        }
        // Destructor
        ~Capture()
        {
            Dispose();
        }


        public byte[,,] GrabData()
        {
            int Depth = BPP / 8;
            byte[,,] ImgData = new byte[Height , Width, Depth];
            var ImgBuffer2 = new byte[ImgData.Length];
            int numelements = ImgData.Length;
            unsafe
            {
                fixed (byte* destPtr = &ImgBuffer2[0])
                fixed (byte* srcPtr = &Buffer[0])
                    CopyMemory((IntPtr)destPtr, (IntPtr)srcPtr, ImgData.Length); 
            }
            int idx = 0;
            for (int i = 0; i < Height; i++)
			{
			    for (int j = 0; j < Width; j++)
			    {
			        for (int k = 0; k < Depth; k++)
                    {
                        ImgData[i, j, k] = Buffer[idx];
                        idx++;
                    }
			    }
			}
            return ImgData;
        }

        private void UpdateBuffer()
        {
            IntPtr srcPtr = Click();
            unsafe
            {
                    fixed (byte* destPtr = &Buffer[0])
                        CopyMemory((IntPtr)destPtr, srcPtr, Buffer.Length);
                        //Array.Reverse(Buffer);
            }
            Marshal.FreeCoTaskMem(srcPtr);
        }

        public void ShowCameraFeed()
        { 
            if (previewGUI == null)
                throw new Exception("Data feed already being sent to a control!");
            if (previewGUI.IsDisposed)
                previewGUI = new PreviewForm();
            previewGUI.Visible = true;
        }

        public Bitmap GetPhoto()
        {
            Bitmap Photo;
            var BufferCopy = new byte[Buffer.Length];
            BufferCopy = Buffer;
            unsafe
            {
                fixed (byte* srcPtr = &BufferCopy[0])
                Photo = new Bitmap(Width, Height, Stride, PixelFormat.Format24bppRgb, (IntPtr)srcPtr);
            }
            return (Bitmap)Photo;
        }

        public static DsDevice[] GetCameras() 
        {
            // Get the collection of video devices
            var capDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            return capDevices;
        }

        private IntPtr Click()
        {
            int hr;

            // get ready to wait for new image
            m_PictureReady.Reset();
            m_ipBuffer = Marshal.AllocCoTaskMem(Math.Abs(m_stride) * m_videoHeight);

            try
            {
                m_WantOne = true;

                // If we are using a still pin, ask for a picture
                if (m_VidControl != null)
                {
                    // Tell the camera to send an image
                    hr = m_VidControl.SetMode(m_pinStill, VideoControlFlags.Trigger);
                    DsError.ThrowExceptionForHR( hr );
                }

                // Start waiting
                if ( ! m_PictureReady.WaitOne(9000, false) )
                {
                    throw new Exception("Timeout waiting to get picture");
                }
            }
            catch
            {
                Marshal.FreeCoTaskMem(m_ipBuffer);
                m_ipBuffer = IntPtr.Zero;
                throw;
            }
	
            // Got one
            return m_ipBuffer;
        }

        public int Width
        {
            get
            {
                return m_videoWidth;
            }
        }
        public int Height
        {
            get
            {
                return m_videoHeight;
            }
        }
        public int Stride
        {
            get
            {
                return m_stride;
            }
        }

        public void setCameraControl(CameraControlProperty Prop, int Val, CameraControlFlags Mode)
        {
            m_CameraControl.Set(Prop, Val, Mode);
        }

        /// <summary> build the capture graph for grabber. </summary>
        private void SetupGraph(DsDevice dev, int iWidth, int iHeight, short iBPP, Control hControl)
        {
            int hr;

            ISampleGrabber sampGrabber = null;
            IPin pCaptureOut = null;
            IPin pSampleIn = null;
            IPin pRenderIn = null;

            // Get the graphbuilder object
            m_FilterGraph = new FilterGraph() as IFilterGraph2;

            try
            {
#if DEBUG
                m_rot = new DsROTEntry(m_FilterGraph);
#endif
                // add the video input device
                hr = m_FilterGraph.AddSourceFilterForMoniker(dev.Mon, null, dev.Name, out capFilter);
                DsError.ThrowExceptionForHR( hr );

                // Find the still pin
                m_pinStill = DsFindPin.ByCategory(capFilter, PinCategory.Still, 0);

                // Didn't find one.  Is there a preview pin?
                if (m_pinStill == null)
                {
                    m_pinStill = DsFindPin.ByCategory(capFilter, PinCategory.Preview, 0);
                }

                // Still haven't found one.  Need to put a splitter in so we have
                // one stream to capture the bitmap from, and one to display.  Ok, we
                // don't *have* to do it that way, but we are going to anyway.
                if (m_pinStill == null)
                {
                    IPin pRaw = null;
                    IPin pSmart = null;

                    // There is no still pin
                    m_VidControl = null;

                    // Add a splitter
                    IBaseFilter iSmartTee = (IBaseFilter)new SmartTee();

                    try
                    {
                        hr = m_FilterGraph.AddFilter(iSmartTee, "SmartTee");
                        DsError.ThrowExceptionForHR( hr );

                        // Find the find the capture pin from the video device and the
                        // input pin for the splitter, and connnect them
                        pRaw = DsFindPin.ByCategory(capFilter, PinCategory.Capture, 0);
                        pSmart = DsFindPin.ByDirection(iSmartTee, PinDirection.Input, 0);

                        hr = m_FilterGraph.Connect(pRaw, pSmart);
                        DsError.ThrowExceptionForHR( hr );

                        // Now set the capture and still pins (from the splitter)
                        m_pinStill = DsFindPin.ByName(iSmartTee, "Preview");
                        pCaptureOut = DsFindPin.ByName(iSmartTee, "Capture");

                        // If any of the default config items are set, perform the config
                        // on the actual video device (rather than the splitter)
                        if (iHeight + iWidth + iBPP > 0)
                        {
                            SetConfigParms(pRaw, iWidth, iHeight, iBPP);
                        }
                    }
                    finally
                    {
                        if (pRaw != null)
                        {
                            Marshal.ReleaseComObject(pRaw);
                        }
                        if (pRaw != pSmart)
                        {
                            Marshal.ReleaseComObject(pSmart);
                        }
                        if (pRaw != iSmartTee)
                        {
                            Marshal.ReleaseComObject(iSmartTee);
                        }
                    }
                }
                else
                {
                    // Get a control pointer (used in Click())
                    m_VidControl = capFilter as IAMVideoControl;

                    pCaptureOut = DsFindPin.ByCategory(capFilter, PinCategory.Capture, 0);

                    // If any of the default config items are set
                    if (iHeight + iWidth + iBPP > 0)
                    {
                        SetConfigParms(m_pinStill, iWidth, iHeight, iBPP);
                    }
                }

                // setup camera and imaging controls
                m_VideoProcess = capFilter as IAMVideoProcAmp;
                m_CameraControl = capFilter as IAMCameraControl;

                // Get the SampleGrabber interface
                sampGrabber = new SampleGrabber() as ISampleGrabber;

                // Configure the sample grabber
                IBaseFilter baseGrabFlt = sampGrabber as IBaseFilter;
                ConfigureSampleGrabber(sampGrabber);
                pSampleIn = DsFindPin.ByDirection(baseGrabFlt, PinDirection.Input, 0);

                // Get the default video renderer
                IBaseFilter pRenderer = new VideoRendererDefault() as IBaseFilter;
                hr = m_FilterGraph.AddFilter(pRenderer, "Renderer");
                DsError.ThrowExceptionForHR( hr );

                pRenderIn = DsFindPin.ByDirection(pRenderer, PinDirection.Input, 0);

                // Add the sample grabber to the graph
                hr = m_FilterGraph.AddFilter( baseGrabFlt, "Ds.NET Grabber" );
                DsError.ThrowExceptionForHR( hr );

                if (m_VidControl == null)
                {
                    // Connect the Still pin to the sample grabber
                    hr = m_FilterGraph.Connect(m_pinStill, pSampleIn);
                    DsError.ThrowExceptionForHR( hr );

                    // Connect the capture pin to the renderer
                    hr = m_FilterGraph.Connect(pCaptureOut, pRenderIn);
                    DsError.ThrowExceptionForHR( hr );
                }
                else
                {
                    // Connect the capture pin to the renderer
                    hr = m_FilterGraph.Connect(pCaptureOut, pRenderIn);
                    DsError.ThrowExceptionForHR( hr );

                    // Connect the Still pin to the sample grabber
                    hr = m_FilterGraph.Connect(m_pinStill, pSampleIn);
                    DsError.ThrowExceptionForHR( hr );
                }

                // Learn the video properties
                SaveSizeInfo(sampGrabber);
                ConfigVideoWindow(hControl);

                // Start the graph
                IMediaControl mediaCtrl = m_FilterGraph as IMediaControl;
                hr = mediaCtrl.Run();
                DsError.ThrowExceptionForHR( hr );
            }
            finally
            {
                if (sampGrabber != null)
                {
                    Marshal.ReleaseComObject(sampGrabber);
                    sampGrabber = null;
                }
                if (pCaptureOut != null)
                {
                    Marshal.ReleaseComObject(pCaptureOut);
                    pCaptureOut = null;
                }
                if (pRenderIn != null)
                {
                    Marshal.ReleaseComObject(pRenderIn);
                    pRenderIn = null;
                }
                if (pSampleIn != null)
                {
                    Marshal.ReleaseComObject(pSampleIn);
                    pSampleIn = null;
                }
            }
        }

        private void SaveSizeInfo(ISampleGrabber sampGrabber)
        {
            int hr;

            // Get the media type from the SampleGrabber
            AMMediaType media = new AMMediaType();

            hr = sampGrabber.GetConnectedMediaType( media );
            DsError.ThrowExceptionForHR( hr );

            if( (media.formatType != FormatType.VideoInfo) || (media.formatPtr == IntPtr.Zero) )
            {
                throw new NotSupportedException( "Unknown Grabber Media Format" );
            }

            // Grab the size info
            VideoInfoHeader videoInfoHeader = (VideoInfoHeader) Marshal.PtrToStructure( media.formatPtr, typeof(VideoInfoHeader) );
            m_videoWidth = videoInfoHeader.BmiHeader.Width;
            m_videoHeight = videoInfoHeader.BmiHeader.Height;
            m_stride = m_videoWidth * (videoInfoHeader.BmiHeader.BitCount / 8);

            DsUtils.FreeAMMediaType(media);
            media = null;
        }

        // Set the video window within the control specified by hControl
        private void ConfigVideoWindow(Control hControl)
        {
            int hr;

            IVideoWindow ivw = m_FilterGraph as IVideoWindow;

            // Set the parent
            hr = ivw.put_Owner(hControl.Handle);
            DsError.ThrowExceptionForHR( hr );

            // Turn off captions, etc
            hr = ivw.put_WindowStyle(WindowStyle.Child | WindowStyle.ClipChildren | WindowStyle.ClipSiblings);
            DsError.ThrowExceptionForHR( hr );

            // Yes, make it visible
            hr = ivw.put_Visible( OABool.True );
            DsError.ThrowExceptionForHR( hr );

            // Move to upper left corner
            Rectangle rc = hControl.ClientRectangle;
            hr = ivw.SetWindowPosition( 0, 0, rc.Right, rc.Bottom );
            DsError.ThrowExceptionForHR( hr );
        }

        private void ConfigureSampleGrabber(ISampleGrabber sampGrabber)
        {
            int hr;
            AMMediaType media = new AMMediaType();

            // Set the media type to Video/RBG24
            media.majorType = MediaType.Video;
            media.subType = MediaSubType.RGB24;
            media.formatType = FormatType.VideoInfo;
            hr = sampGrabber.SetMediaType( media );
            DsError.ThrowExceptionForHR( hr );

            DsUtils.FreeAMMediaType(media);
            media = null;

            // Configure the samplegrabber
            hr = sampGrabber.SetCallback( this, 1 );
            DsError.ThrowExceptionForHR( hr );
        }

        // Set the Framerate, and video size
        private void SetConfigParms(IPin pStill, int iWidth, int iHeight, short iBPP)
        {
            int hr;
            AMMediaType media;
            VideoInfoHeader v;

            IAMStreamConfig videoStreamConfig = pStill as IAMStreamConfig;

            // Get the existing format block
            hr = videoStreamConfig.GetFormat(out media);
            DsError.ThrowExceptionForHR(hr);

            try
            {
                // copy out the videoinfoheader
                v = new VideoInfoHeader();
                Marshal.PtrToStructure( media.formatPtr, v );

                // if overriding the width, set the width
                if (iWidth > 0)
                {
                    v.BmiHeader.Width = iWidth;
                }

                // if overriding the Height, set the Height
                if (iHeight > 0)
                {
                    v.BmiHeader.Height = iHeight;
                }

                // if overriding the bits per pixel
                if (iBPP > 0)
                {
                    v.BmiHeader.BitCount = iBPP;
                }

                // Copy the media structure back
                Marshal.StructureToPtr( v, media.formatPtr, false );

                // Set the new format
                hr = videoStreamConfig.SetFormat( media );
                DsError.ThrowExceptionForHR( hr );
            }
            finally
            {
                DsUtils.FreeAMMediaType(media);
                media = null;
            }
        }

        /// <summary> Shut down capture </summary>
        private void CloseInterfaces()
        {
            int hr;

            try
            {
                if( m_FilterGraph != null )
                {
                    IMediaControl mediaCtrl = m_FilterGraph as IMediaControl;

                    // Stop the graph
                    hr = mediaCtrl.Stop();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            ReleaseObject(m_CameraControl);
            ReleaseObject(m_FilterGraph);
            ReleaseObject(m_VidControl);
            ReleaseObject(m_pinStill);
            ReleaseObject(m_VideoProcess);
        }

        private void ReleaseObject(object Variable)
        {
            if (Variable != null)
            {
                Marshal.ReleaseComObject(Variable);
                Variable = null;
            }
        }

        /// <summary> sample callback, NOT USED. </summary>
        int ISampleGrabberCB.SampleCB( double SampleTime, IMediaSample pSample )
        {
            Marshal.ReleaseComObject(pSample);
            return 0;
        }

        /// <summary> buffer callback, COULD BE FROM FOREIGN THREAD. </summary>
        int ISampleGrabberCB.BufferCB( double SampleTime, IntPtr pBuffer, int BufferLen )
        {
            // Note that we depend on only being called once per call to Click.  Otherwise
            // a second call can overwrite the previous image.
            Debug.Assert(BufferLen == Math.Abs(m_stride) * m_videoHeight, "Incorrect buffer length");

            if (m_WantOne)
            {
                m_WantOne = false;
                Debug.Assert(m_ipBuffer != IntPtr.Zero, "Unitialized buffer");

                // Save the buffer
                CopyMemory(m_ipBuffer, pBuffer, BufferLen);

                // Picture is ready.
                m_PictureReady.Set();
            }

            return 0;
        }
    }
}
