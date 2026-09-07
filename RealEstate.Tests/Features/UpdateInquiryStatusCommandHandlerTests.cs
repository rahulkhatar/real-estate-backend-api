using AutoMapper;
using FluentAssertions;
using Moq;
using RealEstate.Application.Common.Mappings;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Inquiries.Commands;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;
using Xunit;

namespace RealEstate.Tests.Features;

public class UpdateInquiryStatusCommandHandlerTests
{
    private readonly Mock<IInquiryRepository> _inquiryRepo = new();
    private readonly IMapper _mapper;

    public UpdateInquiryStatusCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    private UpdateInquiryStatusCommandHandler CreateHandler() => new(_inquiryRepo.Object, _mapper);

    [Fact]
    public async Task Handle_ValidTransition_UpdatesStatus()
    {
        var inquiry = new Inquiry { Id = "i1", Status = InquiryStatus.New };
        _inquiryRepo.Setup(r => r.GetByIdAsync("i1", It.IsAny<CancellationToken>())).ReturnsAsync(inquiry);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateInquiryStatusCommand("i1", new UpdateInquiryStatusDto { Status = "Contacted" }), CancellationToken.None);

        result.Status.Should().Be(nameof(InquiryStatus.Contacted));
        _inquiryRepo.Verify(r => r.UpdateAsync(inquiry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownInquiry_ThrowsNotFound()
    {
        _inquiryRepo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Inquiry?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(new UpdateInquiryStatusCommand("missing", new UpdateInquiryStatusDto { Status = "Closed" }), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
