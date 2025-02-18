using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandModelBinder;
public class RequestCommandModelBinderProvider<T> : IModelBinderProvider
{
    private readonly IEnumerable<ICommandAuthentication> commandAuthentications;
    public RequestCommandModelBinderProvider(IEnumerable<ICommandAuthentication> commandAuthentications = null)
    {
        this.commandAuthentications = commandAuthentications ?? [];
    }

    public IModelBinder GetBinder(ModelBinderProviderContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Metadata.ModelType == typeof(T))
        {
            return new RequestCommandModelBinder<T>(this.commandAuthentications);
        }

        return null;
    }
}

public class RequestCommandModelBinder<T> : IModelBinder
{
    private readonly IEnumerable<ICommandAuthentication> commandAuthentications;

    public RequestCommandModelBinder(IEnumerable<ICommandAuthentication> commandAuthentications = null)
    {
        this.commandAuthentications = commandAuthentications ?? [];
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        string json;
        using (var reader = new StreamReader(bindingContext.ActionContext.HttpContext.Request.Body, Encoding.UTF8))
            json = reader.ReadToEndAsync().Result;
        if (string.IsNullOrWhiteSpace(json))
        {
            bindingContext.Result = ModelBindingResult.Failed();
            bindingContext.ModelState.TryAddModelError("Unauthorized", "no command.");
            return Task.CompletedTask;
        }
        try
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };
            dynamic model = JsonConvert.DeserializeObject(json, settings);
            if (!(model is T))
            {
                // Some request send Object as a string. We check this behaviour with another deserializing.
                model = JsonConvert.DeserializeObject(model, settings);
            }

            if (model is null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                bindingContext.ModelState.TryAddModelError("Parsing", "Cant parse to object.");
            }

            if (this.commandAuthentications.Any() && this.commandAuthentications.All(commandAuthentication => commandAuthentication.Execute(bindingContext, model) is true))
            {
                bindingContext.Result = ModelBindingResult.Success(model);
                return Task.CompletedTask;
            }
        }
        catch (JsonException)
        {
            bindingContext.ModelState.TryAddModelError("Parsing", "Invalid JSON format.");
        }

        bindingContext.Result = ModelBindingResult.Failed();
        bindingContext.ModelState.TryAddModelError("Unauthorized", "Access denied.");
        return Task.CompletedTask;
    }
}