using System;
using System.Collections.Generic;
using System.Text;

namespace InstantAIGate.Infrastructure.Inference.Drivers
{
    /// <summary>
    /// Responsible for loading native libraries into the current process.
    /// </summary>
    public interface INativeLibraryLoader
    {
        /// <summary>
        /// Loads the native library from the specified backend.
        /// </summary>
        /// <param name="backend">Backend information containing the path to native libraries.</param>
        /// <exception cref="InvalidOperationException">Thrown when loading fails.</exception>
        void LoadBackend(NativeBackendInfo backend);

        /// <summary>
        /// Checks whether the native library is currently loaded.
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Gets the currently loaded backend information.
        /// </summary>
        NativeBackendInfo? CurrentBackend { get; }
    }
}
