using RealEstate.Core.Common;
using RealEstate.Core.Specifications;

namespace RealEstate.Core.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAllAsync(CancellationToken ct = default);

    Task<T?> GetBySpecAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<PagedResult<T>> ListPagedAsync(ISpecification<T> spec, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<long> CountAsync(ISpecification<T> spec, CancellationToken ct = default);

    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);

    /// <summary>Soft-deletes by setting IsDeleted; the document is never physically removed.</summary>
    Task DeleteAsync(string id, CancellationToken ct = default);
}
