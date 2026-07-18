namespace DesignStore.Tests.Web;

public sealed class ProductEditorViewTests
{
    [Fact]
    public void Product_editor_contains_ready_and_custom_form_modes()
    {
        var viewPath = FindRepoFile("src", "DesignStore.Web", "Areas", "Admin", "Views", "Products", "Edit.cshtml");
        var html = File.ReadAllText(viewPath);

        Assert.Contains("Hotový kus", html);
        Assert.Contains("Zakázková výroba", html);
        Assert.Contains("data-product-mode=\"ready\"", html);
        Assert.Contains("data-product-mode=\"custom\"", html);
        Assert.Contains("data-product-editor", html);
        Assert.Contains("data-ready-field", html);
        Assert.Contains("data-custom-field", html);
        Assert.Contains("data-translation-tab=\"@culture\"", html);
        Assert.Contains("\"cs-CZ\" =>", html);
        Assert.Contains("\"en\" =>", html);
        Assert.Contains("\"de\" =>", html);
        Assert.Contains("class=\"translation-tab", html);
        Assert.Contains("class=\"translation-panel", html);
        Assert.Contains("data-availability-in-stock", html);
        Assert.Contains("data-availability-made-to-order", html);
        Assert.Contains("data-category-template-select", html);
        Assert.Contains("data-category-slug", html);
        Assert.Contains("Přeložit z češtiny", html);
        Assert.Contains("<label>Slug", html);
        Assert.Contains("asp-for=\"Translations[i].Slug\"", html);
        Assert.DoesNotContain("asp-for=\"Translations[i].Slug\" type=\"hidden\"", html);
    }

    [Fact]
    public void Admin_script_contains_category_text_templates_for_all_supported_cultures()
    {
        var scriptPath = FindRepoFile("src", "DesignStore.Web", "wwwroot", "js", "admin.js");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("kozenekabelky", script);
        Assert.Contains("'cs-CZ'", script);
        Assert.Contains("en:", script);
        Assert.Contains("de:", script);
        Assert.Contains("fillTranslationBlanks", script);
        Assert.Contains("data-category-template-select", script);
    }

    [Fact]
    public void Product_editor_contains_multi_image_drag_and_drop_upload()
    {
        var viewPath = FindRepoFile("src", "DesignStore.Web", "Areas", "Admin", "Views", "Products", "Edit.cshtml");
        var html = File.ReadAllText(viewPath);
        var scriptPath = FindRepoFile("src", "DesignStore.Web", "wwwroot", "js", "admin.js");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("data-dropzone-form", html);
        Assert.Contains("data-dropzone-input", html);
        Assert.Contains("multiple required", html);
        Assert.Contains("name=\"files\"", html);
        Assert.Contains("Array.from(files || []).filter(file => file.type.startsWith('image/')).slice(0, 5)", script);
        Assert.Contains("new DataTransfer()", script);
        Assert.Contains("URL.createObjectURL", script);
        Assert.Contains("URL.revokeObjectURL", script);
        Assert.Contains("dropzone-preview", script);
        Assert.Contains("alt=\"Náhled obrázku", script);
        Assert.Contains("Model.Images.Count", html);
        Assert.Contains("product-image-gallery", html);
        Assert.Contains("image.ThumbnailUrl", html);
        Assert.Contains("image.IsPrimary", html);
        Assert.Contains("Produkt zatím nemá přiřazené žádné fotografie.", html);
    }

    [Fact]
    public void Product_editor_switch_hides_irrelevant_fields_and_uses_server_enum_values()
    {
        var scriptPath = FindRepoFile("src", "DesignStore.Web", "wwwroot", "js", "admin.js");
        var script = File.ReadAllText(scriptPath);
        var cssPath = FindRepoFile("src", "DesignStore.Web", "wwwroot", "css", "admin.css");
        var css = File.ReadAllText(cssPath);

        Assert.Contains("querySelector('[data-product-editor]')", script);
        Assert.Contains("classList.toggle('is-custom-mode'", script);
        Assert.Contains("classList.toggle('is-ready-mode'", script);
        Assert.Contains("dataset.availabilityMadeToOrder", script);
        Assert.Contains("dataset.availabilityInStock", script);
        Assert.Contains(".product-editor.is-ready-mode [data-custom-field]", css);
        Assert.Contains(".product-editor.is-custom-mode [data-ready-field]", css);
    }

    [Fact]
    public void Product_editor_translation_tabs_have_script_and_styles()
    {
        var scriptPath = FindRepoFile("src", "DesignStore.Web", "wwwroot", "js", "admin.js");
        var script = File.ReadAllText(scriptPath);
        var cssPath = FindRepoFile("src", "DesignStore.Web", "wwwroot", "css", "admin-product-editor.css");
        var css = File.ReadAllText(cssPath);
        var layoutPath = FindRepoFile("src", "DesignStore.Web", "Areas", "Admin", "Views", "Shared", "_AdminLayout.cshtml");
        var layout = File.ReadAllText(layoutPath);

        Assert.Contains("data-translation-tab", script);
        Assert.Contains("activateTranslationTab", script);
        Assert.Contains("aria-selected", script);
        Assert.Contains(".translation-tabs", css);
        Assert.Contains(".translation-panel[hidden]", css);
        Assert.Contains("~/css/admin-product-editor.css", layout);
    }

    [Fact]
    public void Product_editor_uses_compact_admin_layout_styles()
    {
        var viewPath = FindRepoFile("src", "DesignStore.Web", "Areas", "Admin", "Views", "Products", "Edit.cshtml");
        var html = File.ReadAllText(viewPath);
        var cssPath = FindRepoFile("src", "DesignStore.Web", "wwwroot", "css", "admin-product-editor.css");
        var css = File.ReadAllText(cssPath);

        Assert.Contains("product-media-card", html);
        Assert.Contains(".product-editor .form-grid", css);
        Assert.Contains("repeat(4, minmax(0, 1fr))", css);
        Assert.Contains(".product-editor.admin-form textarea", css);
        Assert.Contains("min-height: 72px", css);
        Assert.Contains(".product-editor .nested-row", css);
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
