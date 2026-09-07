using FluentAssertions;
using Moq;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Agents;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Entities;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;
using Xunit;

namespace RealEstate.Tests.Features;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IAgentRepository> _agentRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    private ChangePasswordCommandHandler CreateHandler() => new(_agentRepo.Object, _passwordHasher.Object);

    private static ChangePasswordDto Dto() => new() { CurrentPassword = "oldPass123", NewPassword = "newPass456" };

    [Fact]
    public async Task Handle_CorrectCurrentPassword_HashesAndSavesNewPassword()
    {
        var agent = new Agent { Id = "a1", PasswordHash = "old-hash" };
        _agentRepo.Setup(r => r.GetByIdAsync("a1", It.IsAny<CancellationToken>())).ReturnsAsync(agent);
        _passwordHasher.Setup(h => h.Verify("oldPass123", "old-hash")).Returns(true);
        _passwordHasher.Setup(h => h.Hash("newPass456")).Returns("new-hash");

        var handler = CreateHandler();
        await handler.Handle(new ChangePasswordCommand("a1", Dto()), CancellationToken.None);

        agent.PasswordHash.Should().Be("new-hash");
        _agentRepo.Verify(r => r.UpdateAsync(agent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ThrowsUnauthorized_AndDoesNotSave()
    {
        var agent = new Agent { Id = "a1", PasswordHash = "old-hash" };
        _agentRepo.Setup(r => r.GetByIdAsync("a1", It.IsAny<CancellationToken>())).ReturnsAsync(agent);
        _passwordHasher.Setup(h => h.Verify("oldPass123", "old-hash")).Returns(false);

        var handler = CreateHandler();
        var act = () => handler.Handle(new ChangePasswordCommand("a1", Dto()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAppException>();
        _agentRepo.Verify(r => r.UpdateAsync(It.IsAny<Agent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownAgent_ThrowsNotFound()
    {
        _agentRepo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Agent?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(new ChangePasswordCommand("missing", Dto()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
