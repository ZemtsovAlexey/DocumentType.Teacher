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

namespace DocumentType.Teacher.Nets
{
    public static class NeuralNetwork
    {
        public static Network Net;
        public static bool Running { get; set; }

        private static BackPropagationLearning teacher;
        private static List<(double[,] map, double target)> teachBatch;
        private static (int width, int height) imageSize;

        public static event EventHandler<TeachResult> IterationChange;

        static NeuralNetwork()
        {
            Create(602, 26);
            PrepareTeachBatchFile("");
        }
        
        public static void Create(int width, int height)
        {
            imageSize = (width, height);

            Net = new Network();

            Net.InitLayers(602, 26,
                new ConvolutionLayer(ActivationType.ReLu, 8, 3), //600 - 24
                new MaxPoolingLayer(2), //300 - 12 
                new ConvolutionLayer(ActivationType.ReLu, 16, 3), // 150 - 10
                new MaxPoolingLayer(2), // 75 - 5
                new FullyConnectedLayer(150, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(100, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(1, ActivationType.BipolarSigmoid));

            Net.Randomize();

            teacher = new BackPropagationLearning(Net);
        }

        public static Image Compute(Image image)
        {
            var scaledImage = ((Bitmap)image)
                .ToBlackWite()
                .ScaleImage(602, 100000);
            var map = scaledImage.GetDoubleMatrix();
            var width = map.GetLength(1);
            var heigth = map.GetLength(0);
            var partHeight = 26;
            var hPosition = 0;
            var step = 3;

            var result = new List<(int position, double value)>();

            while(hPosition + partHeight < heigth)
            {
                var mapPart = map.GetMapPart(0, hPosition, width, partHeight);
                var value = Net.Compute(mapPart);

                result.Add((hPosition, value[0]));
                hPosition += step;
            }

            var cords = result.Where(x => x.value > 0).Select(x => new Cords { Top = x.position, Bottom = x.position + 26 }).ToList();

            return scaledImage.DrawCords(cords);
        }

        public static async Task TeachRun()
        {
            Running = true;
            var iteration = 0;
            var error = 0d;
            var computed = 0d;
            var succes = 0;
            var totalSuccess = 0;
            var trueAnswers = 0;
            var falseAnswers = 0;
            var rnd = new Random();
            var batchTrue = teachBatch.Where(x => x.target > 0).ToList();
            var batchFalse = teachBatch.Where(x => x.target <= 0).ToList();
            var batchTrueLength = batchTrue.Count();
            var batchFalseLength = batchFalse.Count();

            await Task.Run(() =>
            {
                while (Running)
                {
                    double[,] input;
                    double[] target;

                    if (falseAnswers < 3)
                    {
                        falseAnswers++;
                        trueAnswers = 0;

                        var rndFileIndex = rnd.Next(batchFalseLength);

                        input = batchFalse[rndFileIndex].map;
                        computed = Net.Compute(input)[0];
                        target = new[] { -1d };

                        succes = computed <= 0 ? succes + 1 : 0;
                    }
                    else
                    {
                        falseAnswers = 0;
                        trueAnswers++;

                        var rndFileIndex = rnd.Next(batchTrueLength);

                        input = batchTrue[rndFileIndex].map;
                        computed = Net.Compute(input)[0];
                        target = new[] { 1d };

                        succes = computed > 0 ? succes + 1 : 0;
                    }

                    totalSuccess = Math.Max(0, succes > totalSuccess ? succes : totalSuccess - 1);
                    //error += (target[0] < 0 && computed >= 0) || (target[0] > 0 && computed <= 0) ? teacher.Run(input, target) : 0;
                    error += teacher.Run(input, target);
                    iteration++;
                    IterationChange?.Invoke(new object(), new TeachResult { Iteration = iteration, Error = error / (double)iteration, Successes = totalSuccess });
                }
            }).ConfigureAwait(false);
        }

        public static void TeachStop()
        {
            Running = false;
        }

        public static void PrepareTeachBatchFile(string imagesPath)
        {
            teachBatch = new List<(double[,] map, double target)>();
            var imagesPaths = Directory.EnumerateFiles(@"D:\documents types\teach\invoices", "*.jpg").ToArray();

            foreach (var path in imagesPaths)
            {
                var match = Regex.Match(path, @"(?<from>\d+)-(?<to>\d+).jpg$");

                if (!match.Success)
                    continue;

                var image = (Bitmap)GetImage(path);
                var ratio = (double)image.Width / image.Width;
                var from = 0d;
                var to = 0d;
                var partHeight = 26;
                var hPosition = 0;
                var step = 3;

                if (match.Success)
                {
                    from = Convert.ToDouble(match.Groups["from"].Value) * ratio;
                    to = Convert.ToDouble(match.Groups["to"].Value) * ratio;
                    to = to - from < partHeight ? from + partHeight + step : to;
                }

                var scaledImage = image
                    .ToBlackWite()
                    .ScaleImage(image.Width, 100000);
                var map = scaledImage.GetDoubleMatrix();
                var width = map.GetLength(1);
                var heigth = map.GetLength(0);
                var result = new List<(int position, double value)>();

                while (hPosition + partHeight < heigth)
                {
                    var mapPart = map.GetMapPart(0, hPosition, width, partHeight);

                    teachBatch.Add((mapPart, (from <= hPosition && to >= hPosition + partHeight ? 1d : -1d)));
                    hPosition += step;
                }
            }
        }

        private static Image GetImage(string path)
        {
            using (var file = Image.FromFile(path))
            {
                return new Bitmap(file);
            }
        }
    }
}
