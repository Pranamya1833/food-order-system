using System.Text.Json;
using FoodOrder.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Data;

public class OrderDbContext : DbContext
{
    private readonly string _path;

    public OrderDbContext(string path) => _path = path;

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={_path}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .Property(o => o.Items)
            .HasConversion(
                items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<MenuItem>>(json, (JsonSerializerOptions?)null)
                        ?? new List<MenuItem>());
    }
}