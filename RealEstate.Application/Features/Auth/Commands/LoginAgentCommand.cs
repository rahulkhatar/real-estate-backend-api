using AutoMapper;
using FluentValidation;
using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Auth.Commands;

public record LoginAgentCommand(LoginDto Dto) : IRequest<AuthResponseDto>;

public class LoginAgentCommandValidator : AbstractValidator<LoginAgentCommand>
{
    public LoginAgentCommandValidator()
    {
        RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Dto.Password).NotEmpty();
    }
}

public class LoginAgentCommandHandler(
    IAgentRepository repository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IMapper mapper) : IRequestHandler<LoginAgentCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(LoginAgentCommand request, CancellationToken cancellationToken)
    {
        var agent = await repository.GetByEmailAsync(request.Dto.Email.Trim().ToLowerInvariant(), cancellationToken);

        if (agent is null || !passwordHasher.Verify(request.Dto.Password, agent.PasswordHash))
            throw new UnauthorizedAppException("Invalid email or password.");

        if (agent.Status != Core.Enums.AgentStatus.Active)
            throw new UnauthorizedAppException($"This account is {agent.Status.ToString().ToLower()}.");

        agent.LastLogin = DateTime.UtcNow;
        await repository.UpdateAsync(agent, cancellationToken);

        var token = jwtTokenGenerator.GenerateToken(agent);

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            Agent = mapper.Map<AgentDto>(agent)
        };
    }
}
