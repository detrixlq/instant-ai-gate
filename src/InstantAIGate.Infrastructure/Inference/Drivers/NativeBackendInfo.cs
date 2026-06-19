using System;
using System.Collections.Generic;
using System.Text;

namespace InstantAIGate.Infrastructure.Inference.Drivers
{

    /// <summary>
    /// Represents a discovered native backend (e.g., cpu, vulkan, cuda).
    /// </summary>
    public class NativeBackendInfo
    {
        /// <summary>
        /// Runtime Identifier (e.g., "win-x64", "linux-arm64").
        /// </summary>
        public string Rid { get; set; } = string.Empty;

        /// <summary>
        /// Backend name derived from the folder name (e.g., "cpu", "vulkan", "cuda").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Full absolute path to the backend folder.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// True if the folder contains the main native library (llama.dll / libllama.so).
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Helper flag: true if this is a GPU accelerator (not "cpu").
        /// </summary>
        public bool IsGpu => !Name.Equals("cpu", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// List of native library files found in this backend folder.
        /// </summary>
        public List<string> LibraryFiles { get; set; } = [];
    }
}
