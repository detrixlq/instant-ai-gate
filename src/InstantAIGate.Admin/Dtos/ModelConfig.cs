using InstantAIGate.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace InstantAIGate.Admin.Dtos
{
    public class ModelConfig
    {
        public string? RepoId { get; set; }
        public bool Embeddings { get; set; }
        public uint? ContextSize { get; set; } = 2048;
        public int GpuLayerCount { get; set; } = -1; 
        public bool FlashAttention { get; set; }  = true;
        public int Threads { get; set; } = 4;
        public int MainGpu { get; set; } = 0; //Video card index(if there are several)
        public int MaxContexts { get; set; } = 1;
        public string KvCacheQuantization { get; set; } = "F16";
        public uint BatchSize { get; set; }   
        public bool UseMemoryLock { get; set; }

    }
}
