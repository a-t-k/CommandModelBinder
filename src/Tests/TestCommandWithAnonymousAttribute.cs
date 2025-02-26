using Microsoft.AspNetCore.Authorization;

namespace Tests;

[AllowAnonymous]
public class TestCommandWithAnonymousAttribute : IRequestTestCommand;