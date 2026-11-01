using FoodOrder.Domain;
using FoodOrder.Services;
using FoodOrder.Data;
using Microsoft.Extensions.DependencyInjection;

List<MenuItem> menu = new()
{
    new VegItem { Id = 1, Name = "Paneer Tikka", Price = 250m },
    new NonVegItem { Id = 2, Name = "Chicken Biryani", Price = 280m, SpiceLevel = 3 },
    new VegItem{ Id = 3, Name = "Dosa", Price = 100m}
};

var collection = new ServiceCollection();
collection.AddSingleton<IRepository<Order>>(_ => new EfOrderRepository(new OrderDbContext("orders.db")));
collection.AddSingleton<IOrderService, OrderService>();
var provider = collection.BuildServiceProvider();
var service = provider.GetRequiredService<IOrderService>();

var cart = new Cart();
bool running = true;
while(running)
{
    Console.WriteLine();
    Console.WriteLine("1. View menu");
    Console.WriteLine("2. Add item to the cart");
    Console.WriteLine("3. Place order");
    Console.WriteLine("4. View order");
    Console.WriteLine("5. Reports");
    Console.WriteLine("0. Exit");
    Console.WriteLine("Choose:");

    string? choice = Console.ReadLine();

    switch(choice)
    {
        case "1":
          foreach(var item in menu)
            {
                Console.WriteLine($"{item.Id}. {item.   GetDescription()}");
            }
            break;
    
        case "2":
           Console.Write("Item id:");
           if(int.TryParse(Console.ReadLine(), out int id))
        {
            var found = menu.FirstOrDefault(m => m.Id == id);
            if (found != null)
                {
                    cart.Add(found);
                    Console.WriteLine($"Added {found.Name}.");
                }
                else
                {
                    Console.WriteLine("No such item.");
                }
        }
        else
            {
                Console.WriteLine("Please type a number.");
            }
        break;
        case "3":
          try
            {
                var placed = service.PlaceOrder(cart);
                Console.WriteLine($"Order {placed.Id} placed: Rs. {placed.Total}");
                cart = new Cart();
            }
            catch (EmptyCartException ex)
            {
                Console.WriteLine(ex.Message);
            }
            break;
        case "4":
           foreach (var o in service.GetAllOrders())
            {
                Console.WriteLine($"Order {o.Id} | {o.Status} | Rs. {o.Total}");
            }
            break;
        case "5":
            var orders = service.GetAllOrders();
            Console.WriteLine($"Revenue : Rs. {ReportService.TotalRevenue(orders)}");
            foreach( var (name, count) in ReportService.TopSellingItems(orders))
            {
                Console.WriteLine($"{name}: {count} sold");
            }
            break;
        case "0":
            running = false;
            break;
        default:
            Console.WriteLine("Invalid Choice");
            break;
}
}


