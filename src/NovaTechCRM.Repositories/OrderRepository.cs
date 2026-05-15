using Microsoft.EntityFrameworkCore;
using NovaTechCRM.Domain.Models;

namespace NovaTechCRM.Repositories;

public class NovaTechDbContext : DbContext
{
    public NovaTechDbContext(DbContextOptions<NovaTechDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(i => i.UnitPrice)
            .HasPrecision(18, 2);
    }
}

public class OrderRepository : IOrderRepository
{
    private readonly NovaTechDbContext _db;

    public OrderRepository(NovaTechDbContext db)
    {
        _db = db;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerId, CancellationToken ct = default)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task SaveAsync(Order order, CancellationToken ct = default)
    {
        var existing = await _db.Orders.FindAsync(new object[] { order.Id }, ct);
        if (existing is null)
            _db.Orders.Add(order);
        else
            _db.Entry(existing).CurrentValues.SetValues(order);

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var order = await _db.Orders.FindAsync(new object[] { id }, ct);
        if (order is not null)
        {
            _db.Orders.Remove(order);
            await _db.SaveChangesAsync(ct);
        }
    }
}
