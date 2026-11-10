using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace FoodOrder.Data;

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