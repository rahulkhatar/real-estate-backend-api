using AutoMapper;
using FluentValidation;
using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Entities;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Auth.Commands;

public record RegisterAgentCommand(RegisterAgentDto Dto) : IRequest<AuthResponseDto>;

public class RegisterAgentCommandValidator : AbstractValidator<RegisterAgentCommand>
{
    public RegisterAgentCommandValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Dto.Phone).NotEmpty();
        RuleFor(x => x.Dto.LicenseNumber).NotEmpty();
        RuleFor(x => x.Dto.Password)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.");
    }
}

public class RegisterAgentCommandHandler(
    IAgentRepository repository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IMapper mapper) : IRequestHandler<RegisterAgentCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(RegisterAgentCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        if (await repository.EmailExistsAsync(dto.Email, cancellationToken))
            throw new ConflictException($"An agent with email '{dto.Email}' already exists.");

        if (await repository.PhoneExistsAsync(dto.Phone, cancellationToken))
            throw new ConflictException($"An agent with phone '{dto.Phone}' already exists.");

        if (await repository.LicenseNumberExistsAsync(dto.LicenseNumber, cancellationToken))
            throw new ConflictException($"An agent with license number '{dto.LicenseNumber}' already exists.");

        var agent = new Agent
        {
            Name = dto.Name,
            Email = dto.Email.Trim().ToLowerInvariant(),
            Phone = dto.Phone,
            LicenseNumber = dto.LicenseNumber,
            PasswordHash = passwordHasher.Hash(dto.Password),
            Role = "Agent",
            CommissionPercentage = 2, // default rate; an Admin can adjust it per agent via PUT /api/agents/{id}/commission
        };

        var created = await repository.AddAsync(agent, cancellationToken);
        var token = jwtTokenGenerator.GenerateToken(created);

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            Agent = mapper.Map<AgentDto>(created)
        };
    }
}
