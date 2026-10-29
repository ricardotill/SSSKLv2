using MediatR;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Events;
using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;
using SSSKL.Contracts.Telemetry;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Events;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.Orders.Commands.CreateOrder;

internal sealed class CreateOrderHandler(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IApplicationUserRepository applicationUserRepository,
    IPurchaseNotifier purchaseNotifier,
    IUserNotifier userNotifier,
    IDomainEventDispatcher eventDispatcher,
    ILogger<CreateOrderHandler> logger) : IRequestHandler<CreateOrderCommand>
{
    public async Task Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = request.Order ?? throw new ArgumentNullException(nameof(request), "Order is required");

        // Batch-fetch products and users instead of one query per id
        var products = await productRepository.GetByIds(order.Products);
        if (products.Count != order.Products.Count)
            throw new NotFoundException("Product not found");

        var userIds = order.Users.Select(u => u.ToString()).ToList();
        var users = await applicationUserRepository.GetByIds(userIds);
        if (users.Count != userIds.Count)
            throw new NotFoundException("ApplicationUser not found");

        logger.LogInformation("Create order for {ProductsCount} products and {UsersCount} users", products.Count, users.Count);

        var orders = products
            .SelectMany(p => GenerateUserOrders(users, p, order.Amount, order.Split))
            .ToList();

        await orderRepository.CreateRange(orders);
        SssklTelemetry.RecordOrders(orders);

        // The orders are committed at this point. Everything below is best effort: a failure in
        // live updates, achievements or notifications must not report the order as failed,
        // because the kiosk would then retry and charge the users twice.
        await RunSideEffect("live purchase broadcast", () => NotifyPurchase(orders));

        foreach (var o in orders)
        {
            await eventDispatcher.DispatchAsync(new OrderPlacedEvent(o));
        }

        // Notify users if someone ordered on their behalf
        if (!string.IsNullOrEmpty(request.ActingUserId))
        {
            await RunSideEffect("on-behalf notifications", () => NotifyOnBehalf(request.ActingUserId, users, products));
        }
    }

    private static List<Order> GenerateUserOrders(IList<ApplicationUser> userList, Product p, int amount, bool goingDutch)
    {
        var paid = goingDutch ? decimal.Round(p.Price / userList.Count, 2, MidpointRounding.ToPositiveInfinity) : p.Price;
        var sharedAmount = goingDutch ? amount / userList.Count : amount;

        return userList.Select(u => new Order
        {
            User = u,
            Amount = sharedAmount,
            Paid = paid * amount,
            ProductNaam = p.Name,
            Product = p
        }).ToList();
    }

    private async Task RunSideEffect(string name, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            SssklTelemetry.RecordSideEffectFailure(name);
            logger.LogError(ex, "{SideEffect} failed after order was saved", name);
        }
    }

    private async Task NotifyPurchase(IEnumerable<Order> orders)
    {
        var date = DateTime.Now;
        foreach (var order in orders)
        {
            await purchaseNotifier.NotifyUserPurchaseAsync(new UserPurchaseEvent(
                order.User.FullName,
                order.ProductNaam,
                order.Amount,
                date
            ));
        }
    }

    private async Task NotifyOnBehalf(string actingUserId, IList<ApplicationUser> users, IList<Product> products)
    {
        var actingUser = await applicationUserRepository.GetById(actingUserId);
        var actingUserName = actingUser?.FullName ?? "Iemand";
        var productNames = string.Join(", ", products.Select(p => p.Name));

        foreach (var u in users.Where(u => u.Id != actingUserId))
        {
            await userNotifier.NotifyAsync(
                u.Id,
                "Nieuwe bestelling!",
                $"{actingUserName} heeft {productNames} voor je besteld!",
                "/orders/personal",
                sendPush: true);
        }
    }
}
