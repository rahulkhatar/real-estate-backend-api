using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using RealEstate.Core.Entities;
using RealEstate.Infrastructure.Identity;
using Xunit;

namespace RealEstate.Tests.Identity;

public class JwtTokenGeneratorTests
{
    private readonly JwtTokenGenerator _generator = new(Options.Create(new JwtSettings
    {
        Secret = "test-secret-key-that-is-long-enough-for-hmacsha256-signing",
        Issuer = "RealEstate.Tests",
        Audience = "RealEstate.Tests.Client",
        ExpiryHours = 1
    }));

    [Fact]
    public void GenerateToken_IncludesExpectedClaims()
    {
        var agent = new Agent
        {
            Id = "abc123",
            Email = "agent@example.com",
            Phone = "1234567890",
            Role = "Agent",
            LicenseNumber = "LIC-001"
        };

        var token = _generator.GenerateToken(agent);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == "agentId" && c.Value == "abc123");
        jwt.Claims.Should().Contain(c => c.Value == "agent@example.com");
        jwt.Claims.Should().Contain(c => c.Value == "Agent");
        jwt.Issuer.Should().Be("RealEstate.Tests");
    }

    [Fact]
    public void GenerateToken_SetsExpiryInTheFuture()
    {
        var agent = new Agent { Id = "abc123", Email = "agent@example.com" };

        var token = _generator.GenerateToken(agent);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }
}
