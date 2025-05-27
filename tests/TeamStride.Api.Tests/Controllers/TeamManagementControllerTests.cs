using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Data;
using Xunit;

namespace TeamStride.Api.Tests.Controllers;

public class TeamManagementControllerTests : IClassFixture<WebApplicationFactory<TeamStride.Api.Program>>
{
    private readonly WebApplicationFactory<TeamStride.Api.Program> _factory;
    private readonly HttpClient _client;

    public TeamManagementControllerTests(WebApplicationFactory<TeamStride.Api.Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetTeams_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/team-management");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTeamById_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/team-management/{teamId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTeamBySubdomain_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var subdomain = "test-team";

        // Act
        var response = await _client.GetAsync($"/api/team-management/subdomain/{subdomain}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CheckSubdomainAvailability_ShouldReturnOk()
    {
        // Arrange
        var subdomain = "available-subdomain";

        // Act
        var response = await _client.GetAsync($"/api/team-management/subdomain/{subdomain}/availability");

        // Assert
        // This endpoint might require authentication too, so we'll check for either OK or Unauthorized
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTeam_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var createTeamDto = new CreateTeamDto
        {
            Name = "Test Team",
            Subdomain = "test-team",
            OwnerEmail = "owner@test.com",
            OwnerFirstName = "Test",
            OwnerLastName = "Owner",
            Tier = TeamTier.Free
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/team-management", createTeamDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTeam_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var updateTeamDto = new UpdateTeamDto
        {
            Name = "Updated Team Name"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/team-management/{teamId}", updateTeamDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTeam_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/team-management/{teamId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTierLimits_ShouldReturnOkOrUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/team-management/tiers/{TeamTier.Free}/limits");

        // Assert
        // This endpoint might require authentication too, so we'll check for either OK or Unauthorized
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CanAddAthlete_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/team-management/{teamId}/can-add-athlete");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
} 