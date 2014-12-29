using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterExample
{
    class Filter
    {
        static float[,] ImFilter(byte[, ,] Image, float[,] Kernel)
        { 
            if (Kernel.GetLength(0) != Kernel.GetLength(1) || Kernel.GetLength(0) % 2 != 1)
                throw new Exception("Kernel must be N by N array, where N is an odd number");

            var Imfilt = new float[Image.GetLength(0), Image.GetLength(1)];
            var ImGrayscale = new float[Image.GetLength(0), Image.GetLength(1)];
            int Offset = (Kernel.GetLength(0) - 1) / 2;
            for (int i = 0; i < ImGrayscale.GetLength(0); i++)
            {
                for (int j = 0; j < ImGrayscale.GetLength(1); j++)
                {
                    ImGrayscale[i, j] = 0;
                    for (int k = 0; k < Image.GetLength(2); k++)
                    {
                        ImGrayscale[i, j] = ImGrayscale[i, j] + (float)Image[i, j, k];
                    }
                    ImGrayscale[i, j] = Math.Abs(ImGrayscale[i, j] / (float)Image.Length);
                }
            }
            
            for (int i = Offset; i < Image.GetLength(0) - Offset; i++)
            {
                for (int j = Offset; j < Image.GetLength(1) - Offset; j++)
                {
                    for (int idx = 0; idx < Kernel.GetLength(0); idx++)
                    {
                        for (int idy = 0; idy < Kernel.GetLength(1); idy++)
                        {
                            Imfilt[i, j] = Imfilt[i, j] + Kernel[idx, idx] * ImGrayscale[i - Offset + idx, j - Offset + idy]; 
                        }
                    }
                    Imfilt[i, j] = Imfilt[i, j] / Kernel.Length;
                }
            }
            return Imfilt;
        }
    }
}
