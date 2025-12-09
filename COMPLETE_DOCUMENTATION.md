# Complete CommandModelBinder Documentation

## Table of Contents
1. [Getting Started](#getting-started)
2. [API Reference](#api-reference)
3. [Architecture](#architecture)
4. [Examples](#examples)
5. [Quick Reference](#quick-reference)
6. [Contributing](#contributing)

---

# Getting Started

## Installation

### Option 1: NuGet Package Manager
```
Install-Package CommandModelBinder
```

### Option 2: .NET CLI
```bash
dotnet add package CommandModelBinder
```

### Option 3: Package Manager UI
1. Right-click on your project
2. Select "Manage NuGet Packages"
3. Search for "CommandModelBinder"
4. Click Install

Package: https://www.nuget.org/packages/CommandModelBinder/

## Setup in 4 Steps

### Step 1: Define Command Interface
```csharp
namespace MyApp.Commands;

public interface IMyCommand { }
```

### Step 2: Create Commands
```csharp
public class CreateUserCommand : IMyCommand
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
}

public class DeleteUserCommand : IMyCommand
{
    public int UserId { get; set; }
}
```

### Step 3: Register in Program.cs
```csharp
using CommandModelBinder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0,
        new RequestCommandModelBinderProvider<IMyCommand>(
            new List<ICommandAuthentication>
            {
                new DefaultCommandAuthentication()
            }));
});

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### Step 4: Use in Controller
```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    [HttpPost("create")]
    public IActionResult Create([FromBody] IMyCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (command is CreateUserCommand createCmd)
        {
            return Ok($"User {createCmd.FirstName} created");
        }

        return BadRequest();
    }
}
```

## Troubleshooting

### "ModelState is invalid"
Check errors:
```csharp
if (!ModelState.IsValid)
{
    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
    {
        Console.WriteLine(error.ErrorMessage);
    }
}
```

### "Unauthorized" error
Ensure:
1. User is authenticated
2. User has required roles/claims
3. Command attributes match user permissions

### JSON deserialization fails
Verify:
1. JSON contains `$type` field
2. Property names are camelCase
3. JSON is valid format

---

# API Reference

## Core Classes

### RequestCommandModelBinderProvider<T>

Factory class implementing `IModelBinderProvider`

```csharp
public class RequestCommandModelBinderProvider<T> : IModelBinderProvider
{
    public RequestCommandModelBinderProvider(
        IEnumerable<ICommandAuthentication> commandAuthentications = null)
    
    public IModelBinder GetBinder(ModelBinderProviderContext context)
}
```

**Usage:**
```csharp
var provider = new RequestCommandModelBinderProvider<IMyCommand>(
    new[] { new DefaultCommandAuthentication() });
```

### RequestCommandModelBinder<T>

Core model binder implementing `IModelBinder`

```csharp
public class RequestCommandModelBinder<T> : IModelBinder
{
    public RequestCommandModelBinder(
        IEnumerable<ICommandAuthentication> commandAuthentications = null)
    
    public Task BindModelAsync(ModelBindingContext bindingContext)
}
```

**Responsibilities:**
1. Read JSON from HTTP request body
2. Deserialize to object using Newtonsoft.Json
3. Validate type matches T
4. Execute authentication checks
5. Return ModelBindingResult

## Interfaces

### ICommandAuthentication

```csharp
public interface ICommandAuthentication
{
    bool Execute(ModelBindingContext bindingContext, object model);
}
```

Implement for custom authentication logic:
```csharp
public class CustomAuth : ICommandAuthentication
{
    public bool Execute(ModelBindingContext bindingContext, object model)
    {
        // Your logic
        return true; // or false
    }
}
```

## Attributes

### AllowAnonymousAttribute
```csharp
[AllowAnonymous]
public class PublicCommand : IMyCommand { }
```

### AuthorizeAttribute
```csharp
[Authorize(Roles = "Admin,Manager")]
public class AdminCommand : IMyCommand { }
```

### ClaimRequirementAttribute
```csharp
[ClaimRequirement("Department", "IT")]
public class ITCommand : IMyCommand { }
```

## Extension Methods

### CommandSerializer
```csharp
using CommandModelBinder.Tools;

var command = new MyCommand { Value = "test" };
var json = command.SerializeCommand<IMyCommand>();
```

### CommandAttributesCheck
```csharp
using CommandModelBinder.CommandAuthentications.Attributes;

// Check if anonymous allowed
bool isAnon = command.IsAnonymousAllowed();

// Check role requirement
bool hasRole = command.HasRole(bindingContext);

// Check claim requirement
bool hasClaim = command.HasClaim(bindingContext);
```

## Error Codes

| Error | Key | Meaning |
|-------|-----|---------|
| "no command." | Unauthorized | Empty request body |
| "not valid json." | Unauthorized | Invalid JSON format |
| "Cant parse to object." | Parsing | Type mismatch |
| "roles is not defined." | Unauthorized | Authorize missing Roles |
| "User is not in role." | Unauthorized | User lacks required role |
| "claim is not defined." | Unauthorized | ClaimRequirement malformed |
| "User does not have claim." | Unauthorized | User missing required claim |

---

# Architecture

## Component Overview

```
HTTP Request
    ?
RequestCommandModelBinder<T>
    ??? Read stream
    ??? Deserialize JSON
    ??? Type validation
    ??? Authentication
    ?   ??? AllowAnonymous check
    ?   ??? Role check
    ?   ??? Claim check
    ?   ??? User authenticated check
    ??? Set ModelBindingResult
        ??? Success ? Bind model
        ??? Failure ? Add error
```

## Authentication Flow

### Scenario 1: Public Command
```
[AllowAnonymous]
?
Allow (no auth needed)
```

### Scenario 2: Role-Required Command
```
[Authorize(Roles = "Admin")]
?
Check: User authenticated? AND User has role?
?
Allow if true, Deny if false
```

### Scenario 3: Claim-Required Command
```
[ClaimRequirement("Dept", "IT")]
?
Check: User authenticated? AND User has claim?
?
Allow if true, Deny if false
```

## Design Patterns Used

### Strategy Pattern
`ICommandAuthentication` allows different authentication strategies

### Provider Pattern
`RequestCommandModelBinderProvider<T>` creates model binders

### Decorator Pattern
Attributes decorate commands with requirements

### Extension Methods
`CommandAttributesCheck` provides readable attribute checking

---

# Examples

## Example 1: Basic Usage

```csharp
public interface IOrderCommand { }

public class CreateOrderCommand : IOrderCommand
{
    public int CustomerId { get; set; }
    public decimal Total { get; set; }
}

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    [HttpPost]
    public IActionResult Create([FromBody] IOrderCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (command is CreateOrderCommand createOrder)
        {
            // Process order
            return Ok($"Order total: {createOrder.Total}");
        }

        return BadRequest();
    }
}
```

## Example 2: Anonymous Access

```csharp
[AllowAnonymous]
public class SearchProductsCommand : IProductCommand
{
    public string SearchTerm { get; set; }
}

// No authentication required
```

## Example 3: Role-Based Auth

```csharp
[Authorize(Roles = "Administrator,Manager")]
public class DeleteProductCommand : IProductCommand
{
    public int ProductId { get; set; }
}

// User must have Administrator OR Manager role
```

## Example 4: Claim-Based Auth

```csharp
[ClaimRequirement("Department", "Sales")]
public class UpdateQuotaCommand : ISalesCommand
{
    public decimal NewQuota { get; set; }
}

// User must have Department="Sales" claim
```

## Example 5: Custom Handler

```csharp
public class IPWhitelistAuth : ICommandAuthentication
{
    private readonly List<string> _allowedIPs;

    public IPWhitelistAuth(List<string> allowedIPs)
    {
        _allowedIPs = allowedIPs;
    }

    public bool Execute(ModelBindingContext ctx, object model)
    {
        var ip = ctx.ActionContext.HttpContext.Connection.RemoteIpAddress?.ToString();
        
        if (!_allowedIPs.Contains(ip))
        {
            ctx.ModelState.TryAddModelError("Unauthorized", "IP not allowed");
            return false;
        }

        return true;
    }
}

// Register it
var handlers = new List<ICommandAuthentication>
{
    new DefaultCommandAuthentication(),
    new IPWhitelistAuth(new List<string> { "192.168.1.1" })
};
```

## Example 6: Unit Testing

```csharp
[Test]
public async Task Bind_ValidCommand_Succeeds()
{
    // Arrange
    var command = new MyCommand { Value = "test" };
    var json = command.SerializeCommand<IMyCommand>();
    var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
    
    var binder = new RequestCommandModelBinder<IMyCommand>(
        new[] { new DefaultCommandAuthentication() });
    var ctx = new DefaultModelBindingContext
    {
        ActionContext = new ActionContext
        {
            HttpContext = new DefaultHttpContext { Request = { Body = stream } }
        },
        ModelState = new ModelStateDictionary()
    };

    // Act
    await binder.BindModelAsync(ctx);

    // Assert
    Assert.That(ctx.ModelState.IsValid);
}
```

---

# Quick Reference

## 30-Second Setup
```csharp
// 1. Interface
public interface ICmd { }

// 2. Command
public class MyCmd : ICmd { public string Value { get; set; } }

// 3. Register
builder.Services.AddControllers(opts =>
{
    opts.ModelBinderProviders.Insert(0,
        new RequestCommandModelBinderProvider<ICmd>(
            new[] { new DefaultCommandAuthentication() }));
});

// 4. Use
public IActionResult Do([FromBody] ICmd cmd) => Ok();
```

## Common Patterns

| Pattern | Code |
|---------|------|
| Public | `[AllowAnonymous]` |
| Admin | `[Authorize(Roles = "Admin")]` |
| Multi-Role | `[Authorize(Roles = "Admin,Manager")]` |
| Claim | `[ClaimRequirement("Type", "Value")]` |
| Nested | `public NestedType Property { get; set; }` |

## Attribute Reference

| Attribute | Namespace | Usage |
|-----------|-----------|-------|
| `AllowAnonymous` | Microsoft.AspNetCore.Authorization | Allow public access |
| `Authorize` | Microsoft.AspNetCore.Authorization | Require role/auth |
| `ClaimRequirement` | CommandModelBinder.CommandAuthentications.Attributes | Require claim |

## Namespaces

```csharp
using CommandModelBinder;
using CommandModelBinder.CommandAuthentications;
using CommandModelBinder.CommandAuthentications.Attributes;
using CommandModelBinder.Tools;
using Microsoft.AspNetCore.Authorization;
```

## JSON Request Format

```json
{
  "$type": "MyNamespace.MyCommand, MyAssembly",
  "propertyName": "value",
  "number": 123,
  "nested": {
    "nestedProp": "value"
  }
}
```

## Testing Helper

```csharp
var user = new ClaimsPrincipal(
    new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "1"),
        new Claim(ClaimTypes.Role, "Admin"),
        new Claim("Department", "IT")
    }));

// Use in DefaultHttpContext
var httpContext = new DefaultHttpContext { User = user };
```

---

# Contributing

## Development Setup

```bash
git clone https://github.com/a-t-k/CommandModelBinder.git
cd CommandModelBinder
dotnet restore
dotnet build
dotnet test
```

## Project Structure

```
src/
??? CommandModelBinder/
?   ??? ICommandAuthentication.cs
?   ??? RequestCommandModelBinderProvider.cs
?   ??? CommandAuthentications/
?   ?   ??? DefaultCommandAuthentication.cs
?   ?   ??? Attributes/
?   ?       ??? ClaimRequirementAttribute.cs
?   ?       ??? CommandAttributesCheck.cs
?   ?       ??? ClaimRequirementFilter.cs
?   ??? Tools/
?       ??? CommandSerializer.cs
??? Tests/
    ??? RequestCommandModelBinderTests.cs
```

## Creating Custom Handler

```csharp
public class MyAuth : ICommandAuthentication
{
    public bool Execute(ModelBindingContext bindingContext, object model)
    {
        // Check your condition
        if (/* condition */)
            return true;

        // Set error if fails
        bindingContext.Result = ModelBindingResult.Failed();
        bindingContext.ModelState.TryAddModelError("Key", "Message");
        return false;
    }
}
```

## Building & Testing

```bash
# Build
dotnet build

# Release build
dotnet build -c Release

# Run tests
dotnet test

# Package
dotnet pack -c Release -o nupkg
```

## Performance Tips

1. Cache attribute reflection for high-volume scenarios
2. Minimize authentication handlers chain
3. Use async patterns for large payloads
4. Profile with BenchmarkDotNet if needed

## Code Style

- Follow C# conventions
- Use clear, descriptive names
- Add XML doc comments to public APIs
- Write unit tests for new features
- Keep error messages clear and actionable

---

## Version Information

- **Current Version**: 0.0.3
- **.NET Target**: net9.0
- **Dependencies**:
  - Microsoft.AspNetCore.Authorization 9.0.2
  - Microsoft.AspNetCore.Mvc.Abstractions 2.3.0
  - Microsoft.AspNetCore.Mvc.Core 2.3.0
  - Newtonsoft.Json 13.0.3

---

## Quick Links

- **GitHub**: https://github.com/a-t-k/CommandModelBinder
- **NuGet**: https://www.nuget.org/packages/CommandModelBinder/
- **Issues**: https://github.com/a-t-k/CommandModelBinder/issues

---

## FAQ

**Q: Can I use dependency injection with handlers?**  
A: Yes! Register handlers in the DI container as needed.

**Q: How do I handle complex authorization?**  
A: Implement `ICommandAuthentication` for custom logic.

**Q: Is this suitable for microservices?**  
A: Yes, it's lightweight and works well in distributed architectures.

**Q: Can I combine multiple auth methods?**  
A: Yes! Use multiple handlers in the pipeline.

**Q: How do I debug binding failures?**  
A: Check `ModelState` errors and use `ILogger` for diagnostics.

---

## Support

- ?? Documentation: See above sections
- ?? Report Issues: GitHub Issues
- ?? Questions: GitHub Discussions
- ?? Contributing: See Contributing section

---

**Made with ?? for ASP.NET Core developers**
