using AutoMapper;
using TeamStride.Application.Athletes.Dtos;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;

namespace TeamStride.Infrastructure.Mapping;

public class AthleteProfile : Profile
{
    public AthleteProfile()
    {
        CreateMap<Athlete, AthleteDto>()
            .ForMember(d => d.FirstName, o => o.MapFrom(s => s.User.FirstName))
            .ForMember(d => d.LastName, o => o.MapFrom(s => s.User.LastName))
            .ForMember(d => d.Email, o => o.MapFrom(s => s.User.Email));

        CreateMap<CreateAthleteDto, ApplicationUser>()
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.Email));

        CreateMap<CreateAthleteDto, Athlete>()
            .ForMember(d => d.Profile, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore());

        CreateMap<AthleteProfile, AthleteProfileDto>();
        CreateMap<CreateAthleteProfileDto, AthleteProfile>();
        CreateMap<UpdateAthleteProfileDto, AthleteProfile>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
} 