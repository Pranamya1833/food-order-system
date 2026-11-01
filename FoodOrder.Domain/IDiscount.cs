namespace FoodOrder.Domain;

public interface IDiscount
{
    decimal Apply(decimal total);
}