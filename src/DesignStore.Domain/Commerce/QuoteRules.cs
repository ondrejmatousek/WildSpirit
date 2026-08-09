namespace DesignStore.Domain.Commerce;

public static class QuoteRules
{
    public static bool CanTransition(QuoteStatus from, QuoteStatus to) => (from, to) switch
    {
        (QuoteStatus.Draft, QuoteStatus.Sent or QuoteStatus.Cancelled) => true,
        (QuoteStatus.Sent, QuoteStatus.Accepted or QuoteStatus.Rejected or QuoteStatus.ChangeRequested or QuoteStatus.Expired) => true,
        (QuoteStatus.ChangeRequested, QuoteStatus.Sent or QuoteStatus.Cancelled) => true,
        (QuoteStatus.Accepted, QuoteStatus.ConvertedToOrder) => true,
        _ => false
    };
    public static bool IsUsable(DateTime validUntilUtc, QuoteStatus status, DateTime nowUtc) => validUntilUtc > nowUtc && status is QuoteStatus.Sent or QuoteStatus.ChangeRequested;
}
