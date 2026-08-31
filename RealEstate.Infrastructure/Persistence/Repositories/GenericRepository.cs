using MongoDB.Driver;
using MongoDB.Driver.Linq;
using RealEstate.Core.Common;
using RealEstate.Core.Interfaces;
using RealEstate.Core.Specifications;
using RealEstate.Infrastructure.Persistence.Specifications;

namespace RealEstate.Infrastructure.Persistence.Repositories;

public class GenericRepository<T>(IMongoDbContext context, string collectionName) : IRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> Collection = context.GetCollection<T>(collectionName);

    public async Task<T?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await Collection.Find(x => x.Id == id && !x.IsDeleted).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<T>> ListAllAsync(CancellationToken ct = default) =>
        await Collection.Find(x => !x.IsDeleted).ToListAsync(ct);

    public async Task<T?> GetBySpecAsync(ISpecification<T> spec, CancellationToken ct = default) =>
        await SpecificationEvaluator<T>.GetQuery(Collection, spec).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken ct = default) =>
        await SpecificationEvaluator<T>.GetQuery(Collection, spec).ToListAsync(ct);

    public async Task<PagedResult<T>> ListPagedAsync(ISpecification<T> spec, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var totalCount = await SpecificationEvaluator<T>.GetCountQuery(Collection, spec).LongCountAsync(ct);
        var items = await SpecificationEvaluator<T>.GetQuery(Collection, spec).ToListAsync(ct);
        return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<long> CountAsync(ISpecification<T> spec, CancellationToken ct = default) =>
        await SpecificationEvaluator<T>.GetCountQuery(Collection, spec).LongCountAsync(ct);

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await Collection.InsertOneAsync(entity, cancellationToken: ct);
        return entity;
    }

    public async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        await Collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var update = Builders<T>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        await Collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
    }
}
