using System.Security.Claims;
using FoodOrder.Data;
using FoodOrder.Domain;
using FoodOrder.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IRepository<Order>>(_ => new EfOrderRepository(new OrderDbContext("order.db")));
builder.Services.AddSingleton<IOrderService, OrderService>();
builder.Services.AddSingleton(new UserStore(builder.Configuration["Users:Path"] ?? "users.json"));
builder.Services.AddOpenApi();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = JwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = JwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtSettings.GetKey(builder.Configuration),
            ValidateLifetime = true,
            NameClaimType = "name"
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Food Order API v1"));
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/register", (RegisterRequest request, UserStore users) =>
{
    string username = request.Username?.Trim() ?? "";
    string? error = users.Register(username, request.Password ?? "");
    return error == null
        ? Results.Created("/api/auth/login", new { username })
        : Results.BadRequest(error);
});

app.MapPost("/api/auth/login", (LoginRequest request, UserStore users, IConfiguration config) =>
{
    string? name = users.Login(request.Username?.Trim() ?? "", request.Password ?? "");
    if (name == null)
    {
        return Results.Unauthorized();
    }

    var descriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[] { new Claim("name", name) }),
        Issuer = JwtSettings.Issuer,
        Audience = JwtSettings.Audience,
        Expires = DateTime.UtcNow.AddHours(2),
        SigningCredentials = new SigningCredentials(JwtSettings.GetKey(config), SecurityAlgorithms.HmacSha256)
    };

    string token = new JsonWebTokenHandler().CreateToken(descriptor);
    return Results.Ok(new { token });
});

app.MapGet("/api/menu", () =>
    MenuCatalog.Items.Select(m => new MenuItemResponse(m.Id, m.Name, m.Price, m is VegItem ? "Veg" : "Non-veg")));

app.MapGet("/api/orders", async (IOrderService service, ClaimsPrincipal user) =>
{
    string name = user.Identity?.Name ?? "";
    var orders = (await service.GetAllOrdersAsync()).Where(o => o.CustomerName == name);
    return orders.Select(ToResponse);
}).RequireAuthorization();

app.MapPost("/api/orders", async (PlaceOrderRequest request, IOrderService service, ClaimsPrincipal user) =>
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
        var order = await service.PlaceOrderAsync(cart, discount, user.Identity?.Name ?? "");
        return Results.Created($"/api/orders/{order.Id}", ToResponse(order));
    }
    catch (EmptyCartException ex)
    {
        return Results.BadRequest(ex.Message);
    }
}).RequireAuthorization();

app.MapGet("/api/reports/summary", async (IOrderService service, ClaimsPrincipal user) =>
{
    string name = user.Identity?.Name ?? "";
    var orders = (await service.GetAllOrdersAsync()).Where(o => o.CustomerName == name).ToList();
    return new
    {
        OrderCount = orders.Count,
        Revenue = ReportService.TotalRevenue(orders),
        TopItems = ReportService.TopSellingItems(orders).Select(x => new { x.Name, x.Count })
    };
}).RequireAuthorization();

app.Run();

static OrderResponse ToResponse(Order o) =>
    new(o.Id, o.Status.ToString(), o.Total, o.CreatedAt, o.Items.Select(i => i.Name).ToList());

record MenuItemResponse(int Id, string Name, decimal Price, string Kind);
record PlaceOrderRequest(List<int> ItemIds, string? Coupon);
record OrderResponse(int Id, string Status, decimal Total, DateTime CreatedAt, List<string> Items);
record RegisterRequest(string? Username, string? Password);
record LoginRequest(string? Username, string? Password);

public partial class Program { }