using Microsoft.Data.SqlClient;
using NovaTechCRM.Domain.Models;

namespace NovaTechCRM.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int customerId, CancellationToken ct);
    Task<List<OrderSummary>> GetOrderSummariesAsync(int customerId, CancellationToken ct);
}

public class CustomerRepository : ICustomerRepository
{
    private readonly string _connectionString;

    public CustomerRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Customer?> GetByIdAsync(int customerId, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new SqlCommand(
            "SELECT Id, Name, Email, CreatedAt FROM Customers WHERE Id = @id", conn);
        cmd.Parameters.AddWithValue("@id", customerId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new Customer
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Email = reader.GetString(2),
            CreatedAt = reader.GetDateTime(3),
        };
    }

    /// <summary>
    /// Returns order summaries for a customer.
    ///
    /// BUG (introduced in SQL Server 2019 migration, commit b4e91f):
    /// Original query used a single JOIN against the denormalized ReportingCache table.
    /// During migration the ReportingCache schema changed — the join column was renamed
    /// from CustomerRef to CustomerId. Rather than fix the JOIN, the migration script
    /// dropped the JOIN and rewrote this as a loop: one query to fetch order IDs,
    /// then one query per order to fetch line items.
    ///
    /// New customers (< 18 months) have few orders so the N+1 is invisible (~50ms).
    /// Legacy customers have 300-1500 orders — each triggers a round-trip to SQL Server,
    /// causing 40-60 second load times.
    /// </summary>
    public async Task<List<OrderSummary>> GetOrderSummariesAsync(int customerId, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        // Step 1: fetch all order IDs for this customer
        var orderIds = new List<int>();
        await using (var cmd = new SqlCommand(
            "SELECT Id FROM Orders WHERE CustomerId = @cid ORDER BY CreatedAt DESC", conn))
        {
            cmd.Parameters.AddWithValue("@cid", customerId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                orderIds.Add(reader.GetInt32(0));
        }

        // Step 2: N+1 — one query per order to get line item totals
        // Before migration this was a single JOIN; now it's a loop.
        var summaries = new List<OrderSummary>();
        foreach (var orderId in orderIds)
        {
            await using var cmd = new SqlCommand(@"
                SELECT o.Id, o.Status, o.CreatedAt, SUM(li.Quantity * li.UnitPrice) AS Total
                FROM Orders o
                JOIN LineItems li ON li.OrderId = o.Id
                WHERE o.Id = @oid
                GROUP BY o.Id, o.Status, o.CreatedAt", conn);

            cmd.Parameters.AddWithValue("@oid", orderId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                summaries.Add(new OrderSummary
                {
                    OrderId = reader.GetInt32(0),
                    Status = reader.GetString(1),
                    CreatedAt = reader.GetDateTime(2),
                    Total = reader.GetDecimal(3),
                });
            }
        }

        return summaries;
    }
}
