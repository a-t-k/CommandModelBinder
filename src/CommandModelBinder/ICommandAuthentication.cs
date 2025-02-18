using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CommandModelBinder;
public interface ICommandAuthentication
{
    bool Execute(ModelBindingContext bindingContext, object model);
}