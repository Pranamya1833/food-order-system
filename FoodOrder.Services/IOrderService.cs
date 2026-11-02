using FoodOrder.Domain;

namespace FoodOrder.Services;

public interface IOrderService
{
    Task<Order> PlaceOrderAsync(Cart cart, IDiscount? discount = null);
    Task<IReadOnlyList<Order>> GetAllOrdersAsync();
}