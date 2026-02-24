using PointsWallet.Domain.Models.Abstractions;

namespace PointsWallet.Domain.Models;

public class Wallet : Entity
{
    public long Points { get; private set; } = 0;
    public string UserId { get; private set; }
    public string? SymbolicName { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Wallet(string userId, string? symbolicName = null)
    {
        UserId = userId;
        SymbolicName = symbolicName;
        Id = Guid.NewGuid().ToString();
    }
}