using Neural.Net.CPU.Domain.Save;
using Neural.Net.CPU.Models;

namespace DocumentType.Teacher.Models
{
    public class NetSettings
    {
        public NetSettings(LayerType type, ActivationType? activation, int? neuronsCount, int? kernelSize)
        {
            Type = type;
            Activation = activation;
            NeuronsCount = neuronsCount;
            KernelSize = kernelSize;
        }

        public LayerType Type { get; set; }

        public ActivationType? Activation { get; set; }

        public int? NeuronsCount { get; set; }

        public int? KernelSize { get; set; }
    }
}
