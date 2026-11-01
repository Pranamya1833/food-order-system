namespace FoodOrder.Domain;

public class FlatDiscount : IDiscount
{
    private readonly decimal _amount;
    public FlatDiscount(decimal amount)
    {
        _amount = amount;
    }
    public decimal Apply(decimal total)
    {
        return Math.Max(0, total - _amount);
    }
}