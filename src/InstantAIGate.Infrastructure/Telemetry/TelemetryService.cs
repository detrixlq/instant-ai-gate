using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using InstantAIGate.Application.Dtos.Telemetry;
using InstantAIGate.Application.Interfaces;
using InstantAIGate.Application.Interfaces.Inference;
using InstantAIGate.Infrastructure.NvmlNative;
using Microsoft.Extensions.Logging;

namespace InstantAIGate.Infrastructure.Telemetry
{
    public class TelemetryService : ITelemetryService
    {
        private readonly IModelManager _modelManager;
        private readonly NvmlProvider _nvmlProvider;
        private readonly ILogger<TelemetryService> _logger;

        // Fields for calculating CPU Load
        private DateTime _lastCpuCheck = DateTime.UtcNow;
        private TimeSpan _lastCpuTime = TimeSpan.Zero;
        private int _lastCalculatedCpuUsage = 0;
        private readonly object _cpuLock = new();

        public TelemetryService(
            IModelManager modelManager,
            NvmlProvider nvmlProvider,
            ILogger<TelemetryService> logger)
        {
            _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
            _nvmlProvider = nvmlProvider ?? throw new ArgumentNullException(nameof(nvmlProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize the starting checkpoint for CPU tracking
            _lastCpuTime = Process.GetCurrentProcess().TotalProcessorTime;
        }

        public SystemTelemetry GetCurrentSystemTelemetry()
        {
            var telemetry = new SystemTelemetry();

            // 1. Collect Hardware Metrics (GPU)
            telemetry.Gpu = new GpuStatus
            {
                UsedMemoryGb = _nvmlProvider.GetUsedMemoryGb(),
                TotalMemoryGb = _nvmlProvider.GetTotalMemoryGb(),
                TemperatureCelsius = _nvmlProvider.GetTemperature(),
                UtilizationPercent = _nvmlProvider.GetUtilization()
            };

            // 2. Collect System Metrics (CPU & RAM)
            telemetry.System = GetSystemHardwareStatus();

            // 3. Collect Model Metrics from the unified Native Manager
            var nativeDetails = _modelManager.GetNativeDetails();
            var activeModels = _modelManager.ActiveModels;
            var semaphores = _modelManager.UserSemaphores;

            foreach (var detail in nativeDetails)
            {
                var repoId = detail.RepoId;

                activeModels.TryGetValue(repoId, out var config);
                semaphores.TryGetValue(repoId, out var sem);

                int maxUsers = config?.MaxContexts ?? 0;
                int activeUsers = (sem != null && maxUsers > 0) ? (maxUsers - sem.CurrentCount) : 0;

                telemetry.Models.Add(new ModelTelemetry
                {
                    RepoId = repoId,
                    IsLoaded = true,
                    ContextSize = detail.ContextSize,
                    GpuLayers = detail.GpuLayers,
                    Threads = detail.Threads,
                    FlashAttention = detail.FlashAttention,
                    IdleContextsCount = detail.IdleContextsCount,
                    MaxParallelUsers = maxUsers,
                    ActiveUsersCount = activeUsers,
                    IsQueueWaiting = (sem != null && sem.CurrentCount == 0)
                });
            }

            return telemetry;
        }

        private SystemHardwareStatus GetSystemHardwareStatus()
        {
            var status = new SystemHardwareStatus();

            // --- COLLECT RAM METRICS ---
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                GetWindowsMemory(out double totalGb, out double usedGb);
                status.TotalRamGb = totalGb;
                status.UsedRamGb = usedGb;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                GetLinuxMemory(out double totalGb, out double usedGb);
                status.TotalRamGb = totalGb;
                status.UsedRamGb = usedGb;
            }
            else
            {
                // Fallback for other platforms using the current process working set
                status.TotalRamGb = 16.0;
                status.UsedRamGb = Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0 / 1024.0;
            }

            // --- COLLECT CPU METRICS ---
            status.CpuUtilizationPercent = CalculateCpuUsage();

            return status;
        }

        private int CalculateCpuUsage()
        {
            lock (_cpuLock)
            {
                var currentTime = DateTime.UtcNow;
                var currentCpuTime = Process.GetCurrentProcess().TotalProcessorTime;

                var timeWindow = currentTime - _lastCpuCheck;

                // Throttling to prevent calculation spam more frequently than every 500ms
                if (timeWindow.TotalMilliseconds < 500)
                {
                    return _lastCalculatedCpuUsage;
                }

                var systemTimeDelta = timeWindow.TotalMilliseconds * Environment.ProcessorCount;
                var cpuTimeDelta = (currentCpuTime - _lastCpuTime).TotalMilliseconds;

                _lastCpuCheck = currentTime;
                _lastCpuTime = currentCpuTime;

                if (systemTimeDelta <= 0) return _lastCalculatedCpuUsage;

                double usage = (cpuTimeDelta / systemTimeDelta) * 100;
                _lastCalculatedCpuUsage = Math.Clamp((int)Math.Round(usage), 0, 100);

                return _lastCalculatedCpuUsage;
            }
        }

        #region Memory Helpers (Native API / ProcFS)

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX() => dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
        }

        private void GetWindowsMemory(out double totalGb, out double usedGb)
        {
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                totalGb = memStatus.ullTotalPhys / 1024.0 / 1024.0 / 1024.0;
                usedGb = (memStatus.ullTotalPhys - memStatus.ullAvailPhys) / 1024.0 / 1024.0 / 1024.0;
            }
            else
            {
                totalGb = 0; usedGb = 0;
            }
        }

        private void GetLinuxMemory(out double totalGb, out double usedGb)
        {
            totalGb = 0; usedGb = 0;
            try
            {
                string[] lines = File.ReadAllLines("/proc/meminfo");
                double memTotalKib = 0;
                double memAvailableKib = 0;

                foreach (var line in lines)
                {
                    if (line.StartsWith("MemTotal:"))
                        memTotalKib = double.Parse(System.Text.RegularExpressions.Regex.Match(line, @"\d+").Value);
                    if (line.StartsWith("MemAvailable:"))
                        memAvailableKib = double.Parse(System.Text.RegularExpressions.Regex.Match(line, @"\d+").Value);
                }

                totalGb = memTotalKib / 1024.0 / 1024.0;
                usedGb = (memTotalKib - memAvailableKib) / 1024.0 / 1024.0;
            }
            catch
            {
                // Fallback catch block to swallow exceptions under non-standard sysfs mapping configurations
            }
        }
        #endregion
    }
}