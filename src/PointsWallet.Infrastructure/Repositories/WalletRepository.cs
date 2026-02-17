using PointsWallet.Domain.Models;
using PointsWallet.Domain.Repositories;

namespace PointsWallet.Infrastructure.Repositories;

public class WalletRepository(PointsWalletDbContext context) 
    : Repository<Wallet>(context), IWalletRepository
{
}
