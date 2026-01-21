using CoreBank.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreBank.Infrastructure.BackgroundJobs;

public class ScheduledPaymentJob
{
    private readonly IScheduledPaymentService _scheduledPaymentService;
    private readonly ILogger<ScheduledPaymentJob> _logger;

    public ScheduledPaymentJob(
        IScheduledPaymentService scheduledPaymentService,
        ILogger<ScheduledPaymentJob> logger)
    {
        _scheduledPaymentService = scheduledPaymentService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting scheduled payment processing job at {Time}", DateTime.UtcNow);

        try
        {
            await _scheduledPaymentService.ProcessDuePaymentsAsync();
            _logger.LogInformation("Scheduled payment processing job completed at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scheduled payment processing job");
            throw;
        }
    }
}
