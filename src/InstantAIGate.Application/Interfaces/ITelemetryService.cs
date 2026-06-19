using InstantAIGate.Application.Dtos.Telemetry;

namespace InstantAIGate.Application.Interfaces
{
    /// <summary>
    /// Provides real-time, deterministic system telemetry data, aggregating hardware performance metrics 
    /// from the GPU and operational state data from the inference engine manager.
    /// </summary>
    public interface ITelemetryService
    {
        /// <summary>
        /// Captures a point-in-time snapshot of the system's status.
        /// Aggregates GPU resource utilization (memory/temperature) and active model concurrency metrics
        /// without relying on heuristic estimates or long-running averages.
        /// </summary>
        /// <returns>A structured telemetry object containing hardware and model-specific metrics.</returns>
        SystemTelemetry GetCurrentSystemTelemetry();
    }
}