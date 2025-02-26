using Microsoft.AspNetCore.Authorization;

namespace Tests;

[Authorize(Roles = "Wrong")]
public class TestCommandWithWrongRole : IRequestTestCommand;