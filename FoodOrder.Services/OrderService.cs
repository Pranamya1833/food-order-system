using FoodOrder.Domain;

namespace FoodOrder.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _repository;
    private int _nextId;
    public OrderService(IRepository<Order> repository)
    {
        _repository = repository;
        _nextId = repository.GetAll().Count + 1;
    }
    public Order PlaceOrder(Cart cart, IDiscount? discount = null)
    {
        if(cart.Items.Count == 0)
        {
            throw new EmptyCartException();
        }
        decimal total = cart.GetTotal();
        
        if(discount!=null)
        {
            total = discount.Apply(total);
        }

        var order = new Order
        {
            Id = _nextId++,
            Items = cart.Items.ToList(),
            Total = total
        };

        _repository.Add(order);
        return order;

    }
    public IReadOnlyList<Order> GetAllOrders() => _repository.GetAll();
}