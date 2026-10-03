using SecureHR.Domain.Entities;

namespace SecureHR.Domain.Interfaces.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    }
}
