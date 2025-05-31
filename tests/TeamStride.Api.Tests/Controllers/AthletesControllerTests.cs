using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TeamStride.Api.Controllers;
using TeamStride.Application.Athletes.Dtos;
using TeamStride.Application.Athletes.Services;
using TeamStride.Application.Common.Models;
using TeamStride.Domain.Entities;

namespace TeamStride.Api.Tests.Controllers;

public class AthletesControllerTests
{
    private readonly Mock<IAthleteService> _mockAthleteService;
    private readonly Mock<ILogger<AthletesController>> _mockLogger;
    private readonly AthletesController _controller;

    public AthletesControllerTests()
    {
        _mockAthleteService = new Mock<IAthleteService>();
        _mockLogger = new Mock<ILogger<AthletesController>>();
        _controller = new AthletesController(_mockAthleteService.Object, _mockLogger.Object);
    }

    #region GetTeamRoster Tests

    [Fact]
    public async Task GetTeamRoster_WithValidParameters_ShouldReturnOkWithAthletes()
    {
        // Arrange
        var athletes = new List<AthleteDto>
        {
            new AthleteDto
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid().ToString(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@test.com",
                Role = AthleteRole.Athlete
            },
            new AthleteDto
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid().ToString(),
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@test.com",
                Role = AthleteRole.Captain
            }
        };

        var paginatedList = new PaginatedList<AthleteDto>(athletes, 2, 1, 10);
        _mockAthleteService.Setup(x => x.GetTeamRosterAsync(1, 10))
            .ReturnsAsync(paginatedList);

