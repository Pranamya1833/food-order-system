using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodOrder.Data;
using FoodOrder.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace FoodOrder.Api.Tests;

public class InMemoryRepository : IRepository<Order>
{
    private readonly List<Order> _items = new();

    public Task AddAsync(Order item)
    {
        _items.Add(item);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Order>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<Order>>(_items.ToList());
}

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRepository<Order>>();
            services.AddSingleton<IRepository<Order>>(new InMemoryRepository());

            services.RemoveAll<UserStore>();
            services.AddSingleton(new UserStore(Path.Combine(Path.GetTempPath(), $"users-{Guid.NewGuid()}.json")));
        });
    }
}

public class ApiTests
{
    private static async Task<HttpClient> LoggedInClientAsync(ApiFactory factory)
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new { username = "tester", password = "secret123" });

        var login = await client.PostAsJsonAsync("/api/auth/login", new { username = "tester", password = "secret123" });
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());

        return client;
    }

    [Fact]
    public async Task GetMenu_IsPublicAndReturnsTwelveItems()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/menu");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.Equal(12, items!.Count);
    }

    [Fact]
    public async Task GetOrders_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new { username = "tester", password = "secret123" });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "tester", password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new { username = "tester", password = "123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostOrder_WithCoupon_ReturnsCreatedWithDiscountedTotal()
    {
        using var factory = new ApiFactory();
        var client = await LoggedInClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/orders", new { itemIds = new[] { 1, 2 }, coupon = "SAVE10" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(477m, order.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task PostOrder_UnknownItem_ReturnsBadRequest()
    {
        using var factory = new ApiFactory();
        var client = await LoggedInClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/orders", new { itemIds = new[] { 99 } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("99", body);
    }

    [Fact]
    public async Task PostOrder_InvalidCoupon_ReturnsBadRequest()
    {
        using var factory = new ApiFactory();
        var client = await LoggedInClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/orders", new { itemIds = new[] { 1 }, coupon = "NOPE" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostOrder_EmptyCart_ReturnsBadRequest()
    {
        using var factory = new ApiFactory();
        var client = await LoggedInClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/orders", new { itemIds = Array.Empty<int>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("empty", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Summary_AfterOneOrder_ShowsOneOrder()
    {
        using var factory = new ApiFactory();
        var client = await LoggedInClientAsync(factory);

        await client.PostAsJsonAsync("/api/orders", new { itemIds = new[] { 1, 2 } });
        var summary = await client.GetFromJsonAsync<JsonElement>("/api/reports/summary");

        Assert.Equal(1, summary.GetProperty("orderCount").GetInt32());
        Assert.Equal(530m, summary.GetProperty("revenue").GetDecimal());
    }
}