using PointsWallet.Domain.Models;
using PointsWallet.Domain.Repositories;

namespace PointsWallet.Infrastructure.Repositories;

public class UserRepository(ApplicationDbContext context) 
    : Repository<User>(context), IUserRepository
{
}
