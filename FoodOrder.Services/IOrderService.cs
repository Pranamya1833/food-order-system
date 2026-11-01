using FoodOrder.Domain;

namespace FoodOrder.Services;

public interface IOrderService
{
    Order PlaceOrder(Cart cart, IDiscount? discount = null);
    IReadOnlyList<Order>GetAllOrders();
}