        // Act
        var result = await _controller.GetTeamRoster(1, 10);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var returnedAthletes = okResult.Value.ShouldBeOfType<PaginatedList<AthleteDto>>();
        returnedAthletes.Items.Count.ShouldBe(2);
        returnedAthletes.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetTeamRoster_WithUnauthorizedAccess_ShouldReturnForbid()
    {
        // Arrange
        _mockAthleteService.Setup(x => x.GetTeamRosterAsync(1, 10))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _controller.GetTeamRoster(1, 10);

        // Assert
        var forbidResult = result.ShouldBeOfType<ForbidResult>();
        forbidResult.AuthenticationSchemes.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTeamRoster_WithException_ShouldReturnInternalServerError()
    {
        // Arrange
        _mockAthleteService.Setup(x => x.GetTeamRosterAsync(1, 10))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetTeamRoster(1, 10);

        // Assert
        var statusResult = result.ShouldBeOfType<ObjectResult>();
        statusResult.StatusCode.ShouldBe(500);
    }

    #endregion

    #region GetAthlete Tests

    [Fact]
    public async Task GetAthlete_WithValidId_ShouldReturnOkWithAthlete()
    {
        // Arrange
        var athleteId = Guid.NewGuid();
        var athlete = new AthleteDto
        {
            Id = athleteId,
            UserId = Guid.NewGuid().ToString(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            Role = AthleteRole.Athlete
        };

        _mockAthleteService.Setup(x => x.GetByIdAsync(athleteId))
            .ReturnsAsync(athlete);

        // Act
        var result = await _controller.GetAthlete(athleteId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var returnedAthlete = okResult.Value.ShouldBeOfType<AthleteDto>();
        returnedAthlete.Id.ShouldBe(athleteId);
        returnedAthlete.FirstName.ShouldBe("John");
    }

    [Fact]
    public async Task GetAthlete_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var athleteId = Guid.NewGuid();
        _mockAthleteService.Setup(x => x.GetByIdAsync(athleteId))
            .ThrowsAsync(new InvalidOperationException($"Athlete with ID {athleteId} not found"));

        // Act
        var result = await _controller.GetAthlete(athleteId);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        notFoundResult.Value.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetAthlete_WithUnauthorizedAccess_ShouldReturnForbid()
    {
        // Arrange
        var athleteId = Guid.NewGuid();
        _mockAthleteService.Setup(x => x.GetByIdAsync(athleteId))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _controller.GetAthlete(athleteId);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region GetAthleteByUserId Tests

    [Fact]
    public async Task GetAthleteByUserId_WithExistingUser_ShouldReturnOkWithAthlete()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var athlete = new AthleteDto
        {
            Id = Guid.NewGuid(),
            UserId = userId.ToString(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            Role = AthleteRole.Athlete
        };

        _mockAthleteService.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(athlete);

        // Act
        var result = await _controller.GetAthleteByUserId(userId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var returnedAthlete = okResult.Value.ShouldBeOfType<AthleteDto>();
        returnedAthlete.UserId.ShouldBe(userId.ToString());
    }

    [Fact]
    public async Task GetAthleteByUserId_WithNonExistentUser_ShouldReturnNoContent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockAthleteService.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync((AthleteDto?)null);

        // Act
        var result = await _controller.GetAthleteByUserId(userId);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
    }

    #endregion

    #region GetTeamCaptains Tests

    [Fact]
    public async Task GetTeamCaptains_WithCaptains_ShouldReturnOkWithCaptains()
    {
        // Arrange
        var captains = new List<AthleteDto>
        {
            new AthleteDto
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid().ToString(),
                FirstName = "Captain",
                LastName = "One",
                Email = "captain1@test.com",
                Role = AthleteRole.Captain
            },
            new AthleteDto
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid().ToString(),
                FirstName = "Captain",
                LastName = "Two",
                Email = "captain2@test.com",
                Role = AthleteRole.Captain
            }
        };

        _mockAthleteService.Setup(x => x.GetTeamCaptainsAsync())
            .ReturnsAsync(captains);

        // Act
        var result = await _controller.GetTeamCaptains();

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var returnedCaptains = okResult.Value.ShouldBeOfType<List<AthleteDto>>();
        returnedCaptains.Count.ShouldBe(2);
        returnedCaptains.All(c => c.Role == AthleteRole.Captain).ShouldBeTrue();
    }

    #endregion

    #region CreateAthlete Tests

    [Fact]
    public async Task CreateAthlete_WithValidData_ShouldReturnCreatedWithAthlete()
    {
        // Arrange
        var createDto = new CreateAthleteDto
        {
            Email = "newathlete@test.com",
            FirstName = "New",
            LastName = "Athlete",
            Role = AthleteRole.Athlete
        };

        var createdAthlete = new AthleteDto
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid().ToString(),
            FirstName = "New",
            LastName = "Athlete",
            Email = "newathlete@test.com",
            Role = AthleteRole.Athlete
        };

        _mockAthleteService.Setup(x => x.CreateAsync(createDto))
            .ReturnsAsync(createdAthlete);

        // Act
        var result = await _controller.CreateAthlete(createDto);

        // Assert
        var createdResult = result.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.ActionName.ShouldBe(nameof(AthletesController.GetAthlete));
        createdResult.RouteValues!["id"].ShouldBe(createdAthlete.Id);
        
        var returnedAthlete = createdResult.Value.ShouldBeOfType<AthleteDto>();
        returnedAthlete.Email.ShouldBe("newathlete@test.com");
    }

    [Fact]
    public async Task CreateAthlete_WithInvalidModelState_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = new CreateAthleteDto
        {
            Email = "invalid-email",
            FirstName = "",
            LastName = ""
        };

        _controller.ModelState.AddModelError("Email", "Email is invalid");
        _controller.ModelState.AddModelError("FirstName", "FirstName is required");

        // Act
        var result = await _controller.CreateAthlete(createDto);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.Value.ShouldBeOfType<SerializableError>();
    }

    [Fact]
    public async Task CreateAthlete_WithUnauthorizedAccess_ShouldReturnForbid()
    {
        // Arrange
        var createDto = new CreateAthleteDto
        {
            Email = "athlete@test.com",
            FirstName = "Test",
            LastName = "Athlete"
        };

        _mockAthleteService.Setup(x => x.CreateAsync(createDto))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _controller.CreateAthlete(createDto);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CreateAthlete_WithBusinessRuleViolation_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = new CreateAthleteDto
        {
            Email = "athlete@test.com",
            FirstName = "Test",
            LastName = "Athlete"
        };

        _mockAthleteService.Setup(x => x.CreateAsync(createDto))
            .ThrowsAsync(new InvalidOperationException("Team has reached the maximum number of athletes"));

        // Act
        var result = await _controller.CreateAthlete(createDto);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.Value.ShouldNotBeNull();
    }

