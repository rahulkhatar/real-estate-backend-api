using RealEstate.Core.Entities;

namespace RealEstate.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(Agent agent);
}
