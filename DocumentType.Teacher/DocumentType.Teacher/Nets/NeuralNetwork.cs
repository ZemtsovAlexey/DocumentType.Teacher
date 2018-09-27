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
using Neural.Net.CPU.Domain.Layers;
using Neural.Net.CPU.Models;

namespace DocumentType.Teacher.Nets
{
    public static class NeuralNetwork
    {
        public static Network Net;
        public static bool Running { get; set; }

        private static BackPropagationLearning teacher;
        private static List<(double[,] map, double target)> teachBatch;
        private static (int width, int height) imageSize;
        private static int scanStep => imageSize.height > 5 ? imageSize.height / 5 : imageSize.height;

        public static event EventHandler<TeachResult> IterationChange;

        static NeuralNetwork()
        {
            Create(602, 34);
            PrepareTeachBatchFile();
        }
        
        public static void Create(int width, int height)
        {
            imageSize = (width, height);

            Net = new Network();

            Net.InitLayers(width, height,
                new ConvolutionLayer(ActivationType.ReLu, 5, 7), //596 - 20
                new MaxPoolingLayer(2), // 298 - 10
                new ConvolutionLayer(ActivationType.ReLu, 10, 3), //296 - 8
                new MaxPoolingLayer(2), // 148 - 4
                new FullyConnectedLayer(50, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(50, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(50, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(50, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(1, ActivationType.BipolarSigmoid));

            Net.Randomize();

            teacher = new BackPropagationLearning(Net);
        }
        
        public static void Create(int width, int height, NetSettings[] settings)
        {
            imageSize = (width, height);

            Net = new Network();

            var layers = new List<ILayer>();

            foreach (var setting in settings)
            {
                switch (setting.Type)
                {
                    case LayerType.Convolution:
                        layers.Add(new ConvolutionLayer(setting.Activation.Value, setting.NeuronsCount.Value, setting.KernelSize.Value));
                        break;
                    case LayerType.FullyConnected:
                        layers.Add(new FullyConnectedLayer(setting.NeuronsCount.Value, setting.Activation.Value));
                        break;
                    case LayerType.MaxPoolingLayer:
                        layers.Add(new MaxPoolingLayer(setting.KernelSize.Value));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            Net.InitLayers(width, height, layers.ToArray());

            Net.Randomize();

            teacher = new BackPropagationLearning(Net);
        }

        public static Image Compute(Image image)
        {
//            var a = BitmapExt.SomeMethode((Bitmap)image);
//            image = image.RotateImage((float)a);
            
            var scaledImage = ((Bitmap)image)
                .ToBlackWite()
                .ScaleImage(imageSize.width, 100000);
            var map = scaledImage.GetDoubleMatrix();
            var width = map.GetLength(1);
            var heigth = map.GetLength(0);
            var partHeight = imageSize.height;
            var hPosition = 0;
            var step = scanStep;

            var result = new List<(int position, double value)>();

            while(hPosition + partHeight < heigth)
            {
                var mapPart = map.GetMapPart(0, hPosition, width, partHeight);
                var value = Net.Compute(mapPart);

                result.Add((hPosition, value[0]));
                hPosition += step;
            }

            var cords = result.Where(x => x.value > 0.3d).Select(x => new Cords { Top = x.position, Bottom = x.position + 26 }).ToList();

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
                    var prevSuccess = success;

                    if (falseAnswers < 1)
                    {
                        falseAnswers++;
                        trueAnswers = 0;

                        var rndFileIndex = rnd.Next(batchFalseLength);

                        input = batchFalse[rndFileIndex].map;
                        computed = Net.Compute(input)[0];
                        target = new[] { -1d };

                        success = computed <= 0 ? success + 1 : 0;
                        globalSuccess = computed <= 0 ? globalSuccess + 1 : globalSuccess;
                    }
                    else
                    {
                        falseAnswers = 0;
                        trueAnswers++;

                        var rndFileIndex = rnd.Next(batchTrueLength);

                        input = batchTrue[rndFileIndex].map;
                        computed = Net.Compute(input)[0];
                        target = new[] { 1d };

                        success = computed > 0.9d ? success + 1 : 0;
                        globalSuccess = computed > 0.9d ? globalSuccess + 1 : globalSuccess;
                    }

                    totalSuccess = Math.Max(0, success > totalSuccess ? success : totalSuccess - 1);
//                    error += prevSuccess > success ? teacher.Run(input, target) : 0;
                    error += teacher.Run(input, target);
                    iteration++;

//                    if (iteration % 10 == 0)
                        IterationChange?.Invoke(new object(), new TeachResult
                        {
                            Iteration = iteration, 
                            Error = error / (double)iteration, 
                            Successes = totalSuccess,
                            SuccessPercent = globalSuccess / (double)iteration
                        });
                }
            }).ConfigureAwait(false);
        }

        public static void TeachStop()
        {
            Running = false;
        }

        public static List<byte[]> GetLayerViews(int layerIndex)
        {
            var layer = (IMatrixLayer)Net.Layers[layerIndex];

            return layer.Outputs.Select(x => x.Value.ToBitmap().ToByteArray()).ToList();
        }

        public static void PrepareTeachBatchFile()
        {
            teachBatch = new List<(double[,] map, double target)>();
            var imagesPaths = Directory.EnumerateFiles(@"TeachData/invoices", "*.jpg").ToArray();

            foreach (var path in imagesPaths)
            {
                var match = Regex.Match(path, @"(?<from>\d+)-(?<to>\d+).jpg$");

                if (!match.Success)
                    continue;

                var image = (Bitmap)GetImage(path);
                var ratio = (double)imageSize.width / image.Width;
                var from = 0d;
                var to = 0d;
                var partHeight = imageSize.height;
                var hPosition = 0;
                var step = 1;

                if (match.Success)
                {
                    from = Convert.ToDouble(match.Groups["from"].Value) * ratio;
                    to = Convert.ToDouble(match.Groups["to"].Value) * ratio;
                    to = to - from < partHeight ? from + partHeight + step : to;
                }

                var scaledImage = image
                    .ToBlackWite()
                    .ScaleImage(imageSize.width, 100000);
                var map = scaledImage.GetDoubleMatrix();
                var width = map.GetLength(1);
                var heigth = map.GetLength(0);
                var result = new List<(int position, double value)>();

                while (hPosition + partHeight < heigth)
                {
                    if ((hPosition < from && hPosition + partHeight > from) || 
                        (hPosition > from && hPosition < to && hPosition + partHeight > to))
                    {
                        hPosition += step;
                        continue;
                    }
                    
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