    #endregion

    #region UpdateAthlete Tests

    [Fact]
    public async Task UpdateAthlete_WithValidData_ShouldReturnOkWithUpdatedAthlete()
    {
        // Arrange
        var athleteId = Guid.NewGuid();
        var updateDto = new UpdateAthleteDto
        {
            Role = AthleteRole.Captain,
            JerseyNumber = "99"
        };

        var updatedAthlete = new AthleteDto
        {
            Id = athleteId,
            UserId = Guid.NewGuid().ToString(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            Role = AthleteRole.Captain,
            JerseyNumber = "99"
        };

        _mockAthleteService.Setup(x => x.UpdateAsync(athleteId, updateDto))
            .ReturnsAsync(updatedAthlete);

        // Act
        var result = await _controller.UpdateAthlete(athleteId, updateDto);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var returnedAthlete = okResult.Value.ShouldBeOfType<AthleteDto>();
        returnedAthlete.Role.ShouldBe(AthleteRole.Captain);
        returnedAthlete.JerseyNumber.ShouldBe("99");
    }

    [Fact]
    public async Task UpdateAthlete_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var athleteId = Guid.NewGuid();
        var updateDto = new UpdateAthleteDto { Role = AthleteRole.Captain };

        _mockAthleteService.Setup(x => x.UpdateAsync(athleteId, updateDto))
            .ThrowsAsync(new InvalidOperationException($"Athlete with ID {athleteId} not found"));

        // Act
        var result = await _controller.UpdateAthlete(athleteId, updateDto);

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region UpdateAthleteRole Tests

    [Fact]
    public async Task UpdateAthleteRole_WithValidData_ShouldReturnOkWithUpdatedAthlete()
    {
        // Arrange
        var athleteId = Guid.NewGuid();
        var newRole = AthleteRole.Captain;

        var updatedAthlete = new AthleteDto
        {
            Id = athleteId,
            UserId = Guid.NewGuid().ToString(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            Role = AthleteRole.Captain
        };

        _mockAthleteService.Setup(x => x.UpdateRoleAsync(athleteId, newRole))
            .ReturnsAsync(updatedAthlete);

        // Act
        var result = await _controller.UpdateAthleteRole(athleteId, newRole);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var returnedAthlete = okResult.Value.ShouldBeOfType<AthleteDto>();
        returnedAthlete.Role.ShouldBe(AthleteRole.Captain);
    }

    #endregion

    #region UpdatePhysicalStatus Tests

    [Fact]
    public async Task UpdatePhysicalStatus_WithValidData_ShouldReturnOkWithUpdatedAthlete()
    {
        // Arrange
        var athleteId = Guid.NewGuid();
        var hasPhysical = true;

        var updatedAthlete = new AthleteDto
        {
            Id = athleteId,
            UserId = Guid.NewGuid().ToString(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            Role = AthleteRole.Athlete,
            HasPhysicalOnFile = true
        };

        _mockAthleteService.Setup(x => x.UpdatePhysicalStatusAsync(athleteId, hasPhysical))
            .ReturnsAsync(updatedAthlete);

        // Act
        var result = await _controller.UpdatePhysicalStatus(athleteId, hasPhysical);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var returnedAthlete = okResult.Value.ShouldBeOfType<AthleteDto>();
        returnedAthlete.HasPhysicalOnFile.ShouldBeTrue();
    }

    #endregion

    #region UpdateWaiverStatus Tests

    [Fact]
    public async Task UpdateWaiverStatus_WithValidData_ShouldReturnOkWithUpdatedAthlete()
    {
        // Arrange
        var athleteId = Guid.NewGuid();
        var hasSigned = true;

        var updatedAthlete = new AthleteDto
        {
            Id = athleteId,
            UserId = Guid.NewGuid().ToString(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            Role = AthleteRole.Athlete,
            HasWaiverSigned = true
        };

        _mockAthleteService.Setup(x => x.UpdateWaiverStatusAsync(athleteId, hasSigned))
            .ReturnsAsync(updatedAthlete);

        // Act
        var result = await _controller.UpdateWaiverStatus(athleteId, hasSigned);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var returnedAthlete = okResult.Value.ShouldBeOfType<AthleteDto>();
        returnedAthlete.HasWaiverSigned.ShouldBeTrue();
    }

    #endregion

    #region UpdateAthleteProfile Tests

    [Fact]
    public async Task UpdateAthleteProfile_WithValidData_ShouldReturnOkWithUpdatedAthlete()
    {
        // Arrange
        var athleteId = Guid.NewGuid();
        var profileDto = new UpdateAthleteProfileDto
        {
            PreferredEvents = "100m, 200m",
            PersonalBests = "100m: 10.5s",
            Goals = "Sub 10 seconds"
        };

        var updatedAthlete = new AthleteDto
        {
            Id = athleteId,
            UserId = Guid.NewGuid().ToString(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            Role = AthleteRole.Athlete,
            Profile = new AthleteProfileDto
            {
                Id = Guid.NewGuid(),
                AthleteId = athleteId,
                PreferredEvents = "100m, 200m",
                PersonalBests = "100m: 10.5s",
                Goals = "Sub 10 seconds"
            }
        };

        _mockAthleteService.Setup(x => x.UpdateProfileAsync(athleteId, profileDto))
            .ReturnsAsync(updatedAthlete);

        // Act
        var result = await _controller.UpdateAthleteProfile(athleteId, profileDto);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var returnedAthlete = okResult.Value.ShouldBeOfType<AthleteDto>();
        returnedAthlete.Profile.ShouldNotBeNull();
        returnedAthlete.Profile!.PreferredEvents.ShouldBe("100m, 200m");
    }

    #endregion

    #region DeleteAthlete Tests

    [Fact]
    public async Task DeleteAthlete_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var athleteId = Guid.NewGuid();
        _mockAthleteService.Setup(x => x.DeleteAsync(athleteId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteAthlete(athleteId);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
        _mockAthleteService.Verify(x => x.DeleteAsync(athleteId), Times.Once);
    }

    [Fact]
    public async Task DeleteAthlete_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var athleteId = Guid.NewGuid();
        _mockAthleteService.Setup(x => x.DeleteAsync(athleteId))
            .ThrowsAsync(new InvalidOperationException($"Athlete with ID {athleteId} not found"));

        // Act
        var result = await _controller.DeleteAthlete(athleteId);

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteAthlete_WithUnauthorizedAccess_ShouldReturnForbid()
    {
        // Arrange
        var athleteId = Guid.NewGuid();
        _mockAthleteService.Setup(x => x.DeleteAsync(athleteId))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _controller.DeleteAthlete(athleteId);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region IsAthleteInTeam Tests

    [Fact]
    public async Task IsAthleteInTeam_WithAthleteInTeam_ShouldReturnOkWithTrue()
    {
        // Arrange
        var athleteId = Guid.NewGuid();
        _mockAthleteService.Setup(x => x.IsAthleteInTeamAsync(athleteId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.IsAthleteInTeam(athleteId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var isInTeam = okResult.Value.ShouldBeOfType<bool>();
        isInTeam.ShouldBeTrue();
    }

    [Fact]
    public async Task IsAthleteInTeam_WithAthleteNotInTeam_ShouldReturnOkWithFalse()
    {
        // Arrange
        var athleteId = Guid.NewGuid();
        _mockAthleteService.Setup(x => x.IsAthleteInTeamAsync(athleteId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.IsAthleteInTeam(athleteId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var isInTeam = okResult.Value.ShouldBeOfType<bool>();
        isInTeam.ShouldBeFalse();
    }

    #endregion
} 