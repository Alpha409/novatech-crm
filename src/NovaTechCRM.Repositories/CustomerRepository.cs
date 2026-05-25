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

    public async Task<List<OrderSummary>> GetOrderSummariesAsync(int customerId, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new SqlCommand(@"
            SELECT o.Id, o.Status, o.CreatedAt, SUM(li.Quantity * li.UnitPrice) AS Total
            FROM Orders o
            JOIN LineItems li ON li.OrderId = o.Id
            WHERE o.CustomerId = @cid
            GROUP BY o.Id, o.Status, o.CreatedAt
            ORDER BY o.CreatedAt DESC", conn);

        cmd.Parameters.AddWithValue("@cid", customerId);

        var summaries = new List<OrderSummary>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            summaries.Add(new OrderSummary
            {
                OrderId = reader.GetInt32(0),
                Status = reader.GetString(1),
                CreatedAt = reader.GetDateTime(2),
                Total = reader.GetDecimal(3),
            });
        }

        return summaries;
    }
}
