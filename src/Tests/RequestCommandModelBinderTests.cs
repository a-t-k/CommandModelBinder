using CommandModelBinder;
using CommandModelBinder.CommandAuthentications;
using CommandModelBinder.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;

namespace Tests;

public class RequestCommandModelBinderTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void When_BindingContextIsNULL_ThrowException()
    {
        async Task CheckMethod()
        {
            var modelBinder = new RequestCommandModelBinder<IRequestTestCommand>();
            ModelBindingContext? bindingContext = null;
            await modelBinder.BindModelAsync(bindingContext);
        }

        Assert.ThrowsAsync<ArgumentNullException>(CheckMethod);
    }

    [Test]
    public async Task When_BodyPayloadCantBeParsedToCommand_SetError()
    {
        var modelBinder = new RequestCommandModelBinder<IRequestTestCommand>();
        var bindingContext = new DefaultModelBindingContext();
        var bodyPayloadAsString = "{}";
        var bodyStream = this.GenerateStreamFromString(bodyPayloadAsString);
        var httpContext = new DefaultHttpContext();
        var request = new DefaultHttpRequest(httpContext) { Body = bodyStream };
        bindingContext.ActionContext = new ActionContext { HttpContext = httpContext };
        bindingContext.ModelState = new ModelStateDictionary();

        await modelBinder.BindModelAsync(bindingContext);

        Assert.That(!bindingContext.ModelState.IsValid);
        Assert.That(bindingContext.ModelState.ErrorCount == 1);
        Assert.That(bindingContext.ModelState.Keys.Any(e => e == "Parsing"));
    }


    [Test]
    public async Task When_BodyPayloadIsEmpty_SetError()
    {
        var modelBinder = new RequestCommandModelBinder<IRequestTestCommand>();
        var bindingContext = new DefaultModelBindingContext();
        string? bodyPayloadAsString = null;
        var bodyStream = this.GenerateStreamFromString(bodyPayloadAsString);
        var httpContext = new DefaultHttpContext();
        var request = new DefaultHttpRequest(httpContext) { Body = bodyStream };
        bindingContext.ActionContext = new ActionContext { HttpContext = httpContext };
        bindingContext.ModelState = new ModelStateDictionary();

        await modelBinder.BindModelAsync(bindingContext);

        Assert.That(!bindingContext.ModelState.IsValid);
        Assert.That(bindingContext.ModelState.ErrorCount == 1);
        Assert.That(bindingContext.ModelState.Keys.Any(e => e == "Unauthorized"));
    }

    [Test]
    public async Task When_BodyPayloadIsNotValidJson_SetError()
    {
        var modelBinder = new RequestCommandModelBinder<IRequestTestCommand>();
        var bindingContext = new DefaultModelBindingContext();
        var bodyPayloadAsString = "I_Am_Not_Valid_Json";
        var bodyStream = this.GenerateStreamFromString(bodyPayloadAsString);
        var httpContext = new DefaultHttpContext();
        var request = new DefaultHttpRequest(httpContext) { Body = bodyStream };
        bindingContext.ActionContext = new ActionContext { HttpContext = httpContext };
        bindingContext.ModelState = new ModelStateDictionary();

        await modelBinder.BindModelAsync(bindingContext);

        Assert.That(!bindingContext.ModelState.IsValid);
        Assert.That(bindingContext.ModelState.ErrorCount == 1);
        Assert.That(bindingContext.ModelState.Keys.Any(e => e == "Unauthorized"));
    }

    [Test]
    public async Task When_BodyPayloadCanBindToCommand_And_NoAuthenticationAttributeIsSet_SetError()
    {
        var command = new TestCommand();
        var bodyPayloadAsString = command.SerializeCommand<IRequestTestCommand>();
        var bodyStream = this.GenerateStreamFromString(bodyPayloadAsString);
        var modelBinder = new RequestCommandModelBinder<IRequestTestCommand>();
        var bindingContext = new DefaultModelBindingContext();
        var httpContext = new DefaultHttpContext();
        var request = new DefaultHttpRequest(httpContext) { Body = bodyStream };
        bindingContext.ActionContext = new ActionContext { HttpContext = httpContext };
        bindingContext.ModelState = new ModelStateDictionary();

        await modelBinder.BindModelAsync(bindingContext);

        Assert.That(!bindingContext.ModelState.IsValid);
        Assert.That(bindingContext.ModelState.ErrorCount == 1);
        Assert.That(bindingContext.ModelState.Keys.Any(e => e == "Unauthorized"));
    }

    [Test]
    public async Task When_BodyPayloadCanBindToCommand_And_CommandAuthIsSet_And_NoAuthenticationAttributeIsSet_SetError()
    {
        var command = new TestCommand();
        var bodyPayloadAsString = command.SerializeCommand<IRequestTestCommand>();
        var bodyStream = this.GenerateStreamFromString(bodyPayloadAsString);
        var modelBinder = new RequestCommandModelBinder<IRequestTestCommand>(new List<ICommandAuthentication> { new DefaultCommandAuthentication() });
        var bindingContext = new DefaultModelBindingContext();
        var httpContext = new DefaultHttpContext();
        var request = new DefaultHttpRequest(httpContext) { Body = bodyStream };
        bindingContext.ActionContext = new ActionContext { HttpContext = httpContext };
        bindingContext.ModelState = new ModelStateDictionary();

        await modelBinder.BindModelAsync(bindingContext);

        Assert.That(!bindingContext.ModelState.IsValid);
        Assert.That(bindingContext.ModelState.ErrorCount == 1);
        Assert.That(bindingContext.ModelState.Keys.Any(e => e == "Unauthorized"));
    }

    [Test]
    public async Task When_BodyPayloadCanBindToCommand_And_AllowAnonymousAuthenticationAttributeIsSet_SetSuccess()
    {
        var command = new TestCommandWithAnonymousAttribute();
        var bodyPayloadAsString = command.SerializeCommand<IRequestTestCommand>();
        var bodyStream = this.GenerateStreamFromString(bodyPayloadAsString);
        var modelBinder = new RequestCommandModelBinder<IRequestTestCommand>(new List<ICommandAuthentication> { new DefaultCommandAuthentication() });
        var bindingContext = new DefaultModelBindingContext();
        var httpContext = new DefaultHttpContext();
        var request = new DefaultHttpRequest(httpContext) { Body = bodyStream };
        bindingContext.ActionContext = new ActionContext { HttpContext = httpContext };
        bindingContext.ModelState = new ModelStateDictionary();

        await modelBinder.BindModelAsync(bindingContext);

        Assert.That(bindingContext.ModelState.IsValid);
        Assert.That(bindingContext.ModelState.ErrorCount == 0);
    }


    [Test]
    public async Task When_BodyPayloadCanBindToCommand_And_RoleAdministratorWithAutorizeAttributeIsSet_SetError()
    {
        var command = new TestCommandWithRoleAdministrator();
        var bodyPayloadAsString = command.SerializeCommand<IRequestTestCommand>();
        var bodyStream = this.GenerateStreamFromString(bodyPayloadAsString);
        var principal = this.GetClaimsPrincipal();
        var modelBinder = new RequestCommandModelBinder<IRequestTestCommand>(new List<ICommandAuthentication> { new DefaultCommandAuthentication() });
        var bindingContext = new DefaultModelBindingContext();
        var httpContext = new DefaultHttpContext
        {
            User = principal
        };
        var request = new DefaultHttpRequest(httpContext) { Body = bodyStream };
        bindingContext.ActionContext = new ActionContext { HttpContext = httpContext };
        bindingContext.ModelState = new ModelStateDictionary();

        await modelBinder.BindModelAsync(bindingContext);

        Assert.That(bindingContext.ModelState.IsValid);
        Assert.That(bindingContext.ModelState.ErrorCount == 0);
    }

    [Test]
    public async Task When_BodyPayloadCanBindToCommand_And_WrongRoleAndAutorizeAttributeIsSet_SetSuccess()
    {
        var command = new TestCommandWithWrongRole();
        var bodyPayloadAsString = command.SerializeCommand<IRequestTestCommand>();
        var bodyStream = this.GenerateStreamFromString(bodyPayloadAsString);
        var principal = this.GetClaimsPrincipal();
        var modelBinder = new RequestCommandModelBinder<IRequestTestCommand>(new List<ICommandAuthentication> { new DefaultCommandAuthentication() });
        var bindingContext = new DefaultModelBindingContext();
        var httpContext = new DefaultHttpContext
        {
            User = principal
        };
        var request = new DefaultHttpRequest(httpContext) { Body = bodyStream };
        bindingContext.ActionContext = new ActionContext { HttpContext = httpContext };
        bindingContext.ModelState = new ModelStateDictionary();

        await modelBinder.BindModelAsync(bindingContext);

        Assert.That(!bindingContext.ModelState.IsValid);
        Assert.That(bindingContext.ModelState.ErrorCount == 2);
        Assert.That(bindingContext.ModelState.Keys.Any(e => e == "Unauthorized"));
    }

    private Stream GenerateStreamFromString(string? value)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(value);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    private ClaimsPrincipal GetClaimsPrincipal()
    {
        var claimsIdentity = new List<Claim>() { new(ClaimTypes.Role, "Administrator"), new("Test.Claim", string.Empty) };
        var identity = new ClaimsIdentity(claimsIdentity, authenticationType: "OnlyTestAndSetIsAuthenticatedToTrue");
        var principal = new ClaimsPrincipal(identity);
        return principal;
    }
}