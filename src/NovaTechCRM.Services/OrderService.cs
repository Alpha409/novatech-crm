using Microsoft.Extensions.Logging;
using NovaTechCRM.Domain.Models;
using NovaTechCRM.Repositories;

namespace NovaTechCRM.Services;

public class OrderService
{
    private readonly IOrderRepository _orderRepo;
    private readonly IFraudShieldService _fraudShield;
    private readonly INotificationService _notifications;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepo,
        IFraudShieldService fraudShield,
        INotificationService notifications,
        ILogger<OrderService> logger)
    {
        _orderRepo = orderRepo;
        _fraudShield = fraudShield;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<Order> PlaceOrderAsync(Order order, CancellationToken ct = default)
    {
        order.Status = OrderStatus.FraudCheckPending;
        await _orderRepo.SaveAsync(order, ct);

        // BUG (NOVA-47): FraudShield check is fired without awaiting the result.
        // The _ discard means we never inspect whether the check passed or failed.
        // FulfillOrderAsync runs immediately after, racing against the fraud check.
        // On fast machines this usually works, but under any I/O latency the order
        // gets fulfilled before FraudShield responds, bypassing fraud controls entirely.
        _ = _fraudShield.CheckAsync(order, ct);

        await FulfillOrderAsync(order, ct);

        return order;
    }

    private async Task FulfillOrderAsync(Order order, CancellationToken ct = default)
    {
        // This runs without knowing the fraud check outcome — the race condition.
        order.Status = OrderStatus.Fulfilled;
        order.FulfilledAt = DateTime.UtcNow;
        await _orderRepo.SaveAsync(order, ct);

        await _notifications.SendOrderConfirmationAsync(order, ct);

        _logger.LogInformation("Order {OrderId} fulfilled for customer {CustomerId} (amount: {Amount:C})",
            order.Id, order.CustomerId, order.TotalAmount);
    }

    public async Task<Order?> GetOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        return await _orderRepo.GetByIdAsync(orderId, ct);
    }

    public async Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(string customerId, CancellationToken ct = default)
    {
        return await _orderRepo.GetByCustomerAsync(customerId, ct);
    }
}
