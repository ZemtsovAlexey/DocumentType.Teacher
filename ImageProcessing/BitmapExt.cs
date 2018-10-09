using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageProcessing
{
    public static class BitmapExt
    {
        public static unsafe double[,] GetDoubleMatrix(this Image bitmap, double delimetr = 255f, bool invert = true)
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

        public static Image ScaleImage(this Image image, int maxWidth, int maxHeight, bool equalSize = false)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);
            
            var x = !equalSize || newWidth > newHeight ? 0 : (newHeight - newWidth) / 2;
            var y = !equalSize || newHeight > newWidth ? 0 : (newWidth - newHeight) / 2;
            var newImage = new Bitmap(newWidth, newHeight);
            
            if (equalSize)
            {
                var size = Math.Max(newWidth, newHeight);

                newImage = new Bitmap(size, size);
            }
            
            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.DrawImage(image, x, y, newWidth, newHeight);
            }

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
        
        public static int GetStep(this BitmapData bitmap)
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

        public static Image ToBlackWite(this Image image, double averege = 0)
        {
            var bitmap = (Bitmap) image;
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

            bitmap.UnlockBits(bitmapData);
            newBitmap.UnlockBits(newBitmapData);

            return newBitmap;
        }

        public static unsafe Image ToBitmap(this double[,] data)
        {
            var height = data.GetLength(0);
            var width = data.GetLength(1);
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var step = bitmap.GetStep();
            var bData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            var minBright = 0d;
            var maxBright = 0d;

            for (var y = 0; y < data.GetLength(0); y++)
            {
                var row = (byte*)bData.Scan0 + y * bData.Stride;
                var offset = 0;

                for (var x = 0; x < data.GetLength(1); x++)
                {
                    maxBright = data[y, x] > maxBright ? data[y, x] : maxBright;
                    offset += step;
                }
            }

            for (var y = 0; y < data.GetLength(0); y++)
            {
                var row = (byte*)bData.Scan0 + y * bData.Stride;
                var offset = 0;

                for (var x = 0; x < data.GetLength(1); x++)
                {
                    var bright = (byte)Math.Max(0, Math.Min(255, data[y, x] / maxBright * 255));
                    SetPixelBright(row, step, offset, bright);
                    offset += step;
                }
            }

            bitmap.UnlockBits(bData);

            return bitmap;
        }

        public static byte[] ToByteArray(this Image image)
        {
            using (var stream = new MemoryStream())
            {
                image.Save(stream, ImageFormat.Jpeg);

                return stream.ToArray();
            }
        }

        public static Stream ToStream(this Image image)
        {
            var stream = new MemoryStream();
            image.Save(stream, ImageFormat.Jpeg);
            stream.Position = 0;

            return stream;
        }

        public static Image RotateImage(this Image image, float angle)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            var offset = new PointF((float)image.Width / 2, (float)image.Height / 2);
            var rotatedBmp = new Bitmap(image.Width, image.Height);

            rotatedBmp.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            var g = Graphics.FromImage(rotatedBmp);

            g.TranslateTransform(offset.X, offset.Y);
            g.RotateTransform(angle);
            g.TranslateTransform(-offset.X, -offset.Y);
            g.DrawImage(image, new PointF(0, 0));

            return rotatedBmp;
        }
        
        public static double FindAngel(this Image image)
        {
            return image.GetDoubleMatrix().FindAngel();
        }

        public static double FindAngel(this double[,] imageMap)
        {
            var imageWidth = imageMap.GetLength(1);
            var imageHeight = imageMap.GetLength(0);
            var D = (int)(Math.Sqrt(imageWidth * imageWidth + imageHeight * imageHeight));
            var hughWidth = 181;
            var hughHeight = ((int)(1.414213562 * D) * 2) + 1;
            var hughSpace = new double[hughHeight, hughWidth];
            var xpoint = 0;
            var maxT = 0d;
            var table = CreateTable();
            
            for (var xi = 0; xi < imageWidth; xi++)
            {
                for (var yi = 0; yi < imageHeight; yi++)
                {
                    if (Math.Abs(imageMap[yi, xi]) == 0) continue;
                    
                    for (var i = 0; i < 181; i++)
                    {
                        var rho = (int)((xi * table[0, i] + yi * table[1, i])) + (hughHeight / 2);
                        var g = hughSpace[rho, i] + 1;
                        
                        if (g > maxT)
                        {
                            maxT = g;
                            xpoint = i;
                        }
                        
                        hughSpace[rho, i] = g;
                    }
                }
            }
            
            var thetaHotPoint = ((Math.PI / 180) * -90d) + (Math.PI / 180) * xpoint;
            return (90 - Math.Abs(thetaHotPoint) * (180 / Math.PI)) * (thetaHotPoint< 0 ? -1 : 1);
        }
        
        public static Image CutWhiteBorders(this Image image, out Cords cords)
        {
            var procBitmap = image.ToBlackWite();
            var map = procBitmap.GetDoubleMatrix();

            var top = image.Height;
            var left = image.Width;
            var bottom = 0;
            var right = 0;
            
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var point = map[y, x];

                    if (point > 0.9d)
                    {
                        top = Math.Min(top, y);
                        bottom = Math.Max(bottom, y);
                        left = Math.Min(left, x);
                        right = Math.Max(right, x);
                    }
                }
            }
            
            if (right < left || bottom < top)
            {
                cords = new Cords(0, image.Height, 0, image.Width);
                
                return image;
            }
            
            cords = new Cords(top, bottom, left, right);
            
            var bitmap = (Bitmap) image;
            var resultBitmap = bitmap.Clone(new Rectangle(left, top, right - left, bottom - top), image.PixelFormat);

            return resultBitmap;
        }
        
        private static double[,] CreateTable()
        {
            const double rad = (Math.PI / 180);

            var table = new double[2, 181]; // 0 - cos, 1 - sin;
            var theta = rad * -90;
            
            for (var i = 0; i < 181; i++)
            {
                table[0, i] = Math.Cos(theta);
                table[1, i] = Math.Sin(theta);
                theta += rad;
            }
            
            return table;
        }
        
        private static unsafe byte GetPixelBright(byte* row, int step, int offset)
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

        private static unsafe void SetPixelBright(byte* row, int step, int offset, byte bright)
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
