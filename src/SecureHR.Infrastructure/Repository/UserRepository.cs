using Microsoft.EntityFrameworkCore;
using SecureHR.Domain.Entities;
using SecureHR.Domain.Interfaces.Repositories;
using SecureHR.Infrastructure.Data;

namespace SecureHR.Infrastructure.Repository
{
    public class UserRepository(SecureHRDbContext context) : GenericRepository<User>(context), IUserRepository
    {
        public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        {
            return DbSet.FirstOrDefaultAsync(u => u.Username == username, ct);
        }
    }
}
