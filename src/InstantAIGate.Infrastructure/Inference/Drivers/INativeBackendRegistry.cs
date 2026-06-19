using System;
using System.Collections.Generic;
using System.Text;

namespace InstantAIGate.Infrastructure.Inference.Drivers
{
    /// <summary>
    /// Registry that discovers and manages available native backends.
    /// Automatically scans the runtimes folder to find all available backends.
    /// </summary>
    public interface INativeBackendRegistry
    {
        /// <summary>
        /// Gets all discovered backends (both available and unavailable).
        /// </summary>
        IReadOnlyList<NativeBackendInfo> GetAllBackends();

        /// <summary>
        /// Gets only available backends.
        /// </summary>
        IReadOnlyList<NativeBackendInfo> GetAvailableBackends();

        /// <summary>
        /// Gets a specific backend by name.
        /// </summary>
        NativeBackendInfo? GetBackend(string name);

        /// <summary>
        /// Resolves the best backend based on preference and availability.
        /// </summary>
        /// <param name="preferredBackend">Preferred backend name or "auto".</param>
        /// <returns>The resolved backend info.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no suitable backend is found.</exception>
        NativeBackendInfo ResolveBackend(string preferredBackend);

        /// <summary>
        /// Refreshes the backend registry by rescanning the runtimes folder.
        /// </summary>
        void Refresh();
    }
}
