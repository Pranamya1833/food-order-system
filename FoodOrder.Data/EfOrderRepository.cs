using FoodOrder.Domain;

namespace FoodOrder.Data;

public class EfOrderRepository : IRepository<Order>
{
    private readonly OrderDbContext _db;

    public EfOrderRepository(OrderDbContext db)
    {
        _db = db;
        _db.Database.EnsureCreated();
    }

    public void Add(Order item)
    {
        _db.Orders.Add(item);
        _db.SaveChanges();
    }

    public IReadOnlyList<Order> GetAll() => _db.Orders.ToList();
}