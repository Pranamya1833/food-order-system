using FoodOrder.Domain;

namespace FoodOrder.Services;

public static class MenuCatalog
{
    public static IReadOnlyList<MenuItem> Items { get; } = new List<MenuItem>
    {
        new VegItem { Id = 1, Name = "Paneer Tikka", Price = 250m },
        new NonVegItem { Id = 2, Name = "Chicken Biryani", Price = 280m, SpiceLevel = 3 },
        new VegItem { Id = 3, Name = "Masala Dosa", Price = 100m },
        new VegItem { Id = 4, Name = "Veg Burger", Price = 120m },
        new VegItem { Id = 5, Name = "Margherita Pizza", Price = 299m },
        new NonVegItem { Id = 6, Name = "Chicken Roll", Price = 160m, SpiceLevel = 2 },
        new VegItem { Id = 7, Name = "Gulab Jamun", Price = 80m },
        new NonVegItem { Id = 8, Name = "Fish Fry", Price = 320m, SpiceLevel = 2 },
        new VegItem { Id = 9, Name = "Veg Fried Rice", Price = 180m },
        new VegItem { Id = 10, Name = "Pasta Alfredo", Price = 220m },
        new VegItem { Id = 11, Name = "Mango Lassi", Price = 90m },
        new NonVegItem { Id = 12, Name = "Egg Curry", Price = 170m, SpiceLevel = 2 }
    };
}