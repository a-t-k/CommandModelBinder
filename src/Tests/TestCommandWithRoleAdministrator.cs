using Microsoft.AspNetCore.Authorization;

namespace Tests;

[Authorize(Roles = "Administrator")]
public class TestCommandWithRoleAdministrator : IRequestTestCommand;