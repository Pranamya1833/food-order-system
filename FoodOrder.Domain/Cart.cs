namespace FoodOrder.Domain;


public class Cart
{
    private readonly List<MenuItem> _items = new();
     public void Add(MenuItem item)
    {
        _items.Add(item);
    }
    public Decimal GetTotal()
    {
     return _items.Sum(item=> item.Price);
    }
public IReadOnlyList<MenuItem> Items => _items;
public void RemoveAt(int index) => _items.RemoveAt(index);
}

