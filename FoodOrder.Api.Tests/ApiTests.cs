using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
        });
    }
}

public class ApiTests
{
    [Fact]
    public async Task GetMenu_ReturnsTwelveItems()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/menu");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.Equal(12, items!.Count);
    }

    [Fact]
    public async Task PostOrder_WithCoupon_ReturnsCreatedWithDiscountedTotal()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", new { itemIds = new[] { 1, 2 }, coupon = "SAVE10" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(477m, order.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task PostOrder_UnknownItem_ReturnsBadRequest()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", new { itemIds = new[] { 99 } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("99", body);
    }

    [Fact]
    public async Task PostOrder_InvalidCoupon_ReturnsBadRequest()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", new { itemIds = new[] { 1 }, coupon = "NOPE" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostOrder_EmptyCart_ReturnsBadRequest()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", new { itemIds = Array.Empty<int>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("empty", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Summary_AfterOneOrder_ShowsOneOrder()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/orders", new { itemIds = new[] { 1, 2 } });
        var summary = await client.GetFromJsonAsync<JsonElement>("/api/reports/summary");

        Assert.Equal(1, summary.GetProperty("orderCount").GetInt32());
        Assert.Equal(530m, summary.GetProperty("revenue").GetDecimal());
    }
}