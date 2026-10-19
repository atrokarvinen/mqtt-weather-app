using Microsoft.AspNetCore.Mvc;

namespace MqttWeatherStorage.Controllers;

[ApiController]
[Route("queue-diagnostics")]
public class QueueDiagnosticsController : ControllerBase
{
    [HttpGet]
    public QueueDiagnosticsResponse GetQueueDiagnostics()
    {
        return new QueueDiagnosticsResponse(MqttWeatherStoreQueue.GetQueueLength());
    }
}

public sealed record QueueDiagnosticsResponse(int QueueSize);
