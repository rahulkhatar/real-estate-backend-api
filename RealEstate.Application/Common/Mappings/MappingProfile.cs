using AutoMapper;
using RealEstate.Application.DTOs;
using RealEstate.Application.DTOs.Common;
using RealEstate.Core.Entities;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Value objects
        CreateMap<Coordinates, CoordinatesDto>().ReverseMap();
        CreateMap<Location, LocationDto>().ReverseMap();
        CreateMap<ImageAsset, ImageAssetDto>().ReverseMap();
        CreateMap<SizeInfo, SizeInfoDto>().ReverseMap();
        CreateMap<ProjectSnapshot, ProjectSnapshotDto>().ReverseMap();
        CreateMap<PropertySnapshot, PropertySnapshotDto>().ReverseMap();
        CreateMap<UnitSnapshot, UnitSnapshotDto>().ReverseMap();
        CreateMap<AgentSnapshot, AgentSnapshotDto>().ReverseMap();

        // Project
        CreateMap<Project, ProjectDto>();
        CreateMap<CreateProjectDto, Project>();
        CreateMap<UpdateProjectDto, Project>();

        // Property
        CreateMap<Property, PropertyDto>();
        CreateMap<CreatePropertyDto, Property>();
        CreateMap<UpdatePropertyDto, Property>();

        // Unit
        CreateMap<Unit, UnitDto>();
        CreateMap<CreateUnitDto, Unit>();
        CreateMap<UpdateUnitDto, Unit>();

        // UnitLayout
        CreateMap<UnitLayout, UnitLayoutDto>();
        CreateMap<CreateUnitLayoutDto, UnitLayout>();
        CreateMap<UpdateUnitLayoutDto, UnitLayout>();

        // Agent
        CreateMap<Agent, AgentDto>();
        CreateMap<UpdateAgentProfileDto, Agent>();

        // Booking
        CreateMap<Booking, BookingDto>();

        // Payment
        CreateMap<Payment, PaymentDto>();
    }
}
