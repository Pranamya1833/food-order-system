namespace FoodOrder.Domain;

public class EmptyCartException : Exception
{
    public EmptyCartException() : base("Cart is empty.")
    {
    }
}