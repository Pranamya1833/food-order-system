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
            ? JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path)) ?? new()
            : new();
    }

    public async Task AddAsync(T item)
    {
        _items.Add(item);
        string json = JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_path, json);
    }

    public Task<IReadOnlyList<T>> GetAllAsync() => Task.FromResult<IReadOnlyList<T>>(_items);
}