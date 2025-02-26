using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace CommandModelBinder.Tools;
public static class CommandSerializer
{
    public static string SerializeCommand<T>(this T command)
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.All,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        settings.Converters.Add(new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() });
        var serializedCommand = JsonConvert.SerializeObject(command, settings);
        return serializedCommand;
    }
}

