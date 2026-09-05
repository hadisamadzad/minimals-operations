# Minimals.Operations

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=hadisamadzad_operations&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=hadisamadzad_operations)
[![🛡️ Security Checks](https://github.com/hadisamadzad/minimals-operations/actions/workflows/security-check.yaml/badge.svg)](https://github.com/hadisamadzad/minimals-operations/actions/workflows/security-check.yaml)
[![🧩 Run Unit Tests](https://github.com/hadisamadzad/minimals-operations/actions/workflows/unit-test.yaml/badge.svg)](https://github.com/hadisamadzad/minimals-operations/actions/workflows/unit-test.yaml)
[![🚀 Build, Test & Publish](https://github.com/hadisamadzad/minimals-operations/actions/workflows/build-and-deploy.yaml/badge.svg)](https://github.com/hadisamadzad/minimals-operations/actions/workflows/build-and-deploy.yaml)

[![NuGet](https://img.shields.io/nuget/v/Minimals.Operations.svg)](https://www.nuget.org/packages/Minimals.Operations/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Minimals.Operations.svg)](https://www.nuget.org/packages/Minimals.Operations/)

![Minimals.Operations](icon.png)

A lightweight, type-safe operation result pattern implementation for .NET. Provides structured error handling, operation status tracking, and dependency injection support for implementing use cases in clean architecture applications.

## Features

- ✅ **Type-safe result pattern** - No more exception-driven control flow
- ✅ **Rich status information** - Distinguish between validation errors, not found, unauthorized, and more
- ✅ **Functional composition** - Map, Bind, and Match methods for elegant result handling
- ✅ **Source-generated mediator** - Register commands and event operations at compile time with lazy resolution
- ✅ **Clean architecture ready** - Perfect for implementing use cases and operations
- ✅ **Fully documented** - Comprehensive XML documentation for all APIs
- ✅ **Zero runtime dependencies** - Only requires `Microsoft.Extensions.DependencyInjection.Abstractions`

## Installation

```bash
dotnet add package Minimals.Operations --version 2.0.0
```

## Quick Start

### 1. Define a Command

```csharp
using Minimals.Operations;

public record CreateUserCommand(string Email, string Name) : IOperationCommand<User>;
```

### 2. Implement an Operation

```csharp
public class CreateUserOperation : IOperation<CreateUserCommand, User>
{
    private readonly IUserRepository _repository;

    public CreateUserOperation(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<User>> ExecuteAsync(
        CreateUserCommand command,
        CancellationToken? cancellation = null)
    {
        // Validation
        if (string.IsNullOrEmpty(command.Email))
        {
            return OperationResult<User>.ValidationFailure("Email is required");
        }

        // Check if user exists
        var existing = await _repository.GetByEmailAsync(command.Email);
        if (existing != null)
        {
            return OperationResult<User>.ValidationFailure("User already exists");
        }

        // Create user
        var user = new User { Email = command.Email, Name = command.Name };
        await _repository.AddAsync(user);

        return OperationResult<User>.Success(user);
    }
}
```

### 3. Register the Mediator

```csharp
// In Program.cs or Startup.cs
services.AddOperations();
```

The v2 package includes its Roslyn analyzer automatically. It discovers typed
`IOperationCommand<TResult>` operations and `IOperation<TEvent>` event operations at compile time,
registers them as transient services, and generates a scoped mediator. Each operation is resolved
only when its command or event is first executed within the current dependency-injection scope.

### 4. Use Operations

```csharp
public class UserController : ControllerBase
{
    private readonly IOperationMediator _operations;

    public UserController(IOperationMediator operations)
    {
        _operations = operations;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        var result = await _operations.ExecuteAsync(
            new CreateUserCommand(request.Email, request.Name));

        return result.Match(
            onSuccess: user => Ok(user),
            onFailure: (status, error) => status switch
            {
                OperationStatus.Invalid => BadRequest(error?.Messages),
                OperationStatus.NotFound => NotFound(error?.Messages),
                OperationStatus.Unauthorized => Unauthorized(),
                _ => StatusCode(500, "An error occurred")
            }
        );
    }
}
```

### Publishing Events

Events implement `IOperationEvent`, and one or more event operations implement
`IOperation<TEvent>`:

```csharp
public sealed record UserCreatedEvent(Guid UserId) : IOperationEvent;

public sealed class SendWelcomeEmailOperation
    : IOperation<UserCreatedEvent>
{
    public Task ExecuteAsync(
        UserCreatedEvent @event,
        CancellationToken? cancellation = null)
    {
        // Send the welcome email.
        return Task.CompletedTask;
    }
}
```

Publish the event through the same mediator:

```csharp
await _operations.PublishAsync(
    new UserCreatedEvent(user.Id),
    cancellationToken);
```

All generated event operations for the event type are invoked sequentially in registration order.

## Operation Statuses

The package provides the following operation statuses:

- **`Completed`** - Operation succeeded
- **`NoOperation`** - Operation determined no action was needed (also considered success)
- **`Invalid`** - Validation error
- **`NotFound`** - Resource not found
- **`Unauthorized`** - Authorization/permission error
- **`Unprocessable`** - Semantic error (request is well-formed but cannot be processed)
- **`Failed`** - Unexpected error

## Functional Composition

### Map - Transform Success Values

```csharp
var result = await operation.ExecuteAsync(command);

var mappedResult = result.Map(user => new UserDto
{
    Id = user.Id,
    Email = user.Email
});
```

### Bind - Chain Operations

```csharp
var result = await getUserOperation.ExecuteAsync(getUserCommand);

var finalResult = result.Bind(user =>
    updateUserOperation.ExecuteAsync(new UpdateUserCommand(user.Id, newEmail))
);
```

### Match - Pattern Matching

```csharp
var response = result.Match(
    onSuccess: user => $"Created user: {user.Email}",
    onFailure: (status, error) => $"Failed: {string.Join(", ", error?.Messages ?? [])}"
);
```

### Detailed Match - Handle Each Status

```csharp
var response = result.MatchDetailed(
    onCompleted: user => Ok(user),
    onNoOperation: () => NoContent(),
    onInvalid: error => BadRequest(error?.Messages),
    onNotFound: error => NotFound(error?.Messages),
    onUnauthorized: error => Unauthorized(),
    onUnprocessable: error => UnprocessableEntity(error?.Messages),
    onFailed: error => StatusCode(500, error?.Messages)
);
```

### Side Effects

```csharp
var result = await operation.ExecuteAsync(command);

result
    .OnSuccess(user => _logger.LogInformation("User created: {Email}", user.Email))
    .OnFailure((status, error) => _logger.LogError("Failed: {Messages}", error?.Messages));
```

## Factory Methods

### Success Results

```csharp
// With value
return OperationResult<User>.Success(user);

// Without value
return OperationResult<NoResult>.Success();
```

### Failure Results

```csharp
// Validation failure
return OperationResult<User>.ValidationFailure("Email is required", "Password is too short");

// Not found
return OperationResult<User>.NotFoundFailure("User not found");

// Authorization failure
return OperationResult<User>.AuthorizationFailure("Insufficient permissions");

// Unprocessable
return OperationResult<User>.UnprocessableFailure("Cannot delete active user");

// General failure
return OperationResult<User>.Failure("Unexpected error occurred");
```

## Best Practices

1. **Commands are immutable** - Use records for commands
2. **One operation per use case** - Keep operations focused
3. **Never throw exceptions** - Always return OperationResult
4. **Validate early** - Check inputs at the start of operations
5. **Use specific statuses** - Don't overuse `Failed`, be specific
6. **Keep operations testable** - Inject dependencies via constructor

## Testing Example

```csharp
[Fact]
public async Task CreateUser_WithValidData_ReturnsSuccess()
{
    // Arrange
    var repository = new InMemoryUserRepository();
    var operation = new CreateUserOperation(repository);
    var command = new CreateUserCommand("test@example.com", "Test User");

    // Act
    var result = await operation.ExecuteAsync(command);

    // Assert
    Assert.True(result.Succeeded);
    Assert.NotNull(result.Value);
    Assert.Equal("test@example.com", result.Value.Email);
}

[Fact]
public async Task CreateUser_WithInvalidEmail_ReturnsValidationFailure()
{
    // Arrange
    var repository = new InMemoryUserRepository();
    var operation = new CreateUserOperation(repository);
    var command = new CreateUserCommand("", "Test User");

    // Act
    var result = await operation.ExecuteAsync(command);

    // Assert
    Assert.False(result.Succeeded);
    Assert.Equal(OperationStatus.Invalid, result.Status);
    Assert.NotNull(result.Error);
}
```

## License

MIT License - See LICENSE file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Author

Hadi Samadzad
