
namespace InvestmentManager.Data.Repositories.Interfaces
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<TEntity?> GetByIdAsync<TId>(TId id) where TId : notnull;
        Task<List<TEntity>> GetAllAsync();
        Task AddAsync(TEntity entity);
        void Update(TEntity entity);
        void Remove(TEntity entity);
        IQueryable<TEntity> Query();
        Task SaveChangesAsync();
        void SaveChanges();
        void Add(TEntity entity);
    }

}
