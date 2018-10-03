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

        public static event EventHandler<TeachResult> IterationChange;

        static DocumentAngelNet()
        {
            Create(200, 200);
            PrepareTeachBatchFile();
        }
        
        public static void Create(int width, int height)
        {
            imageSize = (width, height);

            Net = new Network();

            Net.InitLayers(width, height,
                new ConvolutionLayer(ActivationType.ReLu, 5, 3), //596 - 24
                new MaxPoolingLayer(2), // 298 - 12
                new ConvolutionLayer(ActivationType.ReLu, 20, 3), //296 - 10
                new MaxPoolingLayer(2), // 148 - 5
                new FullyConnectedLayer(100, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(100, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(100, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(100, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(4, ActivationType.BipolarSigmoid));

            Net.Randomize();

            teacher = new BackPropagationLearning(Net);
        }
        
        public static Image Compute(Image image)
        {
            var scaledImage = image
                .CutWhiteBorders(out _)
                .ToBlackWite()
                .ScaleImage(imageSize.width, 100000);

            var map = scaledImage.GetDoubleMatrix();
            var width = map.GetLength(1);
            var height = map.GetLength(0);
            var partHeight = imageSize.height;
            var hPosition = 0;
            var step = scanStep;

            var result = new List<(int position, double value)>();

            while(hPosition + partHeight < height)
            {
                var mapPart = map.GetMapPart(0, hPosition, width, partHeight);
                var value = Net.Compute(mapPart);

                result.Add((hPosition, value[0]));
                hPosition += step;
            }

            var cords = result.Where(x => x.value > 0.5d).Select(x => new Cords { Top = x.position, Bottom = x.position + partHeight }).ToList();

            return scaledImage.DrawCords(cords);
        }

        public static async Task TeachRun()
        {
            Running = true;
            var iteration = 0;
            var error = 0d;
            var computed = 0d;
            var success = 0;
            var globalSuccess = 0;
            var totalSuccess = 0;
            var trueAnswers = 0;
            var falseAnswers = 0;
            var rnd = new Random((int)DateTime.Now.Ticks);
            var batchLength = teachBatch.Count();
            
            await Task.Run(() =>
            {
                while (Running)
                {
                    double[,] input;
                    double[] target = new double[4];
                    var rndFileIndex = 0;

                    rndFileIndex = rnd.Next(batchLength);

                    var data = teachBatch[rndFileIndex];
                    input = data.map;
                    computed = Net.Compute(input)[0];

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

                    success = computed < 0.5d ? success + 1 : 0;

                    totalSuccess = Math.Max(0, success > totalSuccess ? success : totalSuccess - 1);
                    error += teacher.Run(input, target);
                    iteration++;

                    if (iteration % 2 == 0)
                        IterationChange?.Invoke(new object(), new TeachResult
                        {
                            Iteration = iteration, 
                            Error = error / (double)iteration, 
                            Successes = totalSuccess,
                            SuccessPercent = globalSuccess / ((double)iteration / 3),
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
                var match = Regex.Match(path, @"(?<angel>\d+).jpg$");
                
                if (!match.Success) continue;
                
                var image = (Bitmap) GetImage(path);
                var scaledImage = image
                    .CutWhiteBorders(out _)
                    .ToBlackWite()
                    .ScaleImage(imageSize.width, 100000);
                var imageData = ((Bitmap)scaledImage).Clone(new Rectangle(0, 0, imageSize.width, imageSize.height), scaledImage.PixelFormat);
                var map = imageData.GetDoubleMatrix();
                
                teachBatch.Add((map, Convert.ToInt32(match.Groups["angel"].Value)));
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
            switch (target)
            {
                case 0:
                    return computed.ToList().IndexOf(computed.Max()) == 
            }
        }
    }
}
