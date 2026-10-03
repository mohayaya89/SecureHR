using Microsoft.EntityFrameworkCore;
using SecureHR.Domain.Interfaces.Repositories;

namespace SecureHR.Infrastructure.Repository
{
    public class GenericRepository<TEntity>(DbContext context) : IGenericRepository<TEntity> where TEntity : class
    {
        protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

        public async Task AddAsync(TEntity entity, CancellationToken ct = default)
        {
            await DbSet.AddAsync(entity, ct);
        }

        public void Delete(TEntity entity)
        {
            DbSet.Remove(entity);
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            await context.SaveChangesAsync(ct);
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default)
        {
            return await DbSet
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        {
             return await DbSet.FindAsync(id, ct);
        }

        public void Update(TEntity entity)
        {
            DbSet.Update(entity);
        }
    }
}
