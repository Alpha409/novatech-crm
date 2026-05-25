using NovaTechCRM.Domain.Models;

namespace NovaTechCRM.Services;

public interface IDiscountEngine
{
    decimal Apply(decimal originalPrice, IEnumerable<DiscountRule> rules);
}

public class DiscountEngine : IDiscountEngine
{
    /// <summary>
    /// Applies all active discount rules to the original price and returns the final price.
    ///
    /// Rules are applied in the order they are received (typically insertion order / ID order).
    /// Both Percentage and FlatAmount discounts reduce the running total sequentially.
    /// </summary>
    public decimal Apply(decimal originalPrice, IEnumerable<DiscountRule> rules)
    {
        var price = originalPrice;

        foreach (var rule in rules.Where(r => r.IsActive))
        {
            if (rule.Type == DiscountType.Percentage)
            {
                price -= price * (rule.Value / 100m);
            }
            else if (rule.Type == DiscountType.FlatAmount)
            {
                price -= rule.Value;
            }
        }

        return Math.Max(price, 0m);
    }
}
