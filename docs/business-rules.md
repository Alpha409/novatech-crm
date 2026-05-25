# NovaTech CRM — Discount Business Rules

_Last updated by Sales Ops, approved by Finance — Q3 2023_

---

## How Discounts Work

When multiple discount rules apply to an order, they must be applied in the
following mandatory order:

1. **Percentage discounts first** — all percentage-based discounts are applied
   to the original order total before any flat-amount deductions.
2. **Flat-amount discounts second** — fixed dollar deductions are applied after
   all percentage discounts have been calculated.

### Why this order matters

Consider a $200 order with a 20% promotional discount and a $50 loyalty voucher:

| Step | Correct (% first)         | Wrong (flat first)         |
|------|---------------------------|----------------------------|
| 1    | $200 × 0.80 = **$160**    | $200 − $50 = **$150**      |
| 2    | $160 − $50  = **$110** ✓  | $150 × 0.80 = **$120** ✗   |

The correct final price is **$110**. Applying flat amounts first inflates the
effective percentage saving and results in the customer being **overcharged**.

Finance's spreadsheet implements this correctly.
Sales's system was also correct until the discount engine was refactored in
commit `a3f9c2` (October 2023) — the refactor changed rule iteration to
insertion order, silently breaking the % → flat guarantee.

---

## Additional Rules

- Discounts cannot reduce an order below $0.
- Inactive rules (`IsActive = false`) are always skipped.
- Percentage values are whole numbers (20 = 20%, not 0.20).
- Flat-amount discounts are in USD.

---

## Affected Scenarios

The bug only manifests when an order has **both** at least one Percentage rule
**and** at least one FlatAmount rule, AND the FlatAmount rule has a lower `Id`
(i.e. was created earlier) than the Percentage rule.

Single-discount orders are unaffected — which is why Sales did not notice the
regression during their own testing.
