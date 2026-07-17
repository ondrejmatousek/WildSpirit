using DesignStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DesignStore.Infrastructure.Commerce;

public sealed class OutboxWorker(
    IServiceScopeFactory scopes,
    IHostEnvironment environment,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (environment.IsDevelopment())
            {
                try
                {
                    await Process(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Outbox processing failed.");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task Process(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messages = await db.OutboxMessages
            .Where(message =>
                message.ProcessedAtUtc == null &&
                (message.NextAttemptAtUtc == null || message.NextAttemptAtUtc <= DateTime.UtcNow))
            .OrderBy(message => message.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        var root = Path.Combine(environment.ContentRootPath, "App_Data", "emails");
        Directory.CreateDirectory(root);

        foreach (var message in messages)
        {
            try
            {
                await File.WriteAllTextAsync(
                    Path.Combine(root, $"{message.Id:N}.json"),
                    message.Payload,
                    cancellationToken);
                message.ProcessedAtUtc = DateTime.UtcNow;
                message.LastError = "";
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message.AttemptCount++;
                message.NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(Math.Min(60, message.AttemptCount * 5));
                message.LastError = exception.Message[..Math.Min(2000, exception.Message.Length)];
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
