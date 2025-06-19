using TeamStride.Application.Athletes.Dtos;
using TeamStride.Application.Common.Models;
using TeamStride.Domain.Entities;

namespace TeamStride.Application.Athletes.Services;

public interface IAthleteService
{
    Task<AthleteDto> GetByIdAsync(Guid id);
    Task<AthleteDto?> GetByUserIdAsync(Guid userId);
    Task<PaginatedList<AthleteDto>> GetTeamRosterAsync(
        int pageNumber = 1, 
        int pageSize = 10,
        string? searchQuery = null,
        AthleteRole? role = null,
        Gender? gender = null,
        GradeLevel? gradeLevel = null,
        bool? hasPhysical = null,
        bool? hasWaiver = null);
    Task<AthleteDto> CreateAsync(CreateAthleteDto dto);
    Task<AthleteDto> UpdateAsync(Guid id, UpdateAthleteDto dto);
    Task DeleteAsync(Guid id);
    Task<AthleteDto> UpdateRoleAsync(Guid id, AthleteRole role);
    Task<AthleteDto> UpdatePhysicalStatusAsync(Guid id, bool hasPhysical);
    Task<AthleteDto> UpdateWaiverStatusAsync(Guid id, bool hasSigned);
    Task<AthleteDto> UpdateProfileAsync(Guid id, UpdateAthleteProfileDto profile);
    Task<IEnumerable<AthleteDto>> GetTeamCaptainsAsync();
    Task<bool> IsAthleteInTeamAsync(Guid athleteId);
} 