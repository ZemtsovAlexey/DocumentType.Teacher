using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace ImageProcessing
{
    public static class BitmapExt
    {
        public unsafe static double[,] GetDoubleMatrix(this Bitmap bitmap, double delimetr = 255f, bool invert = true)
        {
            var result = new double[bitmap.Height, bitmap.Width];
            var procesBitmap = (Bitmap)bitmap.Clone();
            var bitmapData = procesBitmap.LockBits(new Rectangle(0, 0, procesBitmap.Width, procesBitmap.Height), ImageLockMode.ReadWrite, procesBitmap.PixelFormat);
            var step = procesBitmap.GetStep();

            int imageHeight = procesBitmap.Height;
            int imageWidth = procesBitmap.Width;

            Parallel.For(0, imageHeight, (int y) =>
            {
                var pRow = (byte*)bitmapData.Scan0 + y * bitmapData.Stride;

                Parallel.For(0, imageWidth, (int x) =>
                {
                    var offset = x * step;
                    result[y, x] = step == 1
                        ? (invert ? 1 - (pRow[offset] / delimetr) : (pRow[offset] / delimetr))
                        : (invert ? 1 - ((pRow[offset + 2] + pRow[offset + 1] + pRow[offset]) / 3 / delimetr) : ((pRow[offset + 2] + pRow[offset + 1] + pRow[offset]) / 3 / delimetr));
                });
            });

            procesBitmap.UnlockBits(bitmapData);

            return result;
        }

        public static int GetStep(this Bitmap bitmap)
        {
            switch (bitmap.PixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                    return 3;

                case PixelFormat.Format8bppIndexed:
                    return 1;

                default:
                    return 4;
            }
        }
    }
}
