using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FoodOrder.Data;
using FoodOrder.Domain;
using FoodOrder.Services;
using Microsoft.Extensions.DependencyInjection;
using MenuItem = FoodOrder.Domain.MenuItem;

namespace FoodOrder.Wpf;

// ---------- Display models for the lists ----------
public record MenuCard(int Id, string Emoji, string Name, string Price, string Kind, string DotColor, ImageSource? Photo);
public record CartRow(int Id, string Emoji, string Name, string Qty, string LineTotal);
public record OrderRow(string Emojis, string Title, string Items, string Total, string Status);
public record ReportRow(string Label, string Value, double Bar);

// ---------- Simple user accounts (passwords are salted and hashed) ----------
public class UserRecord
{
    public string Username { get; set; } = "";
    public string Salt { get; set; } = "";
    public string Hash { get; set; } = "";
}

public class UserStore
{
    private readonly string _path;
    private readonly List<UserRecord> _users;

    public UserStore(string path)
    {
        _path = path;
        _users = File.Exists(path)
            ? JsonSerializer.Deserialize<List<UserRecord>>(File.ReadAllText(path)) ?? new()
            : new();
    }

    // Returns an error message, or null when the account was created.
    public string? Register(string username, string password)
    {
        if (username.Length < 3) return "Username needs at least 3 characters.";
        if (password.Length < 6) return "Password needs at least 6 characters.";
        if (_users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            return "That username is already taken.";

        byte[] salt = RandomNumberGenerator.GetBytes(16);
        _users.Add(new UserRecord
        {
            Username = username,
            Salt = Convert.ToBase64String(salt),
            Hash = HashPassword(password, salt)
        });

        File.WriteAllText(_path, JsonSerializer.Serialize(_users, new JsonSerializerOptions { WriteIndented = true }));
        return null;
    }

    // Returns the stored username when the login is correct, otherwise null.
    public string? Login(string username, string password)
    {
        var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (user == null) return null;

        byte[] salt = Convert.FromBase64String(user.Salt);
        byte[] expected = Convert.FromBase64String(user.Hash);
        byte[] actual = Convert.FromBase64String(HashPassword(password, salt));

        return CryptographicOperations.FixedTimeEquals(expected, actual) ? user.Username : null;
    }

    private static string HashPassword(string password, byte[] salt) =>
        Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32));
}

public partial class MainWindow : Window
{
    private readonly List<MenuItem> _menu = new()
    {
        new VegItem { Id = 1, Name = "Paneer Tikka", Price = 250m },
        new NonVegItem { Id = 2, Name = "Chicken Biryani", Price = 280m, SpiceLevel = 3 },
        new VegItem { Id = 3, Name = "Masala Dosa", Price = 100m },
        new VegItem { Id = 4, Name = "Veg Burger", Price = 120m },
        new VegItem { Id = 5, Name = "Margherita Pizza", Price = 299m },
        new NonVegItem { Id = 6, Name = "Chicken Roll", Price = 160m, SpiceLevel = 2 },
        new VegItem { Id = 7, Name = "Gulab Jamun", Price = 80m },
        new NonVegItem { Id = 8, Name = "Fish Fry", Price = 320m, SpiceLevel = 2 },
        new VegItem { Id = 9, Name = "Veg Fried Rice", Price = 180m },
        new VegItem { Id = 10, Name = "Pasta Alfredo", Price = 220m },
        new VegItem { Id = 11, Name = "Mango Lassi", Price = 90m },
        new NonVegItem { Id = 12, Name = "Egg Curry", Price = 170m, SpiceLevel = 2 }
    };

    private readonly Dictionary<int, string> _emoji = new()
    {
        [1] = "\U0001F9C0", [2] = "\U0001F357", [3] = "\U0001F95E", [4] = "\U0001F354",
        [5] = "\U0001F355", [6] = "\U0001F32F", [7] = "\U0001F36E", [8] = "\U0001F41F",
        [9] = "\U0001F35A", [10] = "\U0001F35D", [11] = "\U0001F96D", [12] = "\U0001F95A"
    };

    private readonly Dictionary<string, IDiscount> _coupons = new()
    {
        ["SAVE10"] = new PercentageDiscount(10),
        ["FLAT50"] = new FlatDiscount(50),
        ["BIG100"] = new ThresholdDiscount(500, 100)
    };

