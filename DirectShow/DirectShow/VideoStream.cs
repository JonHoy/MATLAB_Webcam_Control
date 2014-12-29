using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;

namespace DirectShow
{
    public delegate byte[] SubRoutine(byte[] Data);

    public class VideoStream
    {
        private Capture VideoSource;
        private VideoStream ParentStream;
        public int Rows { get; private set; }
        public int Cols { get; private set; }
        public int Planes { get; private set; }
        public byte[] Buffer { get; private set; }
        private Task BufferUpdater;
        private SubRoutine Algorithm;


        public VideoStream(
            ref Capture VideoSource,
            SubRoutine Algorithm // process this routine in the background for the lifetime of the class
            ) 
        {
            this.VideoSource = VideoSource;
            this.Algorithm = Algorithm;
            this.Rows = VideoSource.Height;
            this.Cols = VideoSource.Width;
            this.Planes = 3;
            BackgroundWorker();
        }

        public VideoStream(
            ref VideoStream ParentStream,
            SubRoutine Algorithm // process this routine in the background for the lifetime of the class
            )
        {
            this.ParentStream = ParentStream;
            this.Rows = ParentStream.Rows;
            this.Cols = ParentStream.Cols;
            this.Planes = ParentStream.Planes;
            this.Algorithm = Algorithm;
            BackgroundWorker();
        }

        private void BackgroundWorker()
        {
            int numel = Rows * Cols * Planes;
            Buffer = new Byte[numel];
            try
            {
                BufferUpdater = Task.Run(() =>
                {
                    //Thread.
                    while (true)
                    {
                        if (VideoSource != null)
                            Buffer = Algorithm.Invoke(VideoSource.Buffer);
                        else if (ParentStream != null)
                            Buffer = Algorithm.Invoke(ParentStream.Buffer);
                        else
                            throw new Exception("Buffer cannot be updated!");
                    }
                });
            }
            catch (Exception)
            {
                
                throw;
            }
        }

        public byte[, ,] GrabData() {
            var Data = new Byte[Rows, Cols, Planes];
            
            return Data;
        }

        public Bitmap GetPhoto()
        {
            Bitmap Photo;
            int Stride = 4 * ((Cols * 3 + 3) / 4);
            unsafe
            {
                fixed (byte* srcPtr = &Buffer[0])
                    Photo = new Bitmap(Cols, Rows, Stride, PixelFormat.Format24bppRgb, (IntPtr)srcPtr);
            }
            return (Bitmap)Photo.Clone();
        }
    }
}
