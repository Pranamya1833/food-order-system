using System.Text.Json;
using FoodOrder.Domain;

namespace FoodOrder.Data;

public class JsonRepository<T> : IRepository<T>
{
    private readonly string _path;
    private readonly List<T> _items;

    public JsonRepository(string path)
    {
        _path = path;
        _items = File.Exists(path)
          ?JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path)) ?? new()
          : new();
    }

    public void Add(T item)
    {
        _items.Add(item);
        File.WriteAllText(_path, 
        JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true}));
    } 
    public IReadOnlyList<T> GetAll() => _items;
} 