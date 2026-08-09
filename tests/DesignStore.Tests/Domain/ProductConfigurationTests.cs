using DesignStore.Domain.Catalog;
namespace DesignStore.Tests.Domain;

public sealed class ProductConfigurationTests
{
    [Fact] public void Price_and_production_time_include_adjustments() { var values = new[] { new ProductOptionValue { PriceAdjustment = 250.25m, AdditionalProductionDays = 2 }, new ProductOptionValue { PriceAdjustment = 99.75m, AdditionalProductionDays = 3 } }; Assert.Equal(1350m, ProductPriceCalculator.Calculate(1000m, values)); Assert.Equal(15, ProductPriceCalculator.CalculateProductionDays(10, values)); }
}
