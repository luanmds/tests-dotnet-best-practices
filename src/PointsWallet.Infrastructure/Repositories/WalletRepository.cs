using PointsWallet.Domain.Models;
using PointsWallet.Domain.Repositories;

namespace PointsWallet.Infrastructure.Repositories;

public class WalletRepository(ApplicationDbContext context) 
    : Repository<Wallet>(context), IWalletRepository
{
}
