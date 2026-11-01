namespace FoodOrder.Domain;

public class NonVegItem : MenuItem
{
    public int SpiceLevel {get; set;} 
    public override string GetDescription() => $"{Name} (Nonveg) - Rs. {Price} Spice Level: {SpiceLevel}";
}