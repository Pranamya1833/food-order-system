namespace FoodOrder.Domain;

public interface IRepository<T>
{
    Task AddAsync(T item);
    Task<IReadOnlyList<T>> GetAllAsync();
}