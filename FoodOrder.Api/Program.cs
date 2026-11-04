using FoodOrder.Data;
using FoodOrder.Domain;
using FoodOrder.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IRepository<Order>>(_ => new EfOrderRepository(new OrderDbContext("order.db")));
builder.Services.AddSingleton<IOrderService, OrderService>();
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json"," Food Order API v1"));
app.MapGet("/api/menu", () =>
    MenuCatalog.Items.Select(m => new MenuItemResponse(m.Id, m.Name, m.Price, m is VegItem ? "Veg" : "Non-veg")));

app.MapGet("/api/orders", async (IOrderService service) =>
{
    var orders = await service.GetAllOrdersAsync();
    return orders.Select(ToResponse);
});

app.MapPost("/api/orders", async (PlaceOrderRequest request, IOrderService service) =>
{
    var cart = new Cart();

    foreach (int id in request.ItemIds)
    {
        var item = MenuCatalog.Items.FirstOrDefault(m => m.Id == id);
        if (item == null)
        {
            return Results.BadRequest($"No menu item with id {id}.");
        }
        cart.Add(item);
    }

    IDiscount? discount = null;
    if (!string.IsNullOrWhiteSpace(request.Coupon))
    {
        discount = CouponCatalog.Find(request.Coupon);
        if (discount == null)
        {
            return Results.BadRequest("That coupon code isn't valid.");
        }
    }

    try
    {
        var order = await service.PlaceOrderAsync(cart, discount);
        return Results.Created($"/api/orders/{order.Id}", ToResponse(order));
    }
    catch (EmptyCartException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.MapGet("/api/reports/summary", async (IOrderService service) =>
{
    var orders = await service.GetAllOrdersAsync();
    return new
    {
        OrderCount = orders.Count,
        Revenue = ReportService.TotalRevenue(orders),
        TopItems = ReportService.TopSellingItems(orders).Select(x => new { x.Name, x.Count })
    };
});

app.Run();

static OrderResponse ToResponse(Order o) =>
    new(o.Id, o.Status.ToString(), o.Total, o.CreatedAt, o.Items.Select(i => i.Name).ToList());

record MenuItemResponse(int Id, string Name, decimal Price, string Kind);
record PlaceOrderRequest(List<int> ItemIds, string? Coupon);
record OrderResponse(int Id, string Status, decimal Total, DateTime CreatedAt, List<string> Items);