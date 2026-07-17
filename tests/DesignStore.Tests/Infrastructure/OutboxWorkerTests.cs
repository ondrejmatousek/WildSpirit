using DesignStore.Infrastructure.Commerce;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace DesignStore.Tests.Infrastructure;

public sealed class OutboxWorkerTests
{
    [Fact]
    public async Task StopAsync_WhenWorkerIsWaiting_CompletesWithoutCancellationException()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var worker = new OutboxWorker(
            services.GetRequiredService<IServiceScopeFactory>(),
            new TestHostEnvironment(),
            NullLogger<OutboxWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = nameof(OutboxWorkerTests);
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
