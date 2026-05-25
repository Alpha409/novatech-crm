using NovaTechCRM.Domain.Models;

namespace NovaTechCRM.Services;

public interface IDiscountEngine
{
    decimal Apply(decimal originalPrice, IEnumerable<DiscountRule> rules);
}

public class DiscountEngine : IDiscountEngine
{
    /// <summary>
    /// Applies the single highest-priority active discount rule to the price.
    /// Rules cascade by category: Contract > Promotional > Volume > Default.
    /// Only ONE rule is applied — the highest-priority match wins.
    ///
    /// BUG: current implementation iterates ALL active rules and applies them
    /// additively, ignoring cascade priority entirely. Finance's spreadsheet
    /// is correct — Sales's "stacking" behaviour is wrong.
    /// </summary>
    public decimal Apply(decimal originalPrice, IEnumerable<DiscountRule> rules)
    {
        var price = originalPrice;

        // BUG: applies every active rule instead of only the highest-priority one.
        // A customer with a Contract (10%) and a Promotional (20%) rule gets
        // both applied: $100 → $90 → $72, instead of the correct $100 → $90.
        foreach (var rule in rules.Where(r => r.IsActive))
        {
            price -= price * (rule.DiscountPercent / 100m);
        }

        return Math.Max(price, 0m);
    }
}
