using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Cogitatio.Models;

/// <summary>
/// not used.  it was created to help debug some signalr issues
/// </summary>
public class LoggingCircuitHandler : CircuitHandler
{
    private readonly ILogger<LoggingCircuitHandler> logger;

    public LoggingCircuitHandler(ILogger<LoggingCircuitHandler> logger)
    {
        this.logger = logger;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Circuit opened: {circuit.Id}");
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Circuit Down: {circuit.Id}");
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Circuit closed: {circuit.Id}");
        return Task.CompletedTask;
    }
}