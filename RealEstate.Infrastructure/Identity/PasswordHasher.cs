using BCrypt.Net;
using RealEstate.Application.Interfaces;

namespace RealEstate.Infrastructure.Identity;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.EnhancedHashPassword(password, HashType.SHA384, workFactor: 12);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.EnhancedVerify(password, hash, HashType.SHA384);
}
