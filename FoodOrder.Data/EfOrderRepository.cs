using FoodOrder.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Data;

public class EfOrderRepository : IRepository<Order>
{
    private readonly OrderDbContext _db;

    public EfOrderRepository(OrderDbContext db)
    {
        _db = db;
        _db.Database.EnsureCreated();
    }

    public async Task AddAsync(Order item)
    {
        _db.Orders.Add(item);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync() => await _db.Orders.ToListAsync();
}