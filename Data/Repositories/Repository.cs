using InvestmentManager.Data;
using InvestmentManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PortofolioManager.Infrastructure.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<TEntity> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
        }

        public async Task<TEntity?> GetByIdAsync<TId>(TId id) where TId : notnull
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<List<TEntity>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task AddAsync(TEntity entity)
        {
            //TODO Remover esse método assincrono
            await _dbSet.AddAsync(entity);
        }
       public void Add(TEntity entity)
        {
             _dbSet.Add(entity);
        }
        public void Update(TEntity entity)
        {
            _dbSet.Update(entity);
        }

        public void Remove(TEntity entity)
        {
            _dbSet.Remove(entity);
        }

        public IQueryable<TEntity> Query()
        {
            return _dbSet.AsQueryable();
        }

        public async Task SaveChangesAsync()
        {

            //Reniver esse metodo assincrono.
            await _context.SaveChangesAsync();
        }
              public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }

}
