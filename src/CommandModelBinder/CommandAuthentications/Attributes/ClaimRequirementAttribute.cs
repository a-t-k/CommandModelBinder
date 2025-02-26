using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CommandModelBinder.CommandAuthentications.Attributes;

public class ClaimRequirementAttribute : TypeFilterAttribute
{
    public ClaimRequirementAttribute(string claimType, string claimValue) : base(typeof(ClaimRequirementFilter))
    {
        this.Arguments = [new Claim(claimType, claimValue)];
    }
}