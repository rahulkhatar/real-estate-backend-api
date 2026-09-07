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
using RealEstate.Core.ValueObjects;
using Xunit;

namespace RealEstate.Tests.Features;

public class CreateInquiryCommandHandlerTests
{
    private readonly Mock<IInquiryRepository> _inquiryRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IPropertyRepository> _propertyRepo = new();
    private readonly Mock<IUnitRepository> _unitRepo = new();
    private readonly IMapper _mapper;

    public CreateInquiryCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _inquiryRepo.Setup(r => r.AddAsync(It.IsAny<Inquiry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Inquiry i, CancellationToken _) => i);
    }

    private CreateInquiryCommandHandler CreateHandler() =>
        new(_inquiryRepo.Object, _projectRepo.Object, _propertyRepo.Object, _unitRepo.Object, _mapper);

    private static CreateInquiryDto GeneralDto() => new()
    {
        Name = "Jane Buyer",
        Email = "jane@example.com",
        Phone = "9999999999",
        Message = "Interested in your Mumbai listings.",
    };

    [Fact]
    public async Task Handle_GeneralInquiry_CreatesWithNewStatusAndNoListingLabel()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(new CreateInquiryCommand(GeneralDto()), CancellationToken.None);

        result.Status.Should().Be(nameof(InquiryStatus.New));
        result.ListingType.Should().BeNull();
        result.ListingLabel.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProjectEnquiry_ResolvesLabelFromRealProject()
    {
        _projectRepo.Setup(r => r.GetByIdAsync("pr1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = "pr1", Name = "Sunrise Towers" });

        var dto = GeneralDto();
        dto.ListingType = "Project";
        dto.ListingId = "pr1";

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateInquiryCommand(dto), CancellationToken.None);

        result.ListingType.Should().Be(nameof(InquiryListingType.Project));
        result.ListingLabel.Should().Be("Sunrise Towers");
    }

    [Fact]
    public async Task Handle_UnitEnquiry_ResolvesLabelFromUnitNumberAndPropertySnapshot()
    {
        _unitRepo.Setup(r => r.GetByIdAsync("u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Core.Entities.Unit
            {
                Id = "u1",
                UnitNumber = "A-401",
                PropertySnapshot = new PropertySnapshot { Name = "Block A", Type = "Residential" },
            });

        var dto = GeneralDto();
        dto.ListingType = "Unit";
        dto.ListingId = "u1";

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateInquiryCommand(dto), CancellationToken.None);

        result.ListingLabel.Should().Be("Unit A-401, Block A");
    }

    [Fact]
    public async Task Handle_ListingIdDoesNotExist_ThrowsNotFound()
    {
        _projectRepo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

        var dto = GeneralDto();
        dto.ListingType = "Project";
        dto.ListingId = "missing";

        var handler = CreateHandler();
        var act = () => handler.Handle(new CreateInquiryCommand(dto), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _inquiryRepo.Verify(r => r.AddAsync(It.IsAny<Inquiry>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
