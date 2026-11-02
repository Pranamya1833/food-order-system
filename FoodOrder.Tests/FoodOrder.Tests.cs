using FoodOrder.Domain;
using FoodOrder.Services;
using Xunit;

namespace FoodOrder.Tests;

public class DiscountTests
{
    [Fact]
    public void Percentage_TenPercentof530_Returns477()
    {
        var Discount = new PercentageDiscount(10);
        Assert.Equal(477m, Discount.Apply(530m));
    }

    [Fact]
    public void Flat_BiggerThanTotal_NeverGoesBelowZero()
    {
        var Discount = new FlatDiscount(50);
        Assert.Equal(0m, Discount.Apply(30m));
    }

    [Fact]
    public void Threshold_BeloMinimum_ReturnsTotalUnchanged()
    {
        var Discount = new ThresholdDiscount(600, 100);
        Assert.Equal(530m, Discount.Apply(530m));
    }

    [Fact]
    public void Threshold_ExactlyAtMinimum_AppliesDiscount()
    {
        var Discount = new ThresholdDiscount(500, 100);
        Assert.Equal(400m, Discount.Apply(500m));
    }

}
public class CartAndOrderTests
{
    [Fact]
    public void Cart_GetTotal_AddsAllPrices()
    {
        var cart = new Cart();
        cart.Add(new VegItem { Name = "Dosa" , Price = 100m});
        cart.Add(new VegItem { Name = "Idli" , Price = 60m});
        Assert.Equal(160m, cart.GetTotal());

    }

    [Fact]
    public void Order_ThirdStatusMove_Throws()
    {
        var order = new Order();
        order.MoveToNextStatus();
        order.MoveToNextStatus();
        Assert.Throws<InvalidOrderStateException>(() => order.MoveToNextStatus());
    }
}

public class FakeRepository<T> : IRepository<T>
{
    private readonly List<T> _items = new();

    public Task AddAsync(T item)
    {
        _items.Add(item);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<T>> GetAllAsync() => Task.FromResult<IReadOnlyList<T>>(_items);
}

public class OrderServiceTests
{
    [Fact]
    public async Task PlaceOrder_EmptyCart_Throws()
    {
        var service = new OrderService(new FakeRepository<Order>());
        await Assert.ThrowsAsync<EmptyCartException>(() => service.PlaceOrderAsync(new Cart()));
    }

    [Fact]
    public async Task PlaceOrder_WithDiscount_StoresDiscountedTotal()
    {
        var service = new OrderService(new FakeRepository<Order>());
        var cart = new Cart();
        cart.Add(new VegItem { Name = "Paneer Tikka", Price = 200m });

        var order = await service.PlaceOrderAsync(cart, new PercentageDiscount(10));

        Assert.Equal(180m, order.Total);
    }

    [Fact]
    public async Task PlaceOrder_TwoOrders_GetIncreasingIds()
    {
        var service = new OrderService(new FakeRepository<Order>());
        var cart = new Cart();
        cart.Add(new VegItem { Name = "Dosa", Price = 100m });

        var first = await service.PlaceOrderAsync(cart);
        var second = await service.PlaceOrderAsync(cart);

        Assert.Equal(first.Id + 1, second.Id);
    }
}