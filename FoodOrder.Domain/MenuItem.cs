namespace FoodOrder.Domain;
using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(VegItem), "veg" )]
[JsonDerivedType(typeof(NonVegItem), "nonveg")]

public abstract class MenuItem
{
    public int Id {get; set;}
    public string Name {get; set;} = string.Empty;
    public decimal Price {get; set;}

    public abstract String GetDescription();

}