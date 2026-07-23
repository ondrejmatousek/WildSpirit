namespace DesignStore.E2E.Tests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class E2ETestCollection
{
    public const string Name = "Playwright E2E";
}
