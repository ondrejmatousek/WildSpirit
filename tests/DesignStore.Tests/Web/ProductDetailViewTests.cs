namespace DesignStore.Tests.Web;

public sealed class ProductDetailViewTests
{
    [Fact]
    public void Product_detail_uses_main_image_with_thumbnail_gallery()
    {
        var viewPath = FindRepoFile("src", "DesignStore.Web", "Views", "Catalog", "Detail.cshtml");
        var html = File.ReadAllText(viewPath);
        var cssPath = FindRepoFile("src", "DesignStore.Web", "wwwroot", "css", "vintage.css");
        var css = File.ReadAllText(cssPath);
        var scriptPath = FindRepoFile("src", "DesignStore.Web", "wwwroot", "js", "site.js");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("data-product-gallery", html);
        Assert.Contains("data-product-gallery-main", html);
        Assert.Contains("data-product-thumb", html);
        Assert.Contains("?thumbnail=true", html);
        Assert.Contains(".detail-main-media img", css);
        Assert.Contains("object-fit: contain", css);
        Assert.Contains(".detail-thumbnails", css);
        Assert.Contains("grid-auto-flow: column", css);
        Assert.Contains("[data-product-thumb]", script);
        Assert.Contains("main.src=button.dataset.src", script);
    }

    private static string FindRepoFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Repository file was not found.", Path.Combine(parts));
    }
}
