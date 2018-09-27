using DocumentType.Teacher.Models;
using DocumentType.Teacher.Nets;
using Microsoft.AspNetCore.Mvc;
using Neural.Net.CPU.Domain.Layers;
using Neural.Net.CPU.Domain.Save;
using Neural.Net.CPU.Models;
using System;
using System.Drawing;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Drawing.Imaging;
using System.Linq;
using ImageProcessing;

namespace DocumentType.Teacher.Controllers
{
    [Route("api/net")]
    [ApiController]
    public class NeuralNetworkController : ControllerBase
    {
        public NeuralNetworkController()
        {
//            NeuralNetwork.Create();
        }

        [HttpGet("settings")]
        public NetSettings[] GetSettings()
        {
            if (NeuralNetwork.Net == null)
            {
                return null;
            }

            var settings = new NetSettings[NeuralNetwork.Net.Layers.Length];

            for (var i = 0; i < NeuralNetwork.Net.Layers.Length; i++)
            {
                var layer = NeuralNetwork.Net.Layers[i];

                switch (layer.Type)
                {
                    case LayerType.Convolution:
                        var convLayer = (IConvolutionLayer)layer;
                        settings[i] = new NetSettings(layer.Type, convLayer.ActivationFunctionType, convLayer.NeuronsCount, convLayer.KernelSize);
                        break;

                    case LayerType.MaxPoolingLayer:
                        var poolLayer = (IMaxPoolingLayer)layer;
                        settings[i] = new NetSettings(layer.Type, ActivationType.None, null, poolLayer.KernelSize);
                        break;

                    case LayerType.FullyConnected:
                        var fullLayer = (IFullyConnectedLayer)layer;
                        settings[i] = new NetSettings(layer.Type, fullLayer.ActivationFunctionType, fullLayer.NeuronsCount, null);
                        break;

                    default:
                        throw new ArgumentException();
                }
            }

            return settings;
        }

        [HttpPost("settings/apply")]
        public void ApplySettings(NetSettings[] settings)
        {
            NeuralNetwork.Create(602, 34, settings);
        }
        
        [HttpPost("[action]")]
        public double[] Compute(IFormFile file)
        {
            var image = Image.FromStream(file.OpenReadStream());
            var result = NeuralNetwork.Compute(image);

            return new[] { 0d };
        }

        [HttpGet("layer/{layerIndex:int}")]
        public IActionResult GetLayerViews(int layerIndex)
        {
            var views = NeuralNetwork.GetLayerViews(layerIndex);
            //var result = views.Select((x, i) => File(x, "image/jpeg", $"test {i}")).ToArray();

            return File(views[0], "image/jpeg", "test");
        }

        [HttpPost("compute/image")]
        public IActionResult ComputeImage(IFormFile file)
        {
            var image = Image.FromStream(file.OpenReadStream());
            var result = NeuralNetwork.Compute(image);

            return File(result.ToByteArray(), "image/jpeg", "test");
        }

        [HttpPost("teach/run")]
        public void TeachRun()
        {
            NeuralNetwork.TeachRun();
        }
        
        [HttpPost("teach/stop")]
        public void TeachStop()
        {
            NeuralNetwork.TeachStop();
        }
        
        [HttpPost("teach/batch")]
        public void PrepareTeachBatchFile()
        {
            NeuralNetwork.PrepareTeachBatchFile();
        }
    }
}