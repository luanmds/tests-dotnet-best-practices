using PointsWallet.Domain.Models;
using PointsWallet.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace PointsWallet.Infrastructure.Repositories;

public class WalletRepository(PointsWalletDbContext context) 
    : Repository<Wallet>(context), IWalletRepository
{
    public async Task<Wallet?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(wallet => wallet.UserId == userId, cancellationToken);
    }
}
