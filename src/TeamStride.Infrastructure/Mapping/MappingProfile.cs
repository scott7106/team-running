using AutoMapper;
using TeamStride.Domain.Entities;
using TeamStride.Application.Users.Dtos;
using TeamStride.Application.Tenants.Dtos;
using TeamStride.Domain.Identity;

namespace TeamStride.Infrastructure.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ApplicationUser, UserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<Tenant, TenantDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Tier, opt => opt.MapFrom(src => src.Tier.ToString()));

        CreateMap<UserTenant, UserTenantDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
            .ForMember(dest => dest.Tenant, opt => opt.MapFrom(src => src.Tenant));
    }
} 