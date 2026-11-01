namespace FoodOrder.Domain;

public interface IRepository<T>
{
    void Add(T item);
    IReadOnlyList<T> GetAll();
}
