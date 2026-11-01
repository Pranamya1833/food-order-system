using FoodOrder.Domain;

namespace FoodOrder.Services;

public static class ReportService
{
    public static decimal TotalRevenue(IEnumerable<Order> orders)
    => orders.Sum(o => o.Total);

    public static IEnumerable<(string Name, int Count)> TopSellingItems(IEnumerable<Order> orders)
    => orders.SelectMany(o => o.Items)
             .GroupBy(i => i.Name)
             .Select(g => (Name: g.Key, Count: g.Count()))
             .OrderByDescending( x => x.Count);

    public static IEnumerable<(DateTime Day, decimal Revenue)> RevenuePerDay(IEnumerable<Order> orders)
    => orders.GroupBy(o => o.CreatedAt.Date)
             .Select(g => (Day: g.Key, Revenue: g.Sum(o => o.Total)))
             .OrderBy(x => x.Day);
}
