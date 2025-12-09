using CommandModelBinder.CommandAuthentications.Attributes;

namespace Tests;

[ClaimRequirement("Role", "User")]
public class TestCommandClaimRole : IRequestTestCommand;