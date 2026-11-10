namespace FoodOrder.Domain;

public class Order
{
    public int Id { get; set; }
    public List<MenuItem> Items { get; set; } = new();
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Placed;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CustomerName { get; set; } = "";

    public void MoveToNextStatus()
    {
        if (Status == OrderStatus.Placed)
        {
            Status = OrderStatus.Preparing;
        }
        else if (Status == OrderStatus.Preparing)
        {
            Status = OrderStatus.Delivered;
        }
        else
        {
            throw new InvalidOrderStateException("Order is already delivered. Cannot move to next status.");
        }
    }
}