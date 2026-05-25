using NovaTechCRM.Domain.Models;

namespace NovaTechCRM.Services;

public interface IDiscountEngine
{
    decimal Apply(decimal originalPrice, IEnumerable<DiscountRule> rules);
}

public class DiscountEngine : IDiscountEngine
{
    public decimal Apply(decimal originalPrice, IEnumerable<DiscountRule> rules)
    {
        var price = originalPrice;

        foreach (var rule in rules.Where(r => r.IsActive).OrderBy(r => r.Type))
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
