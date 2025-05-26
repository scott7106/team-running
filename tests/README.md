# TeamStride Test Projects

This directory contains all test projects for the TeamStride application, organized by architectural layer and following the testing strategy outlined in the PRD.

## Project Structure

```
tests/
├── TeamStride.Domain.Tests/          # Unit tests for domain entities and business logic
├── TeamStride.Application.Tests/     # Unit tests for application services and DTOs  
├── TeamStride.Infrastructure.Tests/  # Integration tests for repositories and data access
├── TeamStride.Api.Tests/             # Integration tests for API controllers and endpoints
└── TeamStride.IntegrationTests/      # End-to-end integration tests
```

## Testing Framework & Tools

All test projects use the following standardized testing stack:

- **XUnit** - Primary testing framework
- **Shouldly** - Fluent assertion library for readable test assertions
- **Moq** - Mocking framework for creating test doubles
- **SQLite** - In-memory database for integration tests
- **Microsoft.AspNetCore.Mvc.Testing** - For API integration testing

## Test Organization Patterns

### Inheritance-Based Organization
All test projects follow an inheritance-based organization pattern with base test classes:

- `BaseTest` - Base class for domain unit tests
- `BaseIntegrationTest` - Base class for infrastructure tests with SQLite setup
- `BaseApiTest` - Base class for API tests with test server setup

### AAA Pattern
All tests follow the **Arrange → Act → Assert** pattern:

```csharp
[Fact]
public void Method_Scenario_ExpectedResult()
{
    // Arrange
    var input = "test data";
    
    // Act
    var result = methodUnderTest(input);
    
    // Assert
    result.ShouldBe("expected value");
}
```

## Test Project Details

### 1. TeamStride.Domain.Tests
**Purpose**: Unit tests for domain entities, value objects, and business logic.

**Dependencies**:
- TeamStride.Domain
- XUnit, Shouldly, Moq

**Focus Areas**:
- Entity validation and business rules
- Domain service logic
- Value object behavior
- Domain event handling

**Example**:
```csharp
public class TenantTests : BaseTest
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateTenant()
    {
        // Arrange
        var name = "Test Team";
        var subdomain = "testteam";
        
        // Act
        var tenant = new Tenant(name, subdomain, "description");
        
        // Assert
        tenant.Name.ShouldBe(name);
        tenant.Subdomain.ShouldBe(subdomain);
        tenant.IsActive.ShouldBeTrue();
    }
}
```

### 2. TeamStride.Application.Tests
**Purpose**: Unit tests for application services, DTOs, and orchestration logic.

**Dependencies**:
- TeamStride.Domain
- TeamStride.Application
- XUnit, Shouldly, Moq

**Focus Areas**:
- Application service orchestration
- DTO mapping and validation
- Use case implementations
- Command/query handlers

### 3. TeamStride.Infrastructure.Tests
**Purpose**: Integration tests for repositories, data access, and external service integrations.

**Dependencies**:
- TeamStride.Domain
- TeamStride.Application
- TeamStride.Infrastructure
- XUnit, Shouldly, Moq, SQLite

**Focus Areas**:
- Repository implementations
- Database operations and queries
- Entity Framework mappings
- External service integrations (SendGrid, Twilio, etc.)

**Database Setup**:
Uses SQLite in-memory database for fast, isolated tests:
```csharp
public class RepositoryTests : BaseIntegrationTest
{
    // DbContext is automatically configured with SQLite
    // Database is created and disposed per test class
}
```

### 4. TeamStride.Api.Tests
**Purpose**: Integration tests for API controllers, endpoints, and HTTP behavior.

**Dependencies**:
- All source projects
- XUnit, Shouldly, Moq, SQLite, Microsoft.AspNetCore.Mvc.Testing

**Focus Areas**:
- HTTP endpoint behavior
- Request/response validation
- Authentication and authorization
- API contract testing

**Test Server Setup**:
Uses `WebApplicationFactory` with SQLite for full API testing:
```csharp
public class TenantsControllerTests : BaseApiTest
{
    public TenantsControllerTests(WebApplicationFactory<Program> factory) : base(factory) { }
    
    [Fact]
    public async Task GetTenants_ShouldReturnOkResult()
    {
        // Test uses real HTTP client against test server
        var response = await Client.GetAsync("/api/tenants");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
```

### 5. TeamStride.IntegrationTests
**Purpose**: End-to-end integration tests that test complete user scenarios.

**Dependencies**:
- All source projects
- XUnit, Shouldly, Moq, SQLite, Microsoft.AspNetCore.Mvc.Testing

**Focus Areas**:
- Complete user workflows
- Multi-tenant scenarios
- Authentication flows
- Cross-cutting concerns

## Running Tests

### Command Line
```bash
# Run all tests
dotnet test

# Run tests for specific project
dotnet test tests/TeamStride.Domain.Tests

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests in parallel
dotnet test --parallel
```

### Visual Studio
- Use Test Explorer to run and debug tests
- Right-click on test projects or individual tests to run
- Use Live Unit Testing for continuous test execution

## Test Data Management

### Domain Tests
- Use object builders or factory methods for creating test entities
- Keep test data minimal and focused on the specific scenario

### Integration Tests
- Use the `SeedTestData()` method in base classes to set up test data
- Clear database between tests using `ClearDatabase()` methods
- Use realistic but minimal data sets

### API Tests
- Seed data through the `SeedTestDataAsync()` method
- Test with various user roles and tenant contexts
- Use helper methods for common setup scenarios

## Best Practices

### Test Naming
- Use descriptive test names: `Method_Scenario_ExpectedResult`
- Be specific about the scenario being tested
- Include edge cases and error conditions

### Test Organization
- Group related tests in the same test class
- Use nested classes for complex scenarios
- Keep tests focused on a single concern

### Assertions
- Use Shouldly for readable assertions
- Test both positive and negative scenarios
- Verify all relevant aspects of the result

### Mocking
- Mock external dependencies and infrastructure concerns
- Don't mock the system under test
- Use meaningful mock setups that reflect real behavior

### Test Data
- Keep test data minimal and relevant
- Use constants or builders for reusable test data
- Avoid dependencies between tests

## Continuous Integration

Tests are designed to run in CI/CD pipelines:
- No external dependencies (uses SQLite in-memory)
- Fast execution times
- Deterministic results
- Parallel execution support

## Coverage Goals

Target test coverage by layer:
- **Domain**: 90%+ (critical business logic)
- **Application**: 80%+ (orchestration and validation)
- **Infrastructure**: 70%+ (data access and integrations)
- **API**: 80%+ (endpoint behavior and contracts)

## Troubleshooting

### Common Issues

1. **SQLite Connection Issues**
   - Ensure `Database.OpenConnection()` is called before `EnsureCreated()`
   - Check that connection is properly disposed

2. **Test Isolation Problems**
   - Use `ClearDatabase()` methods between tests
   - Avoid static state in test classes

3. **Async Test Issues**
   - Always await async operations in tests
   - Use `Task.CompletedTask` for synchronous implementations

4. **Mocking Issues**
   - Verify mock setups match actual usage
   - Use `MockBehavior.Strict` for precise control

For additional help, refer to the main project documentation or contact the development team. 