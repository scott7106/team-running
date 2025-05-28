using AutoMapper;
using TeamStride.Domain.Entities;
using TeamStride.Application.Users.Dtos;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Domain.Identity;

namespace TeamStride.Infrastructure.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ApplicationUser, UserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<UserTeam, UserTeamDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
            .ForMember(dest => dest.Team, opt => opt.Ignore());

        // Team Management mappings
        CreateMap<Team, TeamDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Tier, opt => opt.MapFrom(src => src.Tier))
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.MemberCount, opt => opt.Ignore())
            .ForMember(dest => dest.AthleteCount, opt => opt.Ignore())
            .ForMember(dest => dest.AdminCount, opt => opt.Ignore())
            .ForMember(dest => dest.HasPendingOwnershipTransfer, opt => opt.Ignore());

        // Global Admin Team mappings
        CreateMap<Team, GlobalAdminTeamDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Tier, opt => opt.MapFrom(src => src.Tier))
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.OwnerId))
            .ForMember(dest => dest.OwnerEmail, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerFirstName, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerLastName, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerDisplayName, opt => opt.Ignore())
            .ForMember(dest => dest.MemberCount, opt => opt.Ignore())
            .ForMember(dest => dest.AthleteCount, opt => opt.Ignore())
            .ForMember(dest => dest.AdminCount, opt => opt.Ignore())
            .ForMember(dest => dest.HasPendingOwnershipTransfer, opt => opt.Ignore());

        // Deleted Team mappings
        CreateMap<Team, DeletedTeamDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Tier, opt => opt.MapFrom(src => src.Tier))
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.OwnerId))
            .ForMember(dest => dest.OwnerEmail, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerDisplayName, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserEmail, opt => opt.Ignore());

        CreateMap<UserTeam, TeamMemberDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => $"{src.User.LastName}, {src.User.FirstName}"))
            .ForMember(dest => dest.IsOwner, opt => opt.MapFrom(src => src.Role == TeamRole.TeamOwner));

        CreateMap<OwnershipTransfer, OwnershipTransferDto>()
            .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src => src.Team.Name))
            .ForMember(dest => dest.InitiatedByUserName, opt => opt.MapFrom(src => $"{src.InitiatedByUser.FirstName} {src.InitiatedByUser.LastName}"))
            .ForMember(dest => dest.InitiatedOn, opt => opt.MapFrom(src => src.CreatedOn));
    }
} 