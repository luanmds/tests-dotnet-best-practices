using PointsWallet.Domain.Models.Abstractions;

namespace PointsWallet.Domain.Models;

public class Wallet : Entity
{
    public long Points { get; private set; } = 0;
    public string UserId { get; private set; }
    public string? SymbolicName { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    private Wallet() { UserId = string.Empty; }

    public Wallet(string userId, string? symbolicName = null)
    {
        UserId = userId;
        SymbolicName = symbolicName;
        Id = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Adds points to the wallet balance.
    /// </summary>
    /// <param name="points">The number of points to add. Must be greater than zero.</param>
    /// <exception cref="ArgumentException">Thrown when points is less than or equal to zero.</exception>
    public void AddPoints(long points)
    {
        if (points <= 0)
            throw new ArgumentException("Points must be greater than zero", nameof(points));

        Points += points;
        UpdatedAt = DateTime.UtcNow;
    }
}