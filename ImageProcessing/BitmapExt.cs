using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;

namespace ImageProcessing
{
    public static class BitmapExt
    {
        public unsafe static double[,] GetDoubleMatrix(this Image bitmap, double delimetr = 255f, bool invert = true)
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

        public static Image ScaleImage(this Image image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }

        public static double[,] GetMapPart(this double[,] map, int x, int y, int width, int height)
        {
            x = Math.Max(0, Math.Min(map.GetLength(1), x));
            y = Math.Max(0, Math.Min(map.GetLength(0), y));

            height = Math.Max(0, Math.Min(y + height, map.GetLength(0)) - y);
            width = Math.Max(0, Math.Min(x + width, map.GetLength(1)) - x);

            var result = new double[height, width];

            Parallel.For(0, height, (int Y) =>
            {
                Parallel.For(0, width, (int X) =>
                {
                    try
                    {
                        result[Y, X] = map[Y + y, X + x];
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentOutOfRangeException($"{map.GetLength(0)}/{map.GetLength(1)} - {Y + y}/{X + x}", ex);
                    }
                });
            });

            return result;
        }

        public static int GetStep(this Image bitmap)
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

        public static Image DrawCords(this Image bitmap, List<Cords> cords, Color? color = null)
        {
            var newBitmap = (Bitmap)bitmap.Clone();
            var bitmapData = newBitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            var width = bitmap.Width;
            var rnd = new Random();
            var step = bitmap.GetStep();

            unsafe
            {
                foreach (var cord in cords)
                {
                    Color randomColor = color ?? Color.FromArgb(rnd.Next(200), rnd.Next(200), rnd.Next(200));

                    for (var y = cord.Top; y <= cord.Bottom; y++)
                    {
                        var pRow = (byte*)bitmapData.Scan0 + y * bitmapData.Stride;
                        var offset = 0;

                        for (var x = 0; x <= width; x++)
                        {
                            if (x == 0 || x == width - 1 || y == cord.Top || y == cord.Bottom)
                            {
                                pRow[offset + 2] = randomColor.R;
                                pRow[offset + 1] = randomColor.G;
                                pRow[offset] = randomColor.B;
                            }

                            offset += step;
                        }
                    }
                }
            }

            newBitmap.UnlockBits(bitmapData);

            return newBitmap;
        }

        public static float GetAverBright(this Image bitmap)
        {
            var filterdBitmap = (Bitmap)bitmap.Clone();
            var height = filterdBitmap.Height;
            var width = filterdBitmap.Width;
            var bitmapData = filterdBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, filterdBitmap.PixelFormat);

            var docBright = new float[height * width];
            var step = bitmap.GetStep();
            var maxBright = 0f;
            var minBright = 255f;
            var sumBright = 0f;

            unsafe
            {
                for (var y = 0; y < height; y++)
                {
                    var row = (byte*)bitmapData.Scan0 + (y * bitmapData.Stride);
                    var columnOffset = 0;

                    for (var x = 0; x < width; x++)
                    {
                        var bright = GetPixelBright(row, step, columnOffset);

                        docBright[y * width + x] = bright;

                        sumBright += bright;
                        maxBright = bright > maxBright ? bright : maxBright;
                        minBright = bright < minBright ? bright : minBright;

                        columnOffset += step;
                    }
                }
            }

            filterdBitmap.UnlockBits(bitmapData);

            var average = docBright.Sum() / docBright.Length;
            var d = (255 - average) / 2;

            var minAvrList = docBright.Where(x => x <= average - d).ToList();
            var maxAvrList = docBright.Where(x => x >= average + d).ToList();

            var minAvr = (minAvrList.Sum() / minAvrList.Count);
            var maxAvr = (maxAvrList.Sum() / maxAvrList.Count);

            return ((maxBright + minBright + minAvr + maxAvr) / 4f);
        }

        public static Bitmap ToBlackWite(this Bitmap bitmap, double averege = 0)
        {
            averege = averege == 0 ? bitmap.GetAverBright() : averege;

            var newBitmap = new Bitmap(bitmap.Width, bitmap.Height, bitmap.PixelFormat);
            var height = newBitmap.Height;
            var width = newBitmap.Width;
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            var newBitmapData = newBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, newBitmap.PixelFormat);
            var step = newBitmap.GetStep();

            unsafe
            {

                for (var y = 0; y < height; y++)
                {
                    byte c = 255;

                    var row = (byte*)bitmapData.Scan0 + (y * bitmapData.Stride);
                    var newRow = (byte*)newBitmapData.Scan0 + (y * newBitmapData.Stride);

                    int columnOffset = 0;

                    for (int x = 0; x < width; ++x)
                    {
                        var rowPix = GetPixelBright(row, step, columnOffset);

                        c = rowPix < averege ? (byte)0 : (byte)255;

                        SetPixelBright(newRow, step, columnOffset, c);

                        columnOffset += step;
                    }
                }
            }

            newBitmap.UnlockBits(newBitmapData);

            return newBitmap;
        }

        private unsafe static byte GetPixelBright(byte* row, int step, int offset)
        {
            var i = 0;
            var bright = 0;

            for (i = 0; i < step || i < 3; i++)
            {
                bright += row[offset + i];
            }

            bright = bright / i;

            return (byte)Math.Max(0, Math.Min(255, bright));
        }

        private unsafe static void SetPixelBright(byte* row, int step, int offset, byte bright)
        {
            for (var i = 0; i < step || i < 3; i++)
            {
                row[offset + i] = bright;
            }

            if (step > 3)
            {
                row[offset + 3] = byte.MaxValue;
            }
        }
    }
}
