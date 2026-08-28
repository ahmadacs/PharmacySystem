namespace Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static readonly Money Zero = new(0m, "JOD");

    public static Money Of(decimal amount, string currency = "JOD")
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        return new Money(amount, currency.Trim().ToUpperInvariant());
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}