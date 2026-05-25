namespace NovaTechCRM.Domain.Models;

public enum DiscountType
{
    Percentage,   // e.g. 20% off
    FlatAmount    // e.g. $50 off
}

public class DiscountRule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DiscountType Type { get; set; }

    /// <summary>
    /// For Percentage: value between 0 and 100 (e.g. 20 = 20% off).
    /// For FlatAmount: absolute dollar amount to deduct (e.g. 50 = $50 off).
    /// </summary>
    public decimal Value { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
