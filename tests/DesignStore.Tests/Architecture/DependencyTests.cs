using System.Reflection;
using DesignStore.Domain.Catalog;

namespace DesignStore.Tests.Architecture;

public sealed class DependencyTests
{
    [Fact]
    public void Domain_does_not_reference_infrastructure_or_web()
    {
        var references = typeof(Product).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
        Assert.DoesNotContain("DesignStore.Infrastructure", references);
        Assert.DoesNotContain("DesignStore.Web", references);
    }

    [Fact]
    public void Application_does_not_reference_web_or_infrastructure()
    {
        var assembly = Assembly.Load("DesignStore.Application");
        var references = assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
        Assert.DoesNotContain("DesignStore.Web", references);
        Assert.DoesNotContain("DesignStore.Infrastructure", references);
    }
}
