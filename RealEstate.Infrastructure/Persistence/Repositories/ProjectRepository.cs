using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;

namespace RealEstate.Infrastructure.Persistence.Repositories;

public class ProjectRepository(IMongoDbContext context)
    : GenericRepository<Project>(context, CollectionNames.Projects), IProjectRepository
{
}
