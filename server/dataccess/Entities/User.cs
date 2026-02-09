namespace dataccess.Entities;

public class User
{
    public string Id { get; set; } = string.Empty;      // Guid as string
    public string Nickname { get; set; } = string.Empty; // username
    public string Hash { get; set; } = string.Empty;     // base64 sha256
    public string Salt { get; set; } = string.Empty;     // random guid string
    public string Role { get; set; } = "User";           // "User" / "Admin"
}