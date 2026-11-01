namespace FoodOrder.Domain;

public class ThresholdDiscount : IDiscount
{
    private readonly decimal _minimum;
    private readonly decimal _amount;
    
    public ThresholdDiscount(decimal minimum, decimal amount)
    {
        _minimum = minimum;
        _amount = amount;
    }

    public decimal Apply(decimal total)
    {
        if (total>= _minimum)
        {
            return total - _amount;
        }
        return total;
    }
}