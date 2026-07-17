using DesignStore.Domain.Catalog;

namespace DesignStore.Tests.Domain;

public sealed class ProductAvailabilityTests
{
    [Fact]
    public void Variant_product_uses_variant_stock_as_authority()
    {
        var product = new Product { IsPublished = true, HasVariants = true, StockQuantity = 0, AvailabilityStatus = AvailabilityStatus.InStock };
        product.Variants.Add(new ProductVariant { AvailabilityStatus = AvailabilityStatus.InStock, StockQuantity = 1 });

        Assert.True(product.CanAddToCart());
    }
    [Fact]
    public void In_stock_published_product_with_quantity_can_be_added_to_cart()
    {
        var product = new Product { IsPublished = true, AvailabilityStatus = AvailabilityStatus.InStock, TrackInventory = true, StockQuantity = 1 };
        Assert.True(product.CanAddToCart());
        Assert.False(product.CanRequestProduction());
    }

    [Theory]
    [InlineData(AvailabilityStatus.InStock, true, false)]
    [InlineData(AvailabilityStatus.MadeToOrder, true, true)]
    [InlineData(AvailabilityStatus.MadeToOrder, false, false)]
    [InlineData(AvailabilityStatus.Unavailable, true, false)]
    [InlineData(AvailabilityStatus.Discontinued, true, false)]
    public void Production_request_obeys_availability(AvailabilityStatus status, bool canMake, bool expected)
    {
        var product = new Product { IsPublished = true, AvailabilityStatus = status, CanBeMadeToOrder = canMake };
        Assert.Equal(expected, product.CanRequestProduction());
    }
}
