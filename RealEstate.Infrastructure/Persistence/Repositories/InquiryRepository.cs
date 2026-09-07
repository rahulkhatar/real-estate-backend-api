using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;

namespace RealEstate.Infrastructure.Persistence.Repositories;

public class InquiryRepository(IMongoDbContext context)
    : GenericRepository<Inquiry>(context, CollectionNames.Inquiries), IInquiryRepository
{
}
