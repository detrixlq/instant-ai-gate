using InstantAIGate.API.Hub;
using InstantAIGate.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace InstantAIGate.API.Services
{
    public class TelemetryBroadcastService : BackgroundService
    {
        private readonly IHubContext<TelemetryHub> _hubContext;
        private readonly ITelemetryService _telemetryService;
        private readonly ILogger<TelemetryBroadcastService> _logger;

        public TelemetryBroadcastService(IHubContext<TelemetryHub> hubContext, ITelemetryService telemetryService, ILogger<TelemetryBroadcastService> logger)
        {
            _hubContext = hubContext;
            _telemetryService = telemetryService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var snapshot = _telemetryService.GetCurrentSystemTelemetry();
                    // Broadcast to all clients subscribed to the group
                    await _hubContext.Clients.Group("TelemetryConsumers").SendAsync("ReceiveTelemetry", snapshot, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error broadcasting telemetry");
                }
                await Task.Delay(1500, stoppingToken);
            }
        }
    }
}
