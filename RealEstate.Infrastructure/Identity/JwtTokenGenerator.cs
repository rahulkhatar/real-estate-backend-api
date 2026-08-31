using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Entities;

namespace RealEstate.Infrastructure.Identity;

public class JwtTokenGenerator(IOptions<JwtSettings> options) : IJwtTokenGenerator
{
    private readonly JwtSettings _settings = options.Value;

    public string GenerateToken(Agent agent)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, agent.Id),
            new Claim(ClaimTypes.Email, agent.Email),
            new Claim("agentId", agent.Id),
            new Claim("phone", agent.Phone),
            new Claim(ClaimTypes.Role, agent.Role),
            new Claim("licenseNumber", agent.LicenseNumber)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_settings.ExpiryHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
