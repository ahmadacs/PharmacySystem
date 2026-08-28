namespace Domain.ValueObjects;

public sealed record Quantity
{
    public int Value { get; }

    private Quantity(int value) => Value = value;

    public static readonly Quantity Zero = new(0);

    public static Quantity Of(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Quantity cannot be negative.");

        return new Quantity(value);
    }

    public Quantity Add(Quantity other) => new(Value + other.Value);

    public Quantity Subtract(Quantity other)
    {
        var result = Value - other.Value;
        if (result < 0)
            throw new InvalidOperationException("Resulting quantity cannot be negative.");

        return new Quantity(result);
    }

    public bool IsZero => Value == 0;

    public override string ToString() => Value.ToString();
}