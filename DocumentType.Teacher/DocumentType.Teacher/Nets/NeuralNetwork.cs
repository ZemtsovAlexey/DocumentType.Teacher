using ImageProcessing;
using Neural.Net.CPU.Domain.Save;
using Neural.Net.CPU.Layers;
using Neural.Net.CPU.Learning;
using Neural.Net.CPU.Networks;
using System.Drawing;

namespace DocumentType.Teacher.Nets
{
    public static class NeuralNetwork
    {
        public static Network Net;
        public static double Error { get; private set; }

        private static BackPropagationLearning teacher;

        public static void Create()
        {
            Net = new Network();

            Net.InitLayers(26, 26,
                new ConvolutionLayer(ActivationType.ReLu, 2, 3),
                new MaxPoolingLayer(2),
                new ConvolutionLayer(ActivationType.ReLu, 4, 3),
                new MaxPoolingLayer(2),
                new FullyConnectedLayer(100, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(50, ActivationType.BipolarSigmoid),
                new FullyConnectedLayer(1, ActivationType.BipolarSigmoid));

            Net.Randomize();

            teacher = new BackPropagationLearning(Net);
        }

        public static double[] Compute(Bitmap image)
        {
            return Net.Compute(image.GetDoubleMatrix());
        }

        public static void TeachRun(double[,] input, double[] target)
        {
            Error = teacher.Run(input, target);
        }
    }
}
