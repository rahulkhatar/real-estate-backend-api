using AutoMapper;
using FluentValidation;
using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Inquiries.Commands;

public record CreateInquiryCommand(CreateInquiryDto Dto) : IRequest<InquiryDto>;

public class CreateInquiryCommandValidator : AbstractValidator<CreateInquiryCommand>
{
    public CreateInquiryCommandValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Dto.Phone).NotEmpty();
        RuleFor(x => x.Dto.Message).NotEmpty().MaximumLength(2000);

        RuleFor(x => x.Dto.ListingType)
            .Must(t => Enum.TryParse<InquiryListingType>(t, true, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Dto.ListingType))
            .WithMessage("ListingType must be one of: Project, Property, Unit.");

        RuleFor(x => x.Dto.ListingId)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.Dto.ListingType))
            .WithMessage("ListingId is required when ListingType is set.");
    }
}

/// <summary>
/// Public endpoint (anonymous, no auth) -- anyone can submit a general contact message, or an
/// "Enquire" about a specific Project/Property/Unit. When a listing is referenced, its display
/// label is resolved here from the real entity rather than trusted from the client, and a
/// missing/invalid listing id fails the whole submission (better than silently accepting a lead
/// tied to nothing an Admin can act on).
/// </summary>
public class CreateInquiryCommandHandler(
    IInquiryRepository inquiryRepository,
    IProjectRepository projectRepository,
    IPropertyRepository propertyRepository,
    IUnitRepository unitRepository,
    IMapper mapper) : IRequestHandler<CreateInquiryCommand, InquiryDto>
{
    public async Task<InquiryDto> Handle(CreateInquiryCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        InquiryListingType? listingType = null;
        var listingLabel = string.Empty;

        if (!string.IsNullOrWhiteSpace(dto.ListingType) && !string.IsNullOrWhiteSpace(dto.ListingId))
        {
            listingType = Enum.Parse<InquiryListingType>(dto.ListingType, true);
            listingLabel = await ResolveListingLabelAsync(listingType.Value, dto.ListingId, cancellationToken);
        }

        var inquiry = new Inquiry
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Message = dto.Message,
            ListingType = listingType,
            ListingId = dto.ListingId,
            ListingLabel = listingLabel,
            Status = InquiryStatus.New,
        };

        var created = await inquiryRepository.AddAsync(inquiry, cancellationToken);
        return mapper.Map<InquiryDto>(created);
    }

    private async Task<string> ResolveListingLabelAsync(InquiryListingType type, string listingId, CancellationToken ct)
    {
        switch (type)
        {
            case InquiryListingType.Project:
                var project = await projectRepository.GetByIdAsync(listingId, ct)
                    ?? throw new NotFoundException(nameof(Project), listingId);
                return project.Name;

            case InquiryListingType.Property:
                var property = await propertyRepository.GetByIdAsync(listingId, ct)
                    ?? throw new NotFoundException(nameof(Property), listingId);
                return $"{property.Name}, {property.ProjectSnapshot.Name}";

            case InquiryListingType.Unit:
                var unit = await unitRepository.GetByIdAsync(listingId, ct)
                    ?? throw new NotFoundException(nameof(Core.Entities.Unit), listingId);
                return $"Unit {unit.UnitNumber}, {unit.PropertySnapshot.Name}";

            default:
                return string.Empty;
        }
    }
}