    private readonly string[] _foodEmojis =
    {
        "\U0001F355", "\U0001F354", "\U0001F35C", "\U0001F363", "\U0001F32E",
        "\U0001F370", "\U0001F37F", "\U0001F95E", "\U0001F357", "\U0001F366"
    };

    private readonly Dictionary<int, ImageSource?> _images = new();
    private readonly Random _random = new();
    private readonly IOrderService _service;
    private readonly UserStore _users;
    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(2.5) };
    private readonly DispatcherTimer _overlayTimer = new() { Interval = TimeSpan.FromSeconds(3.5) };

    private Cart _cart = new();
    private IDiscount? _discount;
    private bool _registerMode;

    public MainWindow()
    {
        InitializeComponent();

        var collection = new ServiceCollection();
        collection.AddSingleton<IRepository<Order>>(_ => new EfOrderRepository(new OrderDbContext("order.db")));
        collection.AddSingleton<IOrderService, OrderService>();
        _service = collection.BuildServiceProvider().GetRequiredService<IOrderService>();

        _users = new UserStore(Path.Combine(AppContext.BaseDirectory, "users.json"));

        foreach (var dish in _menu)
        {
            _images[dish.Id] = LoadImage(dish.Id);
        }

        _toastTimer.Tick += (s, e) => HideToast();
        _overlayTimer.Tick += (s, e) => HideOverlay();

        CouponBox.KeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter) ApplyCoupon_Click(s, e);
        };
        UserBox.KeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter) AuthSubmit_Click(s, e);
        };
        PassBox.KeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter) AuthSubmit_Click(s, e);
        };

        Loaded += (s, e) => SpawnFalling(RainCanvas, 18, true, 7, 14, 0.3);

        Loaded += async (s, e) =>
        {
            await RefreshOrdersAsync();
            await RefreshReportsAsync();
        };

        ApplyFilter();
        RefreshCart();
    }

    // ================= Login and sign up =================

    private void AuthSubmit_Click(object sender, RoutedEventArgs e)
    {
        string username = UserBox.Text.Trim();
        string password = PassBox.Password;

        if (_registerMode)
        {
            string? error = _users.Register(username, password);
            if (error != null)
            {
                AuthMessage.Text = error;
                return;
            }
        }

        string? name = _users.Login(username, password);
        if (name == null)
        {
            AuthMessage.Text = "Wrong username or password.";
            return;
        }

        EnterApp(name);
    }

    private void ToggleMode_Click(object sender, RoutedEventArgs e)
    {
        _registerMode = !_registerMode;
        AuthMessage.Text = "";

        AuthTitle.Text = _registerMode ? "Create your account" : "Log in to start ordering";
        AuthSubmit.Content = _registerMode ? "Create account" : "Log in";
        ToggleModeButton.Content = _registerMode ? "Already have an account? Log in" : "New here? Create an account";
    }

    private void EnterApp(string name)
    {
        GreetingText.Text = $"Hi, {name}";
        UserBox.Clear();
        PassBox.Clear();
        AuthMessage.Text = "";

        LoginRoot.Visibility = Visibility.Collapsed;
        AppRoot.Visibility = Visibility.Visible;
        FadeIn(AppRoot, 0.4);

        NavMenu.IsChecked = true;
        ShowToast($"Welcome, {name}");
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        _cart = new Cart();
        _discount = null;
        CouponBox.Clear();
        CouponMessage.Text = "Try SAVE10, FLAT50 or BIG100";
        RefreshCart();

        AppRoot.Visibility = Visibility.Collapsed;
        LoginRoot.Visibility = Visibility.Visible;
        FadeIn(LoginRoot, 0.4);
    }

    // ================= Navigation =================

    private async void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;

        string page = (string)((RadioButton)sender).Tag;

        MenuPanel.Visibility = page == "Menu" ? Visibility.Visible : Visibility.Collapsed;
        OrdersPanel.Visibility = page == "Orders" ? Visibility.Visible : Visibility.Collapsed;
        ReportsPanel.Visibility = page == "Reports" ? Visibility.Visible : Visibility.Collapsed;

        await RefreshOrdersAsync();
        await RefreshReportsAsync();

        UIElement shown = page == "Menu" ? MenuPanel : page == "Orders" ? OrdersPanel : ReportsPanel;
        FadeIn(shown, 0.3);
    }

    // ================= Menu =================

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        string query = SearchBox.Text.Trim();
        bool vegOnly = ChipVeg.IsChecked == true;
        bool nonVegOnly = ChipNonVeg.IsChecked == true;

        var cards = _menu
            .Where(m => (!vegOnly || m is VegItem)
                     && (!nonVegOnly || m is NonVegItem)
                     && (query.Length == 0 || m.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .Select(m => new MenuCard(
                m.Id,
                _emoji.GetValueOrDefault(m.Id, "\U0001F37D"),
                m.Name,
                $"Rs. {m.Price:0.##}",
                m is VegItem ? "Veg" : "Non-veg",
                m is VegItem ? "#1E8E5A" : "#C0392B",
                _images.GetValueOrDefault(m.Id)))
            .ToList();

        MenuItemsView.ItemsSource = cards;
        NoResultsText.Visibility = cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // Looks for Images\<dish id>.jpg (or .jpeg / .png) next to the app. No file means the emoji stays.
    private static ImageSource? LoadImage(int id)
    {
        foreach (string ext in new[] { ".jpg", ".jpeg", ".png" })
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Images", id + ext);
            if (!File.Exists(path)) continue;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 420;
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        return null;
    }

    // ================= Cart =================

    private void Plus_Click(object sender, RoutedEventArgs e)
    {
        int id = (int)((Button)sender).Tag;
        var item = _menu.First(m => m.Id == id);

        _cart.Add(item);
        RefreshCart();
        PopBadge();
        ShowToast($"{item.Name} added to cart");
    }

    private void Minus_Click(object sender, RoutedEventArgs e)
    {
        int id = (int)((Button)sender).Tag;
        int index = _cart.Items.Select((m, i) => new { m.Id, Index = i }).Last(x => x.Id == id).Index;

        _cart.RemoveAt(index);
        RefreshCart();
    }

    private void ApplyCoupon_Click(object sender, RoutedEventArgs e)
    {
        string code = CouponBox.Text.Trim().ToUpper();
        _discount = _coupons.GetValueOrDefault(code);

        if (_discount == null)
            CouponMessage.Text = "That coupon code isn't valid.";
        else if (_discount.Apply(_cart.GetTotal()) == _cart.GetTotal())
            CouponMessage.Text = $"{code} added, but your cart doesn't qualify yet.";
        else
            CouponMessage.Text = $"{code} applied.";

        RefreshCart();
    }

    private async void PlaceOrder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var placed = await _service.PlaceOrderAsync(_cart, _discount);

            _cart = new Cart();
            _discount = null;
            CouponBox.Clear();
            CouponMessage.Text = "Try SAVE10, FLAT50 or BIG100";

            RefreshCart();
            await RefreshOrdersAsync();
            await RefreshReportsAsync();
            ShowOrderPlaced(placed);
        }
        catch (EmptyCartException ex)
        {
            ShowToast(ex.Message);
        }
    }

    private void RefreshCart()
    {
        var rows = _cart.Items
            .GroupBy(i => i.Id)
            .Select(g => new CartRow(
                g.Key,
                _emoji.GetValueOrDefault(g.Key, "\U0001F37D"),
                g.First().Name,
                g.Count().ToString(),
                $"Rs. {g.Sum(i => i.Price):0.##}"))
            .ToList();

        CartList.ItemsSource = rows;

        decimal itemTotal = _cart.GetTotal();
        decimal toPay = _discount?.Apply(itemTotal) ?? itemTotal;
        int count = _cart.Items.Count;

        ItemTotalText.Text = $"Rs. {itemTotal:0.##}";
        DiscountText.Text = $"- Rs. {itemTotal - toPay:0.##}";
        ToPayText.Text = $"Rs. {toPay:0.##}";

        CartTitle.Text = count == 0 ? "Your cart" : $"Your cart ({count} {(count == 1 ? "item" : "items")})";
        CartCountText.Text = count.ToString();
        EmptyCartPanel.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        PlaceOrderButton.IsEnabled = count > 0;
    }

    // ================= Orders and reports =================

    private async Task RefreshOrdersAsync()
    {
        var orders = await _service.GetAllOrdersAsync();
        var rows = orders
            .OrderByDescending(o => o.Id)
            .Select(o => new OrderRow(
                string.Concat(o.Items.Select(i => _emoji.GetValueOrDefault(i.Id, "")).Distinct().Take(3)),
                $"Order #{o.Id}",
                string.Join(", ", o.Items.Select(i => i.Name)),
                $"Rs. {o.Total:0.##}",
                o.Status.ToString()))
            .ToList();

        OrdersList.ItemsSource = rows;
        NoOrdersText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task RefreshReportsAsync()
    {
        var orders = await _service.GetAllOrdersAsync();
        decimal revenue = ReportService.TotalRevenue(orders);

        RevenueText.Text = $"Rs. {revenue:0.##}";
        OrderCountText.Text = orders.Count.ToString();
        AverageText.Text = orders.Count == 0 ? "Rs. 0" : $"Rs. {revenue / orders.Count:0.##}";

        var top = ReportService.TopSellingItems(orders).ToList();
        int maxCount = top.Count == 0 ? 1 : top.Max(x => x.Count);
        TopItemsList.ItemsSource = top
            .Select(x => new ReportRow(x.Name, $"{x.Count} sold", 260.0 * x.Count / maxCount))
            .ToList();

        var daily = ReportService.RevenuePerDay(orders).ToList();
        decimal maxRevenue = daily.Count == 0 ? 1 : daily.Max(x => x.Revenue);
        if (maxRevenue == 0) maxRevenue = 1;
        DailyList.ItemsSource = daily
            .Select(x => new ReportRow(x.Day.ToString("dd MMM yyyy"), $"Rs. {x.Revenue:0.##}", 260.0 * (double)(x.Revenue / maxRevenue)))
            .ToList();
    }

    // ================= Animations and feedback =================

    private static void FadeIn(UIElement element, double seconds)
    {
        element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(seconds)));
    }

    private void PopBadge()
    {
        var pop = new DoubleAnimation(1, 1.3, TimeSpan.FromMilliseconds(140)) { AutoReverse = true };
        BadgeScale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        BadgeScale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
    }

    private void ShowToast(string message)
    {
        ToastText.Text = message;
        Toast.Visibility = Visibility.Visible;

        Toast.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
        ToastShift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(24, 0, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });

        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void HideToast()
    {
        _toastTimer.Stop();

        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
        fade.Completed += (s, e) =>
        {
            if (!_toastTimer.IsEnabled) Toast.Visibility = Visibility.Collapsed;
        };
        Toast.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    private void ShowOrderPlaced(Order placed)
    {
        SuccessTitle.Text = $"Order #{placed.Id} placed";
        SuccessSub.Text = $"Total paid: Rs. {placed.Total:0.##}\nYou can follow it in the Orders tab.";

        SuccessOverlay.Visibility = Visibility.Visible;
        FadeIn(SuccessOverlay, 0.2);

        var pop = new DoubleAnimation(0.6, 1, TimeSpan.FromMilliseconds(380))
        {
            EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut }
        };
        SuccessScale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        SuccessScale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);

        SpawnFalling(ConfettiCanvas, 28, false, 1.6, 3.2, 1.0);

        _overlayTimer.Stop();
        _overlayTimer.Start();
    }

    private void Overlay_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => HideOverlay();

    private void HideOverlay()
    {
        _overlayTimer.Stop();
        SuccessOverlay.Visibility = Visibility.Collapsed;
    }

    // Food emojis drifting down a canvas: forever on the login screen, once as confetti.
    private void SpawnFalling(Canvas canvas, int count, bool forever, double minSeconds, double maxSeconds, double opacity)
    {
        canvas.Children.Clear();

        double width = Math.Max(canvas.ActualWidth, 900);
        double height = Math.Max(canvas.ActualHeight, 650);

        for (int i = 0; i < count; i++)
        {
            var emoji = new TextBlock
            {
                Text = _foodEmojis[_random.Next(_foodEmojis.Length)],
                FontFamily = new FontFamily("Segoe UI Emoji"),
                FontSize = _random.Next(24, 46),
                Opacity = opacity,
                RenderTransform = new RotateTransform(_random.Next(-25, 25))
            };

            Canvas.SetLeft(emoji, _random.NextDouble() * width);
            canvas.Children.Add(emoji);

            double seconds = minSeconds + _random.NextDouble() * (maxSeconds - minSeconds);
            var fall = new DoubleAnimation(-60, height + 20, TimeSpan.FromSeconds(seconds))
            {
                BeginTime = forever
                    ? TimeSpan.FromSeconds(-_random.NextDouble() * maxSeconds)
                    : TimeSpan.FromSeconds(_random.NextDouble() * 0.6),
                RepeatBehavior = forever ? RepeatBehavior.Forever : new RepeatBehavior(1)
            };
            emoji.BeginAnimation(Canvas.TopProperty, fall);
        }
    }
}