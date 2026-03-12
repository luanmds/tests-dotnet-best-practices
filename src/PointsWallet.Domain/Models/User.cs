using PointsWallet.Domain.Models.Abstractions;

namespace PointsWallet.Domain.Models;

public class User : Entity
{
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    public User(string name, string email)
    {
        Name = name;
        Email = email;
        Id = Guid.NewGuid().ToString();
    }   
}