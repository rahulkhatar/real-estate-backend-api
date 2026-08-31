using FluentAssertions;
using RealEstate.Infrastructure.Identity;
using Xunit;

namespace RealEstate.Tests.Identity;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Sup3rSecret!");

        _hasher.Verify("Sup3rSecret!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Sup3rSecret!");

        _hasher.Verify("WrongPassword", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_NeverReturnsThePlaintextPassword()
    {
        var hash = _hasher.Hash("Sup3rSecret!");

        hash.Should().NotBe("Sup3rSecret!");
        hash.Should().NotBeNullOrWhiteSpace();
    }
}
