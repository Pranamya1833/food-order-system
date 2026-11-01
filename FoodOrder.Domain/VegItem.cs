namespace FoodOrder.Domain;

public class VegItem : MenuItem
{
    public override string GetDescription() => $"{Name} (Veg) - Rs. {Price}";
}