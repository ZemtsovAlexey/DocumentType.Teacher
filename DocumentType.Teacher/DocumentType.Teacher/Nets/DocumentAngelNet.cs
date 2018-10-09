using System;
using ImageProcessing;
using Neural.Net.CPU.Domain.Save;
using Neural.Net.CPU.Layers;
using Neural.Net.CPU.Learning;
using Neural.Net.CPU.Networks;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentType.Teacher.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Neural.Net.CPU.Domain.Layers;
using Neural.Net.CPU.Models;

namespace DocumentType.Teacher.Nets
{
    public static class DocumentAngelNet
    {
        public static Network Net;

        public static double LearningRate
        {
            get => teacher.LearningRate;
            set => teacher.LearningRate = value;
        }

        public static bool Running { get; set; }

        private static BackPropagationLearning teacher;
        public static List<(double[,] map, int angel)> teachBatch;
        private static (int width, int height) imageSize;
        private static int scaledWidth = 244;

        public static event EventHandler<TeachResult> IterationChange;

        static DocumentAngelNet()
        {
            Create(scaledWidth, scaledWidth);
//            PrepareTeachBatchFile();
        }
        
        public static void Create(int width, int height)
        {
            imageSize = (width, height);

            Net = new Network();

            Net.InitLayers(width, height,
                new ConvolutionLayer(ActivationType.ReLu, 2, 3), //242
                new ConvolutionLayer(ActivationType.ReLu, 4, 3), //240
                new ConvolutionLayer(ActivationType.ReLu, 8, 3), //238
                new ConvolutionLayer(ActivationType.ReLu, 12, 3), //236
                new MaxPoolingLayer(2), // 118
                new ConvolutionLayer(ActivationType.ReLu, 16, 3), //116
                new MaxPoolingLayer(2), // 58
                new ConvolutionLayer(ActivationType.ReLu, 20, 3), //56
                new MaxPoolingLayer(2), // 28
                new ConvolutionLayer(ActivationType.ReLu, 24, 3), //26
                new MaxPoolingLayer(2), // 13
//                new ConvolutionLayer(ActivationType.ReLu, 16, 4), //20
//                new MaxPoolingLayer(2), // 10
                new FullyConnectedLayer(80, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(40, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(40, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(40, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(40, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(4, ActivationType.BipolarSigmoid));

            Net.Randomize();

            teacher = new BackPropagationLearning(Net);
            teacher.LearningRate = 0.02f;
        }
        
        public static Image Compute(Image image)
        {
            var scaledImage = image
                .ToBlackWite()
                .ScaleImage(scaledWidth, 100000, true);

            var map = scaledImage.GetDoubleMatrix();
            var mapPart = map.GetMapPart(map.GetLength(1) / 2 - imageSize.width / 2, map.GetLength(0) / 2 - imageSize.height / 2, imageSize.width, imageSize.height);
            var computed = Net.Compute(mapPart);
            var result = scaledImage.RotateFlip(GetAngel(computed));

            return result;
        }

        public static async Task TeachRun()
        {
            Running = true;
            var iteration = 0;
            var error = 0d;
            var computed = new double[4];
            var success = 0;
            var globalSuccess = 0;
            var totalSuccess = 0;
            var rnd = new Random((int)DateTime.Now.Ticks);
            var batchLength = teachBatch.Count();
            
            await Task.Run(() =>
            {
                while (Running)
                {
                    double[,] input;
                    double[] target = new double[4];
                    var rndFileIndex = 0;
                    var prevSuccess = success;

                    rndFileIndex = rnd.Next(batchLength);

                    var data = teachBatch[rndFileIndex];
                    input = data.map;
                    computed = Net.Compute(input);

                    switch (data.angel)
                    {
                        case 0:
                            target = new[] {1d, -1d, -1d, -1d};
                            break;

                        case 90:
                            target = new[] {-1d, 1d, -1d, -1d};
                            break;

                        case 180:
                            target = new[] {-1d, -1d, 1d, -1d};
                            break;

                        case 270:
                            target = new[] {-1d, -1d, -1d, 1d};
                            break;
                    }

                    var result = Success(computed, data.angel);
                    success = result ? success + 1 : 0;
                    globalSuccess = result ? globalSuccess + 1 : globalSuccess;

                    totalSuccess = Math.Max(0, success > totalSuccess ? success : totalSuccess - 1);
                    error += teacher.Run(input, target);
//                    error += prevSuccess > success ? teacher.Run(input, target) : 0;
                    iteration++;

                    if (iteration % 2 == 0)
                        IterationChange?.Invoke(new object(), new TeachResult
                        {
                            Iteration = iteration, 
                            Error = error / (double)iteration, 
                            Successes = totalSuccess,
                            SuccessPercent = globalSuccess / ((double)iteration),
                            ImageIndex = rndFileIndex,
                            Target = (int)target[0]
                        });
                }
            }).ConfigureAwait(false);
        }

        public static void TeachStop()
        {
            Running = false;
        }

        public static void PrepareTeachBatchFile()
        {
            teachBatch = new List<(double[,] map, int angel)>();
            var imagesPaths = Directory.EnumerateFiles(@"TeachData/angel", "*.jpg").ToArray();

            foreach (var path in imagesPaths)
            {
                var image = GetImage(path)
                    .ToBlackWite();

                for (var angel = 0; angel < 360; angel += 90)
                {
                    var flipedImage = image.RotateFlip(angel);
                    flipedImage = flipedImage.ScaleImage(scaledWidth, 100000, true);

                    var teachAngel = Math.Abs(angel - 360) == 360 ? 0 : Math.Abs(angel - 360);
                    var map = flipedImage.GetDoubleMatrix();
                    var mapPart = map.GetMapPart(map.GetLength(1) / 2 - imageSize.width / 2, map.GetLength(0) / 2 - imageSize.height / 2, imageSize.width, imageSize.height);
                
                    teachBatch.Add((mapPart, teachAngel));
                }
            }
        }

        public static byte[] Save()
        {
            return Net.Save();
        }
        
        public static void Load(byte[] data)
        {
            Net.Load(data);
            teacher = new BackPropagationLearning(Net);
        }

        private static Image GetImage(string path)
        {
            using (var file = Image.FromFile(path))
            {
                return new Bitmap(file);
            }
        }

        private static bool Success(double[] computed, int target)
        {
            var result = computed.ToList().IndexOf(computed.Max());

            switch (target)
            {
                case 0:
                    return result == 0;

                case 90:
                    return result == 1;

                case 180:
                    return result == 2;

                case 270:
                    return result == 3;

                default:
                    return false;
            }
        }

        private static int GetAngel(double[] computed)
        {
            var result = computed.ToList().IndexOf(computed.Max());

            switch (result)
            {
                case 0:
                    return 0;

                case 1:
                    return 90;

                case 2:
                    return 180;

                case 3:
                    return 270;

                default:
                    return 0;
            }
        }

        private static Image RotateFlip(this Image image, int angel)
        {
            var flipedImage = (Image)image.Clone();

            switch (angel)
            {
                case 90:
                    flipedImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    break;
                
                case 180:
                    flipedImage.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    break;
                
                case 270:
                    flipedImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    break;
            }

            return flipedImage;
        }
    }
}
