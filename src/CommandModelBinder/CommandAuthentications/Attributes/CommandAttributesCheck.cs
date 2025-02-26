using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;
using System.Reflection;
using System.Security.Claims;

namespace CommandModelBinder.CommandAuthentications.Attributes;
public static class CommandAttributesCheck
{
    public static bool IsAnonymousAllowed(this object command)
    {
        var attribute = command.GetType().GetCustomAttribute<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>(false);
        if (attribute is null)
        {
            return false;
        }

        return true;
    }

    public static bool HasRole(this object command, ModelBindingContext bindingContext)
    {
        var attribute = command.GetType().GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>(false);
        if (attribute is null)
        {
            return true;
        }
        var roles = attribute.Roles?.Split(',').Select(x => x.Trim());
        if (roles is null)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            bindingContext.ModelState.TryAddModelError("Unauthorized", "roles is not defined.");
            return false;
        }

        var isInRole = roles.Any(r => bindingContext.HttpContext.User.IsInRole(r));
        if (!isInRole)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            bindingContext.ModelState.TryAddModelError("Unauthorized", "User is not in role.");
            return false;
        }

        return true;
    }

    public static bool HasClaim(this object command, ModelBindingContext bindingContext)
    {
        var attribute = command.GetType().GetCustomAttribute<ClaimRequirementAttribute>(false);
        if (attribute is null)
        {
            return true;
        }

        var claim = attribute.Arguments?.FirstOrDefault() as Claim;
        if (claim is null)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            bindingContext.ModelState.TryAddModelError("Unauthorized", "claim is not defined.");
            return false;
        }

        var hasClaim = bindingContext.HttpContext.User.Claims.Any(c => c.Type == claim.Type && c.Value == claim.Value);
        if (!hasClaim)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            bindingContext.ModelState.TryAddModelError("Unauthorized", "User does not have claim.");
            return false;
        }

        return true;
    }
}

