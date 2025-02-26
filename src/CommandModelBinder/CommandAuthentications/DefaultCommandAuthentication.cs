using CommandModelBinder.CommandAuthentications.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CommandModelBinder.CommandAuthentications;
public class DefaultCommandAuthentication : ICommandAuthentication
{
    public bool Execute(ModelBindingContext bindingContext, object model)
    {
        if (model.IsAnonymousAllowed())
        {
            return true;
        }

        if (model.HasClaim(bindingContext) && model.HasRole(bindingContext) && bindingContext.HttpContext.User.Identity is { IsAuthenticated: true })
        {
            bindingContext.Result = ModelBindingResult.Success(model);
            return true;
        }

        return false;
    }
}