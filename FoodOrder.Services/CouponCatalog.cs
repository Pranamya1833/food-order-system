using FoodOrder.Domain;

namespace FoodOrder.Services;

public static class CouponCatalog
{
    private static readonly Dictionary<string, IDiscount> Coupons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SAVE10"] = new PercentageDiscount(10),
        ["FLAT50"] = new FlatDiscount(50),
        ["BIG100"] = new ThresholdDiscount(500, 100)
    };

    public static IDiscount? Find(string code) => Coupons.GetValueOrDefault(code.Trim());
}