namespace FoodOrder.Domain;

public class PercentageDiscount : IDiscount
{
    private readonly decimal _percent;
    public PercentageDiscount(decimal percent)
    {
        _percent = percent;
    }
    public decimal Apply(decimal total)
    {
        return total - (total * _percent/100);
    }
}