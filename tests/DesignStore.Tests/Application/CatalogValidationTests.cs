using DesignStore.Application.Catalog;
using DesignStore.Domain.Catalog;
namespace DesignStore.Tests.Application;

public sealed class CatalogValidationTests
{
    [Fact]
    public async Task Variant_product_cannot_keep_product_stock()
    {
        var m = Valid(); m.HasVariants = true; m.StockQuantity = 1;
        m.Variants.Add(new ProductVariantInput { Sku = "VAR-1", Name = "Varianta" });
        var r = await new EditProductRequestValidator().ValidateAsync(m);
        Assert.Contains(r.Errors, x => x.ErrorMessage.Contains("pouze na variantách"));
    }
    [Fact] public async Task Made_to_order_requires_manufacturing_flag() { var m = Valid(); m.AvailabilityStatus = AvailabilityStatus.MadeToOrder; Assert.False((await new EditProductRequestValidator().ValidateAsync(m)).IsValid); }
    [Fact] public async Task Administration_rejects_html_in_plain_text_content() { var m = Valid(); m.Translations.Single().LongDescription = "<script>alert(1)</script>"; var result = await new EditProductRequestValidator().ValidateAsync(m); Assert.Contains(result.Errors, x => x.ErrorMessage.Contains("HTML")); }
    private static EditProductRequest Valid() => new() { Sku = "TEST-001", CategoryId = Guid.NewGuid(), BasePrice = 100, Translations = [new ProductTranslationInput { Culture = "cs-CZ", Name = "Test", Slug = "test" }] };
}
