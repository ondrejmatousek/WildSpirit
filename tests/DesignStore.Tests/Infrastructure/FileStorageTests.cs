using DesignStore.Application.Abstractions.Files;
using DesignStore.Infrastructure.Files;
using Microsoft.Extensions.Configuration;
namespace DesignStore.Tests.Infrastructure;
public sealed class FileStorageTests
{
    private static LocalFileStorageService Service()=>new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"FileStorage:Root",Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"))}}).Build());
    [Fact] public async Task Rejects_disallowed_mime()=>await Assert.ThrowsAsync<InvalidDataException>(()=>Service().SaveProductImageAsync(new FileUpload(new MemoryStream([1,2,3]),"bad.svg","image/svg+xml",3),default));
    [Fact] public async Task Rejects_path_traversal()=>await Assert.ThrowsAsync<InvalidDataException>(()=>Service().OpenReadAsync("../secret.txt",default));
}
