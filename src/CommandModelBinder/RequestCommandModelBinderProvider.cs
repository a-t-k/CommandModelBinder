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
            if (!this.IsValid(json, settings, out var model))
            {
                bindingContext.Result = ModelBindingResult.Failed();
                bindingContext.ModelState.TryAddModelError("Unauthorized", "not valid json.");
                return Task.CompletedTask;
            }

            if (!(model is T))
            {
                // Some request send Object as a string. We check this behaviour with another deserializing.
                if (!this.IsValid(model.ToString(), settings, out model))
                {
                    bindingContext.Result = ModelBindingResult.Failed();
                    bindingContext.ModelState.TryAddModelError("Unauthorized", "not valid json.");
                    return Task.CompletedTask;
                }
            }

            if (!(model is T))
            {
                bindingContext.Result = ModelBindingResult.Failed();
                bindingContext.ModelState.TryAddModelError("Parsing", "Cant parse to object.");
                return Task.CompletedTask;
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

    private bool IsValid(string jsonValue, JsonSerializerSettings jsonSerializerSettings, out dynamic deserializedObject)
    {
        deserializedObject = null;
        try
        {
            deserializedObject = JsonConvert.DeserializeObject(jsonValue, jsonSerializerSettings);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }
}