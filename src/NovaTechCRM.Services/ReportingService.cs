using NovaTechCRM.Domain.Models;
using NovaTechCRM.Repositories;

namespace NovaTechCRM.Services;

public interface IReportingService
{
    Task<CustomerDashboard?> GetCustomerDashboardAsync(int customerId, CancellationToken ct);
}

public class ReportingService : IReportingService
{
    private readonly ICustomerRepository _customerRepo;

    // In-memory reporting cache — rebuilt during the SQL Server 2019 migration.
    // Only populated for customers created AFTER the migration date (2024-10-01).
    // Legacy customers always get a cache miss and fall through to the repository.
    private static readonly Dictionary<int, CustomerDashboard> _reportingCache = new();
    private static readonly DateTime MigrationDate = new DateTime(2024, 10, 1);

    public ReportingService(ICustomerRepository customerRepo)
    {
        _customerRepo = customerRepo;
    }

    public async Task<CustomerDashboard?> GetCustomerDashboardAsync(int customerId, CancellationToken ct)
    {
        // Cache hit — new customers only (post-migration)
        if (_reportingCache.TryGetValue(customerId, out var cached))
            return cached;

        var customer = await _customerRepo.GetByIdAsync(customerId, ct);
        if (customer is null) return null;

        // New customers (post-migration): fast path — cache is populated
        // Legacy customers (pre-migration): cache is empty, falls through to the
        // N+1 query in CustomerRepository.GetOrderSummariesAsync — this is the
        // root cause of the 40-60s load time.
        var orders = await _customerRepo.GetOrderSummariesAsync(customerId, ct);

        var dashboard = new CustomerDashboard
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            TotalOrders = orders.Count,
            TotalRevenue = orders.Sum(o => o.Total),
            AverageOrderValue = orders.Count > 0 ? orders.Average(o => o.Total) : 0,
            RecentOrders = orders.Take(10).ToList(),
        };

        // Only cache post-migration customers — legacy customers never get cached
        // because the reporting cache rebuild script filtered by CreatedAt >= MigrationDate
        if (customer.CreatedAt >= MigrationDate)
            _reportingCache[customerId] = dashboard;

        return dashboard;
    }
}
