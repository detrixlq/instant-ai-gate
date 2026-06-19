namespace InstantAIGate.Application.Dtos.Telemetry
{
    /// <summary>
    /// Operating system resources (CPU and System RAM).
    /// </summary>
    public class SystemHardwareStatus
    {
        public double UsedRamGb { get; set; }
        public double TotalRamGb { get; set; }
        public int CpuUtilizationPercent { get; set; }
    }
}