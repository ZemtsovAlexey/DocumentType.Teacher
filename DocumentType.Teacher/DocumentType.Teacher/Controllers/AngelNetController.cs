﻿using DocumentType.Teacher.Models;
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
    [Route("api/net/angel")]
    [ApiController]
    public class AngelNetController : ControllerBase
    {
        public AngelNetController()
        {
//            NeuralNetwork.Create();
        }

        [HttpGet("settings")]
        public NetSettings[] GetSettings()
        {
            if (DocumentAngelNet.Net == null)
            {
                return null;
            }

            var settings = new NetSettings[DocumentAngelNet.Net.Layers.Length];

            for (var i = 0; i < DocumentAngelNet.Net.Layers.Length; i++)
            {
                var layer = DocumentAngelNet.Net.Layers[i];

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
            NeuralNetwork.Create(602, 26, settings);
        }
        
        [HttpGet("settings/reset")]
        public void Reset()
        {
            DocumentAngelNet.Reset();
        }
        
        [HttpPost("[action]")]
        public IActionResult Compute(IFormFile file)
        {
            var image = Image.FromStream(file.OpenReadStream());
            var result = DocumentAngelNet.Compute(image);

            return File(result.ToByteArray(), "image/jpeg", "test");
        }

        [HttpGet("[action]")]
        public IActionResult Save()
        {
            var data = DocumentAngelNet.Save();

            return File(data, "application/octet-stream", $"documentTypeNW-{DateTime.Now}.nw");
        }
        
        [HttpPost("[action]")]
        public void Load(IFormFile file)
        {
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                DocumentAngelNet.Load(ms.ToArray());
            }
        }

        [HttpPost("[action]")]
        public void Test(TestRequestDto request)
        {
            var imgPaths = Directory.GetFiles(request.PathFrom, "*.jpg", SearchOption.AllDirectories);
            var i = 0;
            
            foreach (var path in imgPaths.Take(300))
            {
                try
                {
                    var img = new Bitmap(path);
                    var result = DocumentAngelNet.Compute(img);
                
                    result.Save($"{request.PathTo}/{i}.jpg");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                
                i++;
            }
        }
        
        [HttpGet("teach/learningRate")]
        public double GetLearningRate()
        {
            return DocumentAngelNet.LearningRate;
        }
        
        [HttpPost("teach/learningRate/{value:double}")]
        public void SetLearningRate([FromRoute]double value)
        {
            DocumentAngelNet.LearningRate = value;
        }

        [HttpPost("teach/run")]
        public void TeachRun()
        {
            DocumentAngelNet.TeachRun();
        }
        
        [HttpPost("teach/stop")]
        public void TeachStop()
        {
            DocumentAngelNet.TeachStop();
        }
        
        [HttpPost("teach/batch")]
        public void PrepareTeachBatchFile()
        {
            DocumentAngelNet.PrepareTeachBatchFile();
        }
    }

    public class TestRequestDto
    {
        public string PathFrom { get; set; }
        
        public string PathTo { get; set; }
    }
}