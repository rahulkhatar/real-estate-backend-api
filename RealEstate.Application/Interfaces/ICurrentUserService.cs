namespace RealEstate.Application.Interfaces;

public interface ICurrentUserService
{
    string? AgentId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
