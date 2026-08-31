using System.ComponentModel.DataAnnotations;

namespace RealEstate.Application.DTOs;

public class RegisterAgentDto
{
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string LicenseNumber { get; set; } = string.Empty;
}

public class LoginDto
{
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public AgentDto Agent { get; set; } = new();
}
