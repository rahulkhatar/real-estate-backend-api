using AutoMapper;
using FluentValidation;
using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Inquiries.Commands;

public record UpdateInquiryStatusCommand(string Id, UpdateInquiryStatusDto Dto) : IRequest<InquiryDto>;

public class UpdateInquiryStatusCommandValidator : AbstractValidator<UpdateInquiryStatusCommand>
{
    public UpdateInquiryStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Dto.Status)
            .Must(s => Enum.TryParse<InquiryStatus>(s, true, out _))
            .WithMessage("Status must be one of: New, Contacted, Closed.");
    }
}

/// <summary>Admin-only (enforced at the controller) -- moves a lead through New -> Contacted -> Closed.</summary>
public class UpdateInquiryStatusCommandHandler(IInquiryRepository repository, IMapper mapper)
    : IRequestHandler<UpdateInquiryStatusCommand, InquiryDto>
{
    public async Task<InquiryDto> Handle(UpdateInquiryStatusCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Inquiry), request.Id);

        inquiry.Status = Enum.Parse<InquiryStatus>(request.Dto.Status, true);
        await repository.UpdateAsync(inquiry, cancellationToken);

        return mapper.Map<InquiryDto>(inquiry);
    }
}
