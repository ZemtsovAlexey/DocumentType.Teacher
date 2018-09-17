using DocumentType.Teacher.Models;
using DocumentType.Teacher.Nets;
using Microsoft.AspNetCore.Mvc;
using Neural.Net.CPU.Domain.Layers;
using Neural.Net.CPU.Domain.Save;
using Neural.Net.CPU.Models;
using System;
using System.Collections.Generic;

namespace DocumentType.Teacher.Controllers
{
    [Route("api/net")]
    [ApiController]
    public class NeuralNetworkController : ControllerBase
    {
        public NeuralNetworkController()
        {
            NeuralNetwork.Create();
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
    }
}