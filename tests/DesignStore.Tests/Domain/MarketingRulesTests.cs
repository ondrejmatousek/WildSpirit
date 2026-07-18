using DesignStore.Domain.Marketing;

namespace DesignStore.Tests.Domain;

public sealed class MarketingRulesTests
{
    [Fact]
    public void Email_normalization_is_trimmed_and_case_insensitive()
    {
        Assert.Equal("INFO@WILDSPIRIT.DESIGN", MarketingRules.NormalizeEmail(" info@wildspirit.design "));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void Contact_rate_limit_allows_three_messages_per_window(int recentCount, bool expected)
    {
        Assert.Equal(expected, MarketingRules.CanSubmitContact(recentCount));
    }
}
