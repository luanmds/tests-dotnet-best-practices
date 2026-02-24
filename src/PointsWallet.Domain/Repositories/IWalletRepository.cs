using PointsWallet.Domain.Models;

namespace PointsWallet.Domain.Repositories;

public interface IWalletRepository : IRepository<Wallet, string>
{
	Task<Wallet?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
