using FoodOrder.Domain;

namespace FoodOrder.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _repository;
    private int? _nextId;

    public OrderService(IRepository<Order> repository)
    {
        _repository = repository;
    }

    public async Task<Order> PlaceOrderAsync(Cart cart, IDiscount? discount = null, string customerName = "")
    {
        if (cart.Items.Count == 0)
        {
            throw new EmptyCartException();
        }

        decimal total = cart.GetTotal();
        if (discount != null)
        {
            total = discount.Apply(total);
        }

        if (_nextId == null)
        {
            var existing = await _repository.GetAllAsync();
            _nextId = existing.Count == 0 ? 1 : existing.Max(o => o.Id) + 1;
        }

        var order = new Order
        {
            Id = _nextId.Value,
            Items = cart.Items.ToList(),
            Total = total,
            CustomerName = customerName
        };
        _nextId++;

        await _repository.AddAsync(order);
        return order;
    }

    public Task<IReadOnlyList<Order>> GetAllOrdersAsync() => _repository.GetAllAsync();
}