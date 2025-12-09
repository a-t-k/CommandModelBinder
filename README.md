# CommandModelBinder for ASP.NET Core

A professional, lightweight library for binding HTTP requests to strongly-typed command objects with built-in authentication, authorization, and serialization support.

## 🚀 Features

* **Type-Safe Command Binding** - Generic type support for strongly-typed commands
* **Built-in Authentication** - Support for role-based and claim-based authorization
* **Authorization Attributes** - Use `[Authorize]`, `[AllowAnonymous]`, `[ClaimRequirement]`
* **Automatic JSON Serialization** - Consistent handling with type information
* **Extensible** - Create custom authentication handlers for specialized logic
* **FluentValidation Ready** - Compatible with validation frameworks
* **Production-Ready** - Battle-tested quality code

## 📦 Installation

### NuGet Package Manager
```
Install-Package CommandModelBinder
```

### .NET CLI
```bash
dotnet add package CommandModelBinder
```

The package is available on [NuGet](https://www.nuget.org/packages/CommandModelBinder/).

## ⚡ Quick Start

### 1. Define Your Command Interface
```csharp
public interface IMyCommand { }
```

### 2. Create Commands
```csharp
public class CreateUserCommand : IMyCommand
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
}
```

### 3. Register in Program.cs
```csharp
using CommandModelBinder;

builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0,
        new RequestCommandModelBinderProvider<IMyCommand>(
            new List<ICommandAuthentication> 
            { 
                new DefaultCommandAuthentication() 
            }));
});
```

### 4. Use in Your Controller
```csharp
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    [HttpPost("create")]
    public IActionResult Create([FromBody] IMyCommand command)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (command is CreateUserCommand createCmd)
        {
            // Process command
            return Ok($"User {createCmd.FirstName} created");
        }

        return BadRequest();
    }
}
```

## 🔒 Authentication Examples

### Anonymous Access
```csharp
[AllowAnonymous]
public class PublicSearchCommand : IMyCommand { }
```

### Role-Based Authorization
```csharp
[Authorize(Roles = "Administrator")]
public class AdminCommand : IMyCommand { }
```

### Claim-Based Authorization
```csharp
[ClaimRequirement("Department", "IT")]
public class ITCommand : IMyCommand { }
```

## 🏗️ Architecture

### Core Components

| Component | Purpose |
|-----------|---------|
| `RequestCommandModelBinderProvider<T>` | Factory for creating model binders |
| `RequestCommandModelBinder<T>` | Core binding and deserialization logic |
| `ICommandAuthentication` | Extensible authentication interface |
| `DefaultCommandAuthentication` | Standard authentication implementation |
| `CommandSerializer` | Consistent JSON serialization |
| `ClaimRequirementAttribute` | Claim-based authorization attribute |

### Request Flow
```
HTTP Request
    ↓
JSON Deserialization
    ↓
Type Validation
    ↓
Authentication Check (Attributes)
    ↓
ModelState Validation
    ↓
Controller Action Parameter
```

## 🔧 Custom Authentication Handlers

Create specialized authentication logic:

```csharp
public class CustomAuthHandler : ICommandAuthentication
{
    public bool Execute(ModelBindingContext bindingContext, object model)
    {
        // Your custom authentication logic
        if (/* your condition */)
            return true;

        bindingContext.ModelState.TryAddModelError("Unauthorized", "Custom error");
        return false;
    }
}

// Register it
var handlers = new List<ICommandAuthentication>
{
    new DefaultCommandAuthentication(),
    new CustomAuthHandler()
};
```

## ✨ Key Features

### 1. Type Safety
Generic parameter `T` ensures only matching commands are bound:
```csharp
public class RequestCommandModelBinder<IMyCommand> { }
```

### 2. Flexible Authentication
- Role-based: `[Authorize(Roles = "...")]`
- Claim-based: `[ClaimRequirement("type", "value")]`
- Anonymous: `[AllowAnonymous]`
- Custom: Implement `ICommandAuthentication`

### 3. Serialization
Automatic JSON serialization with:
- Type name handling
- CamelCase property names
- Nested object support
- Enum handling

### 4. Error Handling
Clear error messages in ModelState:
- "no command." - Empty body
- "not valid json." - Invalid JSON
- "Cant parse to object." - Type mismatch
- "User is not in role." - Authorization failure
- "User does not have claim." - Claim mismatch

## 📋 Requirements

- **.NET Version**: 9.0 or later
- **ASP.NET Core**: 9.0 compatible
- **Dependencies**:
  - Microsoft.AspNetCore.Authorization
  - Microsoft.AspNetCore.Mvc.Abstractions
  - Microsoft.AspNetCore.Mvc.Core
  - Newtonsoft.Json

## 🧪 Testing

The library includes comprehensive unit tests. Run tests with:

```bash
dotnet test
```

Example unit test:
```csharp
[Test]
public async Task Bind_ValidCommand_ShouldSucceed()
{
    var command = new MyCommand();
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

    await binder.BindModelAsync(ctx);

    Assert.That(ctx.ModelState.IsValid);
}
```

## 🎯 Use Cases

- **E-Commerce**: Product catalog and order management
- **CRM Systems**: Customer relationship management commands
- **Admin Panels**: User management and configuration
- **API Gateways**: Request routing and transformation
- **Microservices**: Inter-service command dispatching
- **Real-time Applications**: WebSocket command handling

## 🚀 Best Practices

1. **Validate ModelState** - Always check `ModelState.IsValid` before processing
2. **Default to Secure** - Require authentication unless marked `[AllowAnonymous]`
3. **Use Claims over Roles** - Claim-based authorization is more fine-grained
4. **Test All Paths** - Test authenticated, unauthenticated, and wrong-role scenarios
5. **Document Requirements** - Comment authorization requirements on commands
6. **Use DTOs** - Map commands to domain models, don't use directly
7. **Handle Errors** - Log authorization failures for security monitoring

## 📞 Support

- 📖 **Documentation**: See links above
- 🐛 **Issues**: [GitHub Issues](https://github.com/a-t-k/CommandModelBinder/issues)
- 💬 **Questions**: Post on GitHub Discussions
- 🔧 **Contributing**: See [CONTRIBUTING.md](CONTRIBUTING.md)

## 📝 Version

- **Current Version**: 0.0.4
- **.NET Target**: net9.0
- **Status**: Production Ready

## 📄 License

This project is licensed under the MIT License. See LICENSE file for details.

## 🔗 Links

- **GitHub**: https://github.com/a-t-k/CommandModelBinder
- **NuGet**: https://www.nuget.org/packages/CommandModelBinder/
- **Issues**: https://github.com/a-t-k/CommandModelBinder/issues

---

## Getting Started with Documentation

Start with one of these depending on your needs:

- **New User?** → [GETTING_STARTED.md](GETTING_STARTED.md) (10 minutes)
- **Need Examples?** → [EXAMPLES.md](EXAMPLES.md) (20 minutes)
- **Want API Details?** → [API_REFERENCE.md](API_REFERENCE.md) (15 minutes)
- **Understanding Architecture?** → [ARCHITECTURE.md](ARCHITECTURE.md) (20 minutes)
- **Quick Lookup?** → [QUICK_REFERENCE.md](QUICK_REFERENCE.md) (5 minutes)
- **Not sure where to start?** → [DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md) (10 minutes)

---

**Made with ❤️ for ASP.NET Core developers